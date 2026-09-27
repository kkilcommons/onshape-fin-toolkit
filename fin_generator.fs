FeatureScript 3070;
import(path : "onshape/std/common.fs", version : "3070.0");
import(path : "onshape/std/primitives.fs", version : "3070.0");

// Shared sampling for the guide preview and Build Fin. Keep the finite
// near-tip stations separate from the terminal maximum-Z loft point.
const FIN_SPAN_STATIONS = [
    0.00, 0.15, 0.30, 0.45, 0.60, 0.75, 0.85, 0.92, 0.96, 0.98, 0.99
];
const FIN_FOIL_X_STATIONS = [
    0.0, 0.01, 0.025, 0.05, 0.10, 0.20, 0.30, 0.40, 0.50,
    0.60, 0.70, 0.80, 0.90, 0.95, 0.975, 0.99, 1.0
];
const NACA_0010_THICKNESS_RATIO = 0.10;
const NACA_0010_RAIL_CHORD_FRACTION = 0.30;

export const FIN_BASE_BOUNDS =
{
    (meter) : [0.001, 0.1143, 1],
    (millimeter) : 114.3
} as LengthBoundSpec;

export const FIN_HEIGHT_BOUNDS =
{
    (meter) : [0.001, 0.127, 1],
    (millimeter) : 127
} as LengthBoundSpec;

export const FIN_TIP_X_BOUNDS =
{
    (meter) : [-1, 0.1474, 1],
    (millimeter) : 147.4
} as LengthBoundSpec;

export const FIN_CANT_BOUNDS =
{
    (degree) : [-15, 0, 15],
    (radian) : 0
} as AngleBoundSpec;


// ============================================================
// LEADING EDGE DEFAULTS
// ============================================================

export const FIN_LE1_X_BOUNDS =
{
    (meter) : [-1, 0.0616, 1],
    (millimeter) : 61.6
} as LengthBoundSpec;

export const FIN_LE1_Z_BOUNDS =
{
    (meter) : [0.001, 0.0445, 1],
    (millimeter) : 44.5
} as LengthBoundSpec;

export const FIN_LE2_X_BOUNDS =
{
    (meter) : [-1, 0.1240, 1],
    (millimeter) : 124.0
} as LengthBoundSpec;

export const FIN_LE2_Z_BOUNDS =
{
    (meter) : [0.001, 0.0914, 1],
    (millimeter) : 91.4
} as LengthBoundSpec;

export const FIN_LE3_X_BOUNDS =
{
    (meter) : [-1, 0.1420, 1],
    (millimeter) : 142.0
} as LengthBoundSpec;

export const FIN_LE3_Z_BOUNDS =
{
    (meter) : [-1, 0.1320, 1],
    (millimeter) : 132.0
} as LengthBoundSpec;


// ============================================================
// TRAILING EDGE DEFAULTS
// ============================================================

export const FIN_TE1_X_BOUNDS =
{
    (meter) : [-1, 0.1111, 1],
    (millimeter) : 111.1
} as LengthBoundSpec;

export const FIN_TE1_Z_BOUNDS =
{
    (meter) : [0.001, 0.0539, 1],
    (millimeter) : 53.9
} as LengthBoundSpec;

export const FIN_TE2_X_BOUNDS =
{
    (meter) : [-1, 0.1343, 1],
    (millimeter) : 134.3
} as LengthBoundSpec;

export const FIN_TE2_Z_BOUNDS =
{
    (meter) : [0.001, 0.1082, 1],
    (millimeter) : 108.2
} as LengthBoundSpec;

export const FIN_TE3_X_BOUNDS =
{
    (meter) : [-1, 0.1490, 1],
    (millimeter) : 149.0
} as LengthBoundSpec;

export const FIN_TE3_Z_BOUNDS =
{
    (meter) : [-1, 0.1240, 1],
    (millimeter) : 124.0
} as LengthBoundSpec;

// Half-thickness of a symmetric NACA 00xx section, normalized by chord.
function naca00HalfThickness(xNorm is number, thicknessRatio is number) returns number
{
    return 5 * thicknessRatio *
        (
            0.2969 * sqrt(xNorm)
            - 0.1260 * xNorm
            - 0.3516 * xNorm^2
            + 0.2843 * xNorm^3
            - 0.1036 * xNorm^4
        );
}

// Return both endpoint coordinates for one planform boundary edge.
function getEdgeEndpoints(context is Context, edge is Query) returns map
{
    var vertices = qAdjacent(
        edge,
        AdjacencyType.VERTEX,
        EntityType.VERTEX
    );

    return {
        "point0" : evVertexPoint(context, {
                "vertex" : qNthElement(vertices, 0)
        }),
        "point1" : evVertexPoint(context, {
                "vertex" : qNthElement(vertices, 1)
        })
    };
}

// Classify an already evaluated boundary without imposing feature-specific
// selection requirements. Preserve query order and the last matching root.
function classifyPlanformBoundary(context is Context, boundaryEdges is array) returns map
{
    var baseEdge;
    var curvedEdges = [];
    for (var edge in boundaryEdges)
    {
        var endpoints = getEdgeEndpoints(context, edge);
        if (abs(endpoints.point0[2] - endpoints.point1[2]) < 0.000001 * meter)
        {
            baseEdge = edge;
        }
        else
        {
            curvedEdges = append(curvedEdges, {
                        "edge" : edge,
                        "point0" : endpoints.point0,
                        "point1" : endpoints.point1
            });
        }
    }
    return { "baseEdge" : baseEdge, "curvedEdges" : curvedEdges };
}

// Root references increase in global X. Keep endpoint order for equal X,
// matching Add Base; Build Fin rejects a zero chord through its own checks.
function getPlanformRootReference(context is Context, baseEdge is Query) returns map
{
    var endpoints = getEdgeEndpoints(context, baseEdge);
    var leadingPoint = endpoints.point0;
    var trailingPoint = endpoints.point1;
    if (leadingPoint[0] > trailingPoint[0])
    {
        leadingPoint = endpoints.point1;
        trailingPoint = endpoints.point0;
    }
    return {
        "leadingPoint" : leadingPoint,
        "trailingPoint" : trailingPoint,
        "chord" : trailingPoint[0] - leadingPoint[0]
    };
}

// Non-root edges have different endpoint heights. Classify their lower
// endpoints against the caller's root X references; ties remain trailing.
function identifyPlanformSides(
    curvedEdges is array,
    rootLeadingX is ValueWithUnits,
    rootTrailingX is ValueWithUnits) returns map
{
    var leadingEdge;
    var trailingEdge;
    var tipPoint;
    for (var edgeData in curvedEdges)
    {
        var rootPoint = edgeData.point0;
        var highPoint = edgeData.point1;
        if (edgeData.point1[2] < edgeData.point0[2])
        {
            rootPoint = edgeData.point1;
            highPoint = edgeData.point0;
        }
        if (abs(rootPoint[0] - rootLeadingX) < abs(rootPoint[0] - rootTrailingX))
        {
            leadingEdge = edgeData.edge;
            tipPoint = highPoint;
        }
        else
        {
            trailingEdge = edgeData.edge;
        }
    }
    return {
        "leadingEdge" : leadingEdge,
        "trailingEdge" : trailingEdge,
        "tipPoint" : tipPoint
    };
}

// Generate one symmetric NACA 00xx section at a normalized span station.
// The root uses its known endpoints; all other sections intersect the solved
// planform edges so every foil remains coincident with the planform.
function createFoilSection(
    context is Context,
    id is Id,
    leadingEdgeQuery is Query,
    trailingEdgeQuery is Query,
    spanFraction is number,
    finHeight is ValueWithUnits,
    rootChord is ValueWithUnits,
    xStations is array,
    thicknessRatio is number,
    railChordFraction is number) returns map
{
    var sectionZ = spanFraction * finHeight;
    var leadingPoint;
    var trailingPoint;

    if (spanFraction == 0)
    {
        leadingPoint = vector(0 * meter, 0 * meter, 0 * meter);
        trailingPoint = vector(rootChord, 0 * meter, 0 * meter);
    }
    else
    {
        var sectionPlane = plane(
            vector(0 * meter, 0 * meter, sectionZ),
            vector(0, 0, 1)
        );

        leadingPoint = evDistance(context, {
                "side0" : leadingEdgeQuery,
                "side1" : sectionPlane
        }).sides[0].point;

        trailingPoint = evDistance(context, {
                "side0" : trailingEdgeQuery,
                "side1" : sectionPlane
        }).sides[0].point;
    }

    var localChord = trailingPoint[0] - leadingPoint[0];
    var upperPoints = [];
    var lowerPoints = [];

    for (var xNorm in xStations)
    {
        var ytNorm = naca00HalfThickness(xNorm, thicknessRatio);

        var xPosition = leadingPoint[0] + xNorm * localChord;
        var yThickness = ytNorm * localChord;

        upperPoints = append(
            upperPoints,
            vector(xPosition, yThickness, sectionZ)
        );

        lowerPoints = append(
            lowerPoints,
            vector(xPosition, -yThickness, sectionZ)
        );
    }

    // Force exact planform endpoints so both surfaces use the identical
    // intersection coordinates returned from the solved planform.
    upperPoints[0] = leadingPoint;
    lowerPoints[0] = leadingPoint;
    upperPoints[size(upperPoints) - 1] = trailingPoint;
    lowerPoints[size(lowerPoints) - 1] = trailingPoint;

    opFitSpline(context, id + "Upper", {
            "points" : upperPoints
    });

    var lowerReverse = [];
    for (var i = size(lowerPoints) - 1; i >= 0; i -= 1)
    {
        lowerReverse = append(lowerReverse, lowerPoints[i]);
    }

    opFitSpline(context, id + "Lower", {
            "points" : lowerReverse
    });

    var upperQuery = qCreatedBy(id + "Upper", EntityType.EDGE);
    var lowerQuery = qCreatedBy(id + "Lower", EntityType.EDGE);

    // A solid loft requires closed profile faces rather than raw wire edges.
    // Fill the two coincident-ended splines into one planar section face.
    opFillSurface(context, id + "Profile", {
            "edgesG0" : qUnion([upperQuery, lowerQuery])
    });

    // NACA 00xx maximum thickness occurs at approximately 30% chord. These
    // evaluated points become the upper and lower side-rail intersections.
    var railX = leadingPoint[0] + railChordFraction * localChord;
    var railY = naca00HalfThickness(
        railChordFraction,
        thicknessRatio
    ) * localChord;

    return {
        "spanFraction" : spanFraction,
        "leadingPoint" : leadingPoint,
        "trailingPoint" : trailingPoint,
        "upper" : upperQuery,
        "lower" : lowerQuery,
        "upperRailPoint" : vector(railX, railY, sectionZ),
        "lowerRailPoint" : vector(railX, -railY, sectionZ),
        "profile" : qCreatedBy(id + "Profile", EntityType.FACE),
        "constructionBodies" : qUnion([
            qCreatedBy(id + "Upper", EntityType.BODY),
            qCreatedBy(id + "Lower", EntityType.BODY),
            qCreatedBy(id + "Profile", EntityType.BODY)
        ])
    };
}



annotation {
    "Feature Type Name" : "Fin Planform",
    "Feature Type Description" : "Create and edit the fin planform outline",
    "Manipulator Change Function" : "finPlanformManipulatorChange"
}
export const finPlanform = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Base" }
        isLength(definition.base, FIN_BASE_BOUNDS);

        annotation { "Name" : "Height" }
        isLength(definition.height, FIN_HEIGHT_BOUNDS);

        annotation { "Name" : "Tip X" }
        isLength(definition.tipX, FIN_TIP_X_BOUNDS);


        annotation { "Name" : "LE1 X" }
        isLength(definition.le1X, FIN_LE1_X_BOUNDS);

        annotation { "Name" : "LE1 Z" }
        isLength(definition.le1Z, FIN_LE1_Z_BOUNDS);

        annotation { "Name" : "LE2 X" }
        isLength(definition.le2X, FIN_LE2_X_BOUNDS);

        annotation { "Name" : "LE2 Z" }
        isLength(definition.le2Z, FIN_LE2_Z_BOUNDS);

        annotation { "Name" : "LE3 X" }
        isLength(definition.le3X, FIN_LE3_X_BOUNDS);


        annotation { "Name" : "TE1 X" }
        isLength(definition.te1X, FIN_TE1_X_BOUNDS);

        annotation { "Name" : "TE1 Z" }
        isLength(definition.te1Z, FIN_TE1_Z_BOUNDS);

        annotation { "Name" : "TE2 X" }
        isLength(definition.te2X, FIN_TE2_X_BOUNDS);

        annotation { "Name" : "TE2 Z" }
        isLength(definition.te2Z, FIN_TE2_Z_BOUNDS);

        annotation { "Name" : "TE3 X" }
        isLength(definition.te3X, FIN_TE3_X_BOUNDS);

        annotation { "Name" : "Show loft guides", "Default" : false }
        definition.showLoftGuides is boolean;
    }
    {
        var planformSketch = newSketch(context, id + "planformSketch", {
                "sketchPlane" : qCreatedBy(makeId("Front"), EntityType.FACE)
        });


        // ============================================================
        // PLANFORM POINTS
        // ============================================================

        var rootLE = vector(
            0 * meter,
            0 * meter
        );

        var rootTE = vector(
            definition.base,
            0 * meter
        );

        var tip = vector(
            definition.tipX,
            definition.height
        );


        var lePoint1 = vector(
            definition.le1X,
            definition.le1Z
        );

        var lePoint2 = vector(
            definition.le2X,
            definition.le2Z
        );

        var lePoint3 = vector(
            definition.le3X,
            definition.height
        );


        var tePoint1 = vector(
            definition.te1X,
            definition.te1Z
        );

        var tePoint2 = vector(
            definition.te2X,
            definition.te2Z
        );

        var tePoint3 = vector(
            definition.te3X,
            definition.height
        );

        if (definition.le1Z >= definition.le2Z ||
            definition.le2Z >= definition.height)
        {
            throw regenError(
                "Leading-edge handle heights must increase toward the tip and remain below Height.",
                ["le1Z", "le2Z", "height"]
            );
        }

        if (definition.te1Z >= definition.te2Z ||
            definition.te2Z >= definition.height)
        {
            throw regenError(
                "Trailing-edge handle heights must increase toward the tip and remain below Height.",
                ["te1Z", "te2Z", "height"]
            );
        }


        // ============================================================
        // LEADING EDGE
        // ============================================================

        skBezier(planformSketch, "leadingEdge", {
                "points" : [
                    rootLE,
                    lePoint1,
                    lePoint2,
                    lePoint3,
                    tip
                ]
        });


        // ============================================================
        // TRAILING EDGE
        // ============================================================

        skBezier(planformSketch, "trailingEdge", {
                "points" : [
                    rootTE,
                    tePoint1,
                    tePoint2,
                    tePoint3,
                    tip
                ]
        });


        // ============================================================
        // TIP GEOMETRY
        // The final control point before the tip shares tip Z. A Bezier curve
        // remains inside the convex hull of its control points, so neither
        // edge can rise above Height and both arrive with horizontal tangents.
        // ============================================================

        // ============================================================
        // BASE
        // ============================================================

        skLineSegment(planformSketch, "base", {
                "start" : rootLE,
                "end" : rootTE
        });


        skSolve(planformSketch);

        // Optional preview of the same upper/lower 30%-chord rails used by
        // Build Fin. No foil profiles or loft body are created here.
        if (definition.showLoftGuides)
        {
            var previewBoundaryEdges = evaluateQuery(
                context,
                qCreatedBy(id + "planformSketch", EntityType.EDGE)
            );
            var previewBoundary = classifyPlanformBoundary(context, previewBoundaryEdges);
            var previewSides = identifyPlanformSides(
                previewBoundary.curvedEdges, rootLE[0], rootTE[0]);
            var previewLeadingEdge = previewSides.leadingEdge;
            var previewTrailingEdge = previewSides.trailingEdge;

            if (previewLeadingEdge == undefined || previewTrailingEdge == undefined)
            {
                throw regenError(
                    "Could not identify the planform edges for the guide preview.",
                    ["showLoftGuides"]
                );
            }

            // Use the same dense station set as Build Fin so the preview shows
            // the actual profile-to-profile rail flow before the loft is built.
            var previewStations = FIN_SPAN_STATIONS;
            var previewUpperPoints = [];
            var previewLowerPoints = [];

            for (var previewFraction in previewStations)
            {
                var previewZ = previewFraction * definition.height;
                var previewLE;
                var previewTE;

                if (previewFraction == 0)
                {
                    previewLE = vector(0 * meter, 0 * meter, 0 * meter);
                    previewTE = vector(definition.base, 0 * meter, 0 * meter);
                }
                else
                {
                    var previewPlane = plane(
                        vector(0 * meter, 0 * meter, previewZ),
                        vector(0, 0, 1)
                    );
                    previewLE = evDistance(context, {
                            "side0" : previewLeadingEdge,
                            "side1" : previewPlane
                    }).sides[0].point;
                    previewTE = evDistance(context, {
                            "side0" : previewTrailingEdge,
                            "side1" : previewPlane
                    }).sides[0].point;
                }

                var previewChord = previewTE[0] - previewLE[0];
                var previewRailX = previewLE[0] + NACA_0010_RAIL_CHORD_FRACTION * previewChord;
                var previewRailY = naca00HalfThickness(
                    NACA_0010_RAIL_CHORD_FRACTION,
                    NACA_0010_THICKNESS_RATIO
                ) * previewChord;

                previewUpperPoints = append(
                    previewUpperPoints,
                    vector(previewRailX, previewRailY, previewZ)
                );
                previewLowerPoints = append(
                    previewLowerPoints,
                    vector(previewRailX, -previewRailY, previewZ)
                );
            }

            var previewTip = vector(definition.tipX, 0 * meter, definition.height);
            previewUpperPoints = append(previewUpperPoints, previewTip);
            previewLowerPoints = append(previewLowerPoints, previewTip);

            opFitSpline(context, id + "previewUpperGuide", {
                    "points" : previewUpperPoints
            });
            opFitSpline(context, id + "previewLowerGuide", {
                    "points" : previewLowerPoints
            });
        }

        // ============================================================
        // PRIMARY MANIPULATORS
        // ============================================================

        var baseManipulator = linearManipulator({
                "base" : vector(0, 0, 0) * meter,
                "direction" : vector(1, 0, 0),
                "offset" : definition.base,
                "minValue" : 0.001 * meter,
                "maxValue" : 1 * meter,
                "primaryParameterId" : "base"
        });


        var heightManipulator = linearManipulator({
                "base" : vector(0, 0, 0) * meter,
                "direction" : vector(0, 0, 1),
                "offset" : definition.height,
                "minValue" : 0.001 * meter,
                "maxValue" : 1 * meter,
                "primaryParameterId" : "height"
        });


        var tipXManipulator = linearManipulator({
                "base" : vector(
                    0 * meter,
                    0 * meter,
                    definition.height
                ),
                "direction" : vector(1, 0, 0),
                "offset" : definition.tipX,
                "minValue" : -1 * meter,
                "maxValue" : 1 * meter,
                "primaryParameterId" : "tipX"
        });


        // ============================================================
        // LEADING EDGE HANDLES
        // ============================================================

        var le1XManipulator = linearManipulator({
                "base" : vector(0 * meter, 0 * meter, definition.le1Z),
                "direction" : vector(1, 0, 0),
                "offset" : definition.le1X,
                "minValue" : -1 * meter,
                "maxValue" : 1 * meter
        });

        var le1ZManipulator = linearManipulator({
                "base" : vector(definition.le1X, 0 * meter, 0 * meter),
                "direction" : vector(0, 0, 1),
                "offset" : definition.le1Z,
                "minValue" : -1 * meter,
                "maxValue" : 1 * meter
        });


        var le2XManipulator = linearManipulator({
                "base" : vector(0 * meter, 0 * meter, definition.le2Z),
                "direction" : vector(1, 0, 0),
                "offset" : definition.le2X,
                "minValue" : -1 * meter,
                "maxValue" : 1 * meter
        });

        var le2ZManipulator = linearManipulator({
                "base" : vector(definition.le2X, 0 * meter, 0 * meter),
                "direction" : vector(0, 0, 1),
                "offset" : definition.le2Z,
                "minValue" : -1 * meter,
                "maxValue" : 1 * meter
        });


        var le3XManipulator = linearManipulator({
                "base" : vector(0 * meter, 0 * meter, definition.height),
                "direction" : vector(1, 0, 0),
                "offset" : definition.le3X,
                "minValue" : -1 * meter,
                "maxValue" : 1 * meter
        });

        // ============================================================
        // TRAILING EDGE HANDLES
        // ============================================================

        var te1XManipulator = linearManipulator({
                "base" : vector(0 * meter, 0 * meter, definition.te1Z),
                "direction" : vector(1, 0, 0),
                "offset" : definition.te1X,
                "minValue" : -1 * meter,
                "maxValue" : 1 * meter
        });

        var te1ZManipulator = linearManipulator({
                "base" : vector(definition.te1X, 0 * meter, 0 * meter),
                "direction" : vector(0, 0, 1),
                "offset" : definition.te1Z,
                "minValue" : -1 * meter,
                "maxValue" : 1 * meter
        });


        var te2XManipulator = linearManipulator({
                "base" : vector(0 * meter, 0 * meter, definition.te2Z),
                "direction" : vector(1, 0, 0),
                "offset" : definition.te2X,
                "minValue" : -1 * meter,
                "maxValue" : 1 * meter
        });

        var te2ZManipulator = linearManipulator({
                "base" : vector(definition.te2X, 0 * meter, 0 * meter),
                "direction" : vector(0, 0, 1),
                "offset" : definition.te2Z,
                "minValue" : -1 * meter,
                "maxValue" : 1 * meter
        });


        var te3XManipulator = linearManipulator({
                "base" : vector(0 * meter, 0 * meter, definition.height),
                "direction" : vector(1, 0, 0),
                "offset" : definition.te3X,
                "minValue" : -1 * meter,
                "maxValue" : 1 * meter
        });

        addManipulators(context, id, {
                "baseManipulator" : baseManipulator,
                "heightManipulator" : heightManipulator,
                "tipXManipulator" : tipXManipulator,

                "le1XManipulator" : le1XManipulator,
                "le1ZManipulator" : le1ZManipulator,

                "le2XManipulator" : le2XManipulator,
                "le2ZManipulator" : le2ZManipulator,

                "le3XManipulator" : le3XManipulator,

                "te1XManipulator" : te1XManipulator,
                "te1ZManipulator" : te1ZManipulator,

                "te2XManipulator" : te2XManipulator,
                "te2ZManipulator" : te2ZManipulator,

                "te3XManipulator" : te3XManipulator,
                "te3XManipulator" : te3XManipulator
        });
    });



export function finPlanformManipulatorChange(
    context is Context,
    definition is map,
    newManipulators is map) returns map
{
    if (newManipulators["baseManipulator"] != undefined)
        definition.base = abs(newManipulators["baseManipulator"].offset);

    if (newManipulators["heightManipulator"] != undefined)
        definition.height = abs(newManipulators["heightManipulator"].offset);

    if (newManipulators["tipXManipulator"] != undefined)
        definition.tipX = newManipulators["tipXManipulator"].offset;


    if (newManipulators["le1XManipulator"] != undefined)
        definition.le1X = newManipulators["le1XManipulator"].offset;

    if (newManipulators["le1ZManipulator"] != undefined)
        definition.le1Z = newManipulators["le1ZManipulator"].offset;

    if (newManipulators["le2XManipulator"] != undefined)
        definition.le2X = newManipulators["le2XManipulator"].offset;

    if (newManipulators["le2ZManipulator"] != undefined)
        definition.le2Z = newManipulators["le2ZManipulator"].offset;

    if (newManipulators["le3XManipulator"] != undefined)
        definition.le3X = newManipulators["le3XManipulator"].offset;

    if (newManipulators["te1XManipulator"] != undefined)
        definition.te1X = newManipulators["te1XManipulator"].offset;

    if (newManipulators["te1ZManipulator"] != undefined)
        definition.te1Z = newManipulators["te1ZManipulator"].offset;

    if (newManipulators["te2XManipulator"] != undefined)
        definition.te2X = newManipulators["te2XManipulator"].offset;

    if (newManipulators["te2ZManipulator"] != undefined)
        definition.te2Z = newManipulators["te2ZManipulator"].offset;

    if (newManipulators["te3XManipulator"] != undefined)
        definition.te3X = newManipulators["te3XManipulator"].offset;

    return definition;
}


// ============================================================
// STEP 2 — BUILD FIN FROM A COMPLETED PLANFORM
// ============================================================

export enum FinFoil
{
    annotation { "Name" : "NACA 0010" }
    NACA_0010
}

annotation {
    "Feature Type Name" : "Build Fin",
    "Feature Type Description" : "DEBUG: build the foil loft and keep all construction geometry"
}
export const buildFin = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation {
            "Name" : "Planform region",
            "Filter" : EntityType.FACE && SketchObject.YES,
            "MaxNumberOfPicks" : 1
        }
        definition.planform is Query;

        annotation { "Name" : "Foil" }
        definition.foil is FinFoil;
    }
    {
        var selectedFaces = evaluateQuery(context, definition.planform);
        if (size(selectedFaces) != 1)
        {
            throw regenError(
                "Select exactly one closed region created by Fin Planform.",
                ["planform"]
            );
        }

        var boundaryQuery = qAdjacent(
            definition.planform,
            AdjacencyType.EDGE,
            EntityType.EDGE
        );
        var boundaryEdges = evaluateQuery(context, boundaryQuery);

        if (size(boundaryEdges) != 3)
        {
            throw regenError(
                "The selected planform must have exactly three boundary edges: base, leading edge, and trailing edge.",
                ["planform"]
            );
        }

        var boundary = classifyPlanformBoundary(context, boundaryEdges);
        var baseEdge = boundary.baseEdge;
        var curvedEdges = boundary.curvedEdges;

        if (baseEdge == undefined || size(curvedEdges) != 2)
        {
            throw regenError(
                "Could not identify the planform base and side edges.",
                ["planform"]
            );
        }

        var rootReference = getPlanformRootReference(context, baseEdge);
        var rootLeadingPoint = rootReference.leadingPoint;
        var rootTrailingPoint = rootReference.trailingPoint;
        var sides = identifyPlanformSides(
            curvedEdges, rootLeadingPoint[0], rootTrailingPoint[0]);
        var leadingEdgeQuery = sides.leadingEdge;
        var trailingEdgeQuery = sides.trailingEdge;
        var tipPoint = sides.tipPoint;

        if (leadingEdgeQuery == undefined ||
            trailingEdgeQuery == undefined ||
            tipPoint == undefined)
        {
            throw regenError(
                "Could not distinguish the planform leading and trailing edges.",
                ["planform"]
            );
        }

        var rootChord = rootReference.chord;
        var finHeight = tipPoint[2] - rootLeadingPoint[2];

        if (rootChord <= 0 * meter || finHeight <= 0 * meter)
        {
            throw regenError(
                "The planform must have positive base length and height.",
                ["planform"]
            );
        }

        // Fixed V1 span stations and normalized NACA sampling locations.
        // Denser finite profiles reduce the amount of interpolation the loft
        // must infer, while the clustered near-tip stations still control the
        // final collapse into the maximum-Z tip point.
        var spanStations = FIN_SPAN_STATIONS;
        var xStations = FIN_FOIL_X_STATIONS;

        var thicknessRatio = NACA_0010_THICKNESS_RATIO;
        var railChordFraction = NACA_0010_RAIL_CHORD_FRACTION;
        var foilSections = [];

        for (var i = 0; i < size(spanStations); i += 1)
        {
            foilSections = append(
                foilSections,
                createFoilSection(
                    context,
                    id + ("foilSection" ~ i),
                    leadingEdgeQuery,
                    trailingEdgeQuery,
                    spanStations[i],
                    finHeight,
                    rootChord,
                    xStations,
                    thicknessRatio,
                    railChordFraction
                )
            );
        }

        var loftProfiles = [];
        var sectionConstructionBodies = [];
        var upperRailPoints = [];
        var lowerRailPoints = [];

        for (var i = 0; i < size(foilSections); i += 1)
        {
            loftProfiles = append(loftProfiles, foilSections[i].profile);
            upperRailPoints = append(upperRailPoints, foilSections[i].upperRailPoint);
            lowerRailPoints = append(lowerRailPoints, foilSections[i].lowerRailPoint);
            sectionConstructionBodies = append(
                sectionConstructionBodies,
                foilSections[i].constructionBodies
            );
        }

        opPoint(context, id + "loftTip", {
                "point" : tipPoint
        });

        loftProfiles = append(
            loftProfiles,
            qCreatedBy(id + "loftTip", EntityType.VERTEX)
        );
        upperRailPoints = append(upperRailPoints, tipPoint);
        lowerRailPoints = append(lowerRailPoints, tipPoint);

        opFitSpline(context, id + "upperThicknessRail", {
                "points" : upperRailPoints
        });
        opFitSpline(context, id + "lowerThicknessRail", {
                "points" : lowerRailPoints
        });

        var upperThicknessRail = qCreatedBy(
            id + "upperThicknessRail",
            EntityType.EDGE
        );
        var lowerThicknessRail = qCreatedBy(
            id + "lowerThicknessRail",
            EntityType.EDGE
        );

        opLoft(context, id + "finLoft", {
                "profileSubqueries" : loftProfiles,
                "guideSubqueries" : [
                    leadingEdgeQuery,
                    trailingEdgeQuery,
                    upperThicknessRail,
                    lowerThicknessRail
                ],
                "bodyType" : ToolBodyType.SOLID
        });

        sectionConstructionBodies = append(
            sectionConstructionBodies,
            qCreatedBy(id + "loftTip", EntityType.BODY)
        );
        sectionConstructionBodies = append(
            sectionConstructionBodies,
            qCreatedBy(id + "upperThicknessRail", EntityType.BODY)
        );
        sectionConstructionBodies = append(
            sectionConstructionBodies,
            qCreatedBy(id + "lowerThicknessRail", EntityType.BODY)
        );

        // DEBUG BUILD: intentionally keep the section profiles, tip point,
        // upper/lower thickness rails, and the planform LE/TE guide curves.
        // Once the tip behavior is understood, restore the opDeleteBodies call.
    });


// ============================================================
// STEP 3 — ADD A MOUNTING BASE
// Futures geometry is the first supported standard.  The generic
// feature name leaves room for later FCS and other base systems.
// ============================================================

export enum FinBaseStandard
{
    annotation { "Name" : "Futures side / quad front (3/4 in)" }
    FUTURES_SIDE_QUAD_FRONT,

    annotation { "Name" : "Futures center / quad rear (1/2 in)" }
    FUTURES_CENTER_QUAD_REAR
}

export enum FinBaseAlignment
{
    annotation { "Name" : "Rear to trailing edge" }
    REAR_TO_TRAILING_EDGE,

    annotation { "Name" : "Front to leading edge" }
    FRONT_TO_LEADING_EDGE
}


// Tuned nominal Futures dimensions, shared by both base variants.
// These values are not manufacturing compensation or construction margins.
const FUTURES_BASE_DIMENSIONS = {
    "length" : 113.5 * millimeter,
    "thickness" : 7.0 * millimeter,
    "screwNotchCenterX" : 3.3 * millimeter,
    "screwNotchCenterDepth" : 6.4 * millimeter,
    "screwNotchRadius" : 2.2 * millimeter,
    "bottomFilletRadius" : 2 * millimeter,
    "slotFilletRadius" : 1.5 * millimeter
};

// Placement policy for a root longer than the base in rear-alignment mode.
const FIN_BASE_LEADING_OVERHANG = 5 * millimeter;

// Keep construction clearance separate from nominal mounting dimensions.
const FIN_BASE_CUTTER_MARGIN = 1 * millimeter;
const FIN_ROOT_EXTENSION_MARGIN = 1 * millimeter;
const FIN_ROOT_TRIM_MARGIN = 1 * millimeter;

// Variant-specific nominal geometry. Keep the retention-mouth bottom offset
// separate from the front inset even where their tuned values coincide.
function getFinBaseDimensions(baseStandard is FinBaseStandard) returns map
{
    var dimensions = FUTURES_BASE_DIMENSIONS;
    if (baseStandard == FinBaseStandard.FUTURES_SIDE_QUAD_FRONT)
    {
        dimensions.depth = 17.7 * millimeter;
        dimensions.retentionMouthTop = 5.5 * millimeter;
        dimensions.retentionMouthBottom = dimensions.depth - 2.1 * millimeter;
        dimensions.frontBottomInset = 2.1 * millimeter;
    }
    else
    {
        dimensions.depth = 12.7 * millimeter;
        dimensions.retentionMouthTop = 4.5 * millimeter;
        dimensions.retentionMouthBottom = dimensions.depth - 0.7 * millimeter;
        dimensions.frontBottomInset = 0.7 * millimeter;
    }
    return dimensions;
}


function createBaseRetentionNotchCutter(
    context is Context,
    id is Id,
    baseLength is ValueWithUnits,
    baseThickness is ValueWithUnits,
    baseDepth is ValueWithUnits,
    mouthTop is ValueWithUnits,
    mouthBottom is ValueWithUnits,
    frontBottomInset is ValueWithUnits)
{
    var cutterPlane = plane(
        vector(0 * meter, -baseThickness / 2 - FIN_BASE_CUTTER_MARGIN, 0 * meter),
        vector(0, 1, 0),
        vector(1, 0, 0)
    );
    var notchSketch = newSketchOnPlane(context, id + "Sketch", {
            "sketchPlane" : cutterPlane
    });

    // The standalone base is built with its rear/slot end at local X = 0
    // and front/V-notch end at local X = baseLength.  Both mouth points lie
    // on the raked front face.  This construction produces an exact 90-degree
    // included notch angle.
    var mouthTopX =
        baseLength - frontBottomInset * mouthTop / baseDepth;
    var mouthBottomX =
        baseLength - frontBottomInset * mouthBottom / baseDepth;
    var mouthTopPoint = vector(mouthTopX, mouthTop);
    var mouthBottomPoint = vector(mouthBottomX, mouthBottom);
    var mouthChord = mouthBottomPoint - mouthTopPoint;
    var mouthChordLength = norm(mouthChord);
    var inwardNormal = vector(
        -mouthChord[1] / mouthChordLength,
        mouthChord[0] / mouthChordLength
    );
    var apexPoint = (mouthTopPoint + mouthBottomPoint) / 2
        + inwardNormal * mouthChordLength / 2;

    skLineSegment(notchSketch, "upper", {
            "start" : mouthTopPoint,
            "end" : apexPoint
    });
    skLineSegment(notchSketch, "lower", {
            "start" : apexPoint,
            "end" : mouthBottomPoint
    });
    skLineSegment(notchSketch, "mouth", {
            "start" : mouthBottomPoint,
            "end" : mouthTopPoint
    });
    skSolve(notchSketch);

    opExtrude(context, id + "Extrude", {
            "entities" : qSketchRegion(id + "Sketch"),
            "direction" : vector(0, 1, 0),
            "endBound" : BoundingType.BLIND,
            "endDepth" : baseThickness + 2 * FIN_BASE_CUTTER_MARGIN
    });
}


function createBaseBlank(
    context is Context,
    id is Id,
    baseLength is ValueWithUnits,
    baseThickness is ValueWithUnits,
    baseDepth is ValueWithUnits,
    frontBottomInset is ValueWithUnits)
{
    var profilePlane = plane(
        vector(0 * meter, -baseThickness / 2, 0 * meter),
        vector(0, 1, 0),
        vector(1, 0, 0)
    );
    var profileSketch = newSketchOnPlane(context, id + "Sketch", {
            "sketchPlane" : profilePlane
    });

    skLineSegment(profileSketch, "top", {
            "start" : vector(0 * meter, 0 * meter),
            "end" : vector(baseLength, 0 * meter)
    });
    skLineSegment(profileSketch, "front", {
            "start" : vector(baseLength, 0 * meter),
            "end" : vector(baseLength - frontBottomInset, baseDepth)
    });
    skLineSegment(profileSketch, "bottom", {
            "start" : vector(baseLength - frontBottomInset, baseDepth),
            "end" : vector(0 * meter, baseDepth)
    });
    skLineSegment(profileSketch, "rear", {
            "start" : vector(0 * meter, baseDepth),
            "end" : vector(0 * meter, 0 * meter)
    });
    skSolve(profileSketch);

    opExtrude(context, id + "Extrude", {
            "entities" : qSketchRegion(id + "Sketch"),
            "direction" : vector(0, 1, 0),
            "endBound" : BoundingType.BLIND,
            "endDepth" : baseThickness
    });
}


// Build and detail one base at the world origin.  Returns only the resulting
// solid body so callers can orient and position it without touching other
// bodies in the Part Studio.
function createDetailedFinBase(
    context is Context,
    id is Id,
    baseStandard is FinBaseStandard) returns map
{
    var dimensions = getFinBaseDimensions(baseStandard);
    var baseLength = dimensions.length;
    var baseThickness = dimensions.thickness;
    var screwNotchCenterX = dimensions.screwNotchCenterX;
    var screwNotchCenterDepth = dimensions.screwNotchCenterDepth;
    var screwNotchRadius = dimensions.screwNotchRadius;
    var baseDepth = dimensions.depth;
    var retentionMouthTop = dimensions.retentionMouthTop;
    var retentionMouthBottom = dimensions.retentionMouthBottom;
    var frontBottomInset = dimensions.frontBottomInset;

    createBaseBlank(
        context,
        id + "blank",
        baseLength,
        baseThickness,
        baseDepth,
        frontBottomInset
    );

    fCuboid(context, id + "slotCutter", {
            "corner1" : vector(
                -FIN_BASE_CUTTER_MARGIN,
                -baseThickness / 2 - FIN_BASE_CUTTER_MARGIN,
                -screwNotchCenterDepth - screwNotchRadius
            ),
            "corner2" : vector(
                screwNotchCenterX,
                baseThickness / 2 + FIN_BASE_CUTTER_MARGIN,
                -screwNotchCenterDepth + screwNotchRadius
            )
    });
    fCylinder(context, id + "roundCutter", {
            "bottomCenter" : vector(
                screwNotchCenterX,
                -baseThickness / 2 - FIN_BASE_CUTTER_MARGIN,
                -screwNotchCenterDepth
            ),
            "topCenter" : vector(
                screwNotchCenterX,
                baseThickness / 2 + FIN_BASE_CUTTER_MARGIN,
                -screwNotchCenterDepth
            ),
            "radius" : screwNotchRadius
    });
    createBaseRetentionNotchCutter(
        context,
        id + "retentionNotch",
        baseLength,
        baseThickness,
        baseDepth,
        retentionMouthTop,
        retentionMouthBottom,
        frontBottomInset
    );

    opBoolean(context, id + "cutDetails", {
            "targets" : qCreatedBy(id + "blank" + "Extrude", EntityType.BODY),
            "tools" : qUnion([
                qCreatedBy(id + "slotCutter", EntityType.BODY),
                qCreatedBy(id + "roundCutter", EntityType.BODY),
                qCreatedBy(id + "retentionNotch" + "Extrude", EntityType.BODY)
            ]),
            "operationType" : BooleanOperationType.SUBTRACTION
    });

    // The subtraction modifies the original blank; it does not create a new
    // result body owned by cutDetails. Keep the body query anchored to the
    // blank extrusion so downstream fillet and transform queries stay valid.
    var baseBody = qCreatedBy(id + "blank" + "Extrude", EntityType.BODY);
    var baseEdges = qOwnedByBody(baseBody, EntityType.EDGE);
    var rearBottomEdge = qClosestTo(
        baseEdges,
        vector(0 * millimeter, 0 * millimeter, -baseDepth)
    );
    var frontBottomEdge = qClosestTo(
        baseEdges,
        vector(
            baseLength - frontBottomInset,
            0 * millimeter,
            -baseDepth
        )
    );

    opFillet(context, id + "bottomFillets", {
            "entities" : qUnion([rearBottomEdge, frontBottomEdge]),
            "radius" : dimensions.bottomFilletRadius
    });

    baseEdges = qOwnedByBody(baseBody, EntityType.EDGE);
    var slotUpperEdge = qClosestTo(
        baseEdges,
        vector(
            0 * millimeter,
            0 * millimeter,
            -screwNotchCenterDepth + screwNotchRadius
        )
    );
    var slotLowerEdge = qClosestTo(
        baseEdges,
        vector(
            0 * millimeter,
            0 * millimeter,
            -screwNotchCenterDepth - screwNotchRadius
        )
    );
    opFillet(context, id + "slotFillets", {
            "entities" : qUnion([slotUpperEdge, slotLowerEdge]),
            "radius" : dimensions.slotFilletRadius
    });

    opDeleteBodies(context, id + "deleteConstruction", {
            "entities" : qUnion([
                qCreatedBy(id + "retentionNotch" + "Sketch", EntityType.BODY),
                qCreatedBy(id + "blank" + "Sketch", EntityType.BODY)
            ])
    });

    return {
        "body" : baseBody,
        "length" : baseLength,
        "depth" : baseDepth,
        "thickness" : baseThickness
    };
}


annotation {
    "Feature Type Name" : "Add Base",
    "Feature Type Description" : "Add and position a mounting base, with optional fin cant"
}
export const addBase = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation {
            "Name" : "Fin body",
            "Filter" : EntityType.BODY && BodyType.SOLID,
            "MaxNumberOfPicks" : 1
        }
        definition.finBody is Query;

        annotation {
            "Name" : "Planform region",
            "Filter" : EntityType.FACE && SketchObject.YES,
            "MaxNumberOfPicks" : 1
        }
        definition.planform is Query;

        annotation { "Name" : "Base standard" }
        definition.baseStandard is FinBaseStandard;

        annotation { "Name" : "Alignment" }
        definition.alignment is FinBaseAlignment;

        annotation { "Name" : "Chordwise offset" }
        isLength(definition.offset, ZERO_DEFAULT_LENGTH_BOUNDS);

        annotation { "Name" : "Fin cant" }
        isAngle(definition.cant, FIN_CANT_BOUNDS);
    }
    {
        if (size(evaluateQuery(context, definition.finBody)) != 1)
        {
            throw regenError("Select exactly one solid fin body.", ["finBody"]);
        }
        if (size(evaluateQuery(context, definition.planform)) != 1)
        {
            throw regenError(
                "Select exactly one closed Fin Planform region.",
                ["planform"]
            );
        }

        var boundaryEdges = evaluateQuery(context, qAdjacent(
            definition.planform,
            AdjacencyType.EDGE,
            EntityType.EDGE
        ));
        var boundary = classifyPlanformBoundary(context, boundaryEdges);
        var baseEdge = boundary.baseEdge;
        if (baseEdge == undefined)
        {
            throw regenError(
                "Could not identify the planform root edge.",
                ["planform"]
            );
        }

        var rootReference = getPlanformRootReference(context, baseEdge);
        var rootLeadingPoint = rootReference.leadingPoint;
        var rootTrailingPoint = rootReference.trailingPoint;
        var rootChord = rootReference.chord;

        // Positive cant moves the tip toward global +Y.  rotationAround is
        // counterclockwise looking against +X, so negate the user angle.
        if (abs(definition.cant) > 0.000001 * degree)
        {
            // Continue the ACTUAL selected foil downward, not the planform
            // region (which lies in X-Z and has no foil thickness).
            var rootFaces = [];
            for (var face in evaluateQuery(context, qGeometry(
                qOwnedByBody(definition.finBody, EntityType.FACE),
                GeometryType.PLANE)))
            {
                var facePlane = evPlane(context, { "face" : face });
                if (abs(facePlane.normal[2]) > 0.999999 &&
                    abs(facePlane.origin[2] - rootLeadingPoint[2]) <
                    0.000001 * meter)
                {
                    rootFaces = append(rootFaces, face);
                }
            }
            if (size(rootFaces) == 0)
            {
                throw regenError("The fin needs a flat root at the selected planform root height before cant is applied.", ["finBody", "planform"]);
            }
            var rootFaceQuery = qUnion(rootFaces);
            var rootBox = evBox3d(context, {
                    "topology" : rootFaceQuery,
                    "tight" : true
            });
            // For a rotation about X at Y=0, depth >= |Y| tan(|cant|)
            // ensures even the lifted side crosses the mounting plane.
            var rootReach = max(abs(rootBox.minCorner[1]),
                                abs(rootBox.maxCorner[1]));
            var extensionDepth = rootReach * tan(abs(definition.cant)) +
                                 FIN_ROOT_EXTENSION_MARGIN;
            opExtrude(context, id + "extendRoot", {
                    "entities" : rootFaceQuery,
                    "direction" : vector(0, 0, -1),
                    "endBound" : BoundingType.BLIND,
                    "endDepth" : extensionDepth
            });
            opBoolean(context, id + "joinRootExtension", {
                    "tools" : qUnion([definition.finBody,
                        qCreatedBy(id + "extendRoot", EntityType.BODY)]),
                    "operationType" : BooleanOperationType.UNION
            });
            opTransform(context, id + "cantFin", {
                    "bodies" : definition.finBody,
                    "transform" : rotationAround(
                        line(
                            vector(
                                rootLeadingPoint[0],
                                0 * meter,
                                rootLeadingPoint[2]
                            ),
                            vector(1, 0, 0)
                        ),
                        -definition.cant
                    )
            });

            // Trim only the fin, before creating the tab, so no tab fitting
            // dimensions can be affected by this cutter.
            var cantedBox = evBox3d(context, {
                    "topology" : definition.finBody,
                    "tight" : true
            });
            var trimMargin = FIN_ROOT_TRIM_MARGIN;
            fCuboid(context, id + "rootTrimTool", {
                    "corner1" : cantedBox.minCorner -
                                vector(1, 1, 1) * trimMargin,
                    "corner2" : vector(
                        cantedBox.maxCorner[0] + trimMargin,
                        cantedBox.maxCorner[1] + trimMargin,
                        rootLeadingPoint[2])
            });
            opBoolean(context, id + "trimRootFlush", {
                    "targets" : definition.finBody,
                    "tools" : qCreatedBy(id + "rootTrimTool", EntityType.BODY),
                    "operationType" : BooleanOperationType.SUBTRACTION
            });
        }

        var baseData = createDetailedFinBase(
            context,
            id + "mountingBase",
            definition.baseStandard
        );

        // User-facing X convention:
        //   lower X = front / leading-edge side
        //   higher X = rear / trailing-edge side
        // The verified standalone geometry is opposite, so mirror it in X
        // before translating it into position.
        var baseFrontX;
        var baseRearX;
        if (definition.alignment ==
            FinBaseAlignment.REAR_TO_TRAILING_EDGE)
        {
            if (rootChord > baseData.length)
            {
                // For an oversized root, preserve a controlled 5 mm leading-
                // edge overhang instead of pushing the base too far forward.
                baseFrontX =
                    rootLeadingPoint[0] + FIN_BASE_LEADING_OVERHANG + definition.offset;
                baseRearX = baseFrontX + baseData.length;
            }
            else
            {
                baseRearX = rootTrailingPoint[0] + definition.offset;
                baseFrontX = baseRearX - baseData.length;
            }
        }
        else
        {
            baseFrontX = rootLeadingPoint[0] + definition.offset;
            baseRearX = baseFrontX + baseData.length;
        }

        var orientBase = mirrorAcross(plane(
            vector(0, 0, 0) * meter,
            vector(1, 0, 0)
        ));
        var placeBase = transform(vector(
            baseRearX,
            0 * meter,
            rootLeadingPoint[2]
        ));
        opTransform(context, id + "positionBase", {
                "bodies" : baseData.body,
                "transform" : placeBase * orientBase
        });

        opBoolean(context, id + "joinFinAndBase", {
                "tools" : qUnion([definition.finBody, baseData.body]),
                "operationType" : BooleanOperationType.UNION
        });
        if (size(evaluateQuery(context, qUnion([
            definition.finBody, baseData.body]))) != 1)
        {
            throw regenError("The base does not contact the trimmed fin. Reduce the chordwise offset or change alignment.", ["offset", "alignment"]);
        }
    });
