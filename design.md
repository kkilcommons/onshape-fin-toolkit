# Onshape Fin Toolkit — Design and Project Context

## Document purpose

This file is the source of truth for the `onshape-fin-toolkit` repository. It records the intended workflow, coordinate conventions, implemented V1 behavior, manufacturing assumptions, validation status, and V2 backlog.

When implementation and this document disagree, do not silently choose one. Confirm the behavior in Onshape, then update the code and this document together.

## Project summary

The project is a set of Onshape FeatureScript tools for:

1. Creating an editable surf-fin planform.
2. Lofting a foil through that planform.
3. Adding a surfboard-fin mounting base.
4. Generating inverse mold tooling for wet composite layup.
5. Eventually exporting named printable mold bodies through the Onshape API.

The design is intentionally staged. Users should shape and approve the planform before foil profiles and solid geometry obscure it.

## Current checkpoint

V1 is a working end-to-end geometric prototype in Onshape.

The current workflow is:

```text
Fin Planform -> Build Fin -> Add Base -> Create Mold
```

Implemented:

- Editable closed planform with manipulators.
- Optional planform-mode preview of the two thickness rails.
- NACA 0010 section generation from local planform chords.
- Eleven finite foil sections and a terminal maximum-Z tip point.
- Four-rail solid loft.
- Separate downstream mounting-base feature.
- Two tuned Futures-style base variants.
- Front/rear alignment, chordwise offset, and fin cant.
- Root extension and flush trimming to eliminate the cant-induced root gap/clash.
- Standalone four-piece inverse-mold proof of concept.
- Two foil-side mold bodies and two lengthwise-split base plugs.
- Open plug socket, controlled plug exposure, world-aligned external mold walls, and a seam that follows the actual canted fin geometry.
- Stable names for the four mold bodies.

Not yet validated:

- Physical Futures-box fit.
- Print tolerances, resin shrinkage, or mold cavity compensation.
- Mold draft and complete undercut/release analysis.
- Wet layup process capability, laminate quality, or structural strength.
- Automated STL export.

## Current source files

The V1 checkpoint was developed in these files:

```text
FinGenerator_step18_cant_root_trim.fs
CreateMold_four_piece_poc.fs
```

`FinGenerator_step18_cant_root_trim.fs` currently contains three exported features:

- `Fin Planform`
- `Build Fin`
- `Add Base`

`CreateMold_four_piece_poc.fs` contains the standalone mold feature:

- `Create Mold - 4 Piece POC`

The repository may rename these to stable production filenames during V2, but behavior should not change during the initial refactor.

## Coordinate system

Use one convention throughout the project:

| Axis | Meaning |
|---|---|
| X | Chordwise; increasing from leading edge toward trailing edge |
| Y | Foil thickness; positive and negative sides of the foil |
| Z | Spanwise; increasing from root toward tip |

Additional rules:

- The editable planform lies in `Y = 0`.
- The root leading edge is the planform origin.
- The root trailing edge is at larger X.
- The mounting base extends below the root plane.
- Positive cant moves the tip toward global `+Y`.
- Build Fin creates the uncanted aerodynamic body. Add Base applies cant downstream about the root chord axis.

Do not mix this with the older document convention that placed the planform in X-Y and thickness in Z.

## User workflow

### 1. Fin Planform

The user inserts `Fin Planform` and edits only the outline.

Primary inputs:

- Base length.
- Height.
- Tip X position/rake.
- Three chordwise controls on the leading edge.
- Three chordwise controls on the trailing edge.
- X/Z manipulation for the lower shape controls.
- Horizontal arrival into the maximum-Z tip through the final pre-tip controls.

The planform consists of:

- One leading-edge Bezier curve.
- One trailing-edge Bezier curve.
- One straight root segment.
- One shared tip at the maximum Z location.

The planform is a closed selectable sketch region. Numeric parameters are persistent state; manipulators are an editing interface.

An optional `Show loft guides` control previews the upper and lower thickness-rail flow without generating foil profiles or the solid fin.

### 2. Build Fin

The user inserts `Build Fin`, selects the closed Fin Planform region, and selects a foil.

The feature infers the three planform boundary edges:

- The root is the edge whose endpoints have the same Z coordinate.
- The two remaining edges are classified as leading and trailing using their root endpoints.
- Their shared highest endpoint is the terminal loft tip.

The planform is the source of truth for every local chord. There is no independent chord-distribution model.

### 3. Add Base

The user inserts `Add Base`, selects the fin body and original planform region, chooses a mounting standard, and sets placement and cant.

The feature creates a mounting base as a separate downstream operation and unions it with the fin.

### 4. Create Mold

The user inserts the standalone mold feature and selects:

- The completed fin body.
- The leading-edge seam.
- The trailing-edge seam.

The selected LE and TE geometry define the actual foil center/parting plane. This makes the mold seam follow rake and cant rather than assuming a global plane.

## Foil and section architecture

### V1 foil

V1 supports NACA 0010.

- Normalized chord coordinate: `x = 0` at LE and `x = 1` at TE.
- Thickness is normalized by local chord.
- Maximum thickness ratio is 10%.
- The thickness-guide landmark is approximately 30% chord, near maximum thickness for NACA 0010.

Every generated section is forced to begin exactly at the solved planform LE and end exactly at the solved planform TE.

### Span stations

The fixed V1 finite sections are:

```text
[0.00, 0.15, 0.30, 0.45, 0.60, 0.75, 0.85, 0.92, 0.96, 0.98, 0.99]
```

The loft terminates at the single true maximum-Z tip point. Dense near-tip stations constrain the final profile collapse and prevent the loft from transitioning backward to a lower geometric endpoint.

### Loft topology

Each section uses consistent point ordering and segmentation. The loft uses four guides:

1. Solved planform leading edge.
2. Solved planform trailing edge.
3. Upper rail through the foil's thickness landmark.
4. Lower rail through the foil's thickness landmark.

Both thickness rails include all finite sections and terminate at the true tip point.

Do not add more rails unless a specific uncontrolled loft region is demonstrated. Mixing different foil families along the span is outside the MVP.

## Mounting-base design

The generic feature is called `Add Base` so later versions can support FCS or other systems.

Current base choices:

- Futures side / quad front, nominal 3/4-inch depth.
- Futures center / quad rear, nominal 1/2-inch depth.

The tuned geometry includes:

- Nominal 113.5 mm base length.
- Circular rear retention recess.
- A 90-degree front retention notch where specified.
- Raked lower-front geometry on the smaller base.
- 1.5 mm roll/fillet at the rear slot edge.
- 2 mm roll/fillet at the lower rear corner.
- 2 mm roll/fillet at the lower front corner.

### Placement

Default mode aligns the base rear with the root trailing edge. Alternate mode aligns the base front with the root leading edge.

A signed chordwise offset adjusts the automatic placement. Positive offset moves the base toward the trailing-edge direction, global `+X`.

For rear alignment when the root chord is longer than the 113.5 mm base, the base front is placed 5 mm behind the planform leading edge. This leaves 5 mm of fin leading-edge overhang rather than pushing the base excessively forward.

### Cant and root closure

Cant range is `-15 degrees` to `+15 degrees`.

The mounting base remains seated and uncanted. Only the fin is rotated about the root chord axis.

For nonzero cant, Add Base:

1. Finds the actual planar root foil face.
2. Extrudes that foil downward far enough to cross the mounting plane after rotation.
3. Unions the extension with the fin.
4. Rotates the fin about the root chord axis.
5. Trims all fin material below the original root plane.
6. Creates and positions the mounting base.
7. Unions the trimmed fin and base into one solid.

The downward extension includes 1 mm of construction margin. That margin is trimmed away and is not a manufacturing allowance.

## Four-piece mold proof of concept

### Mold bodies

The current mold tool generates four named solids:

1. `Mold - Foil Side A`
2. `Mold - Foil Side B`
3. `Mold - Base Plug A`
4. `Mold - Base Plug B`

The finished fin remains present as the nominal master while it cuts all four cavities.

### Foil-side split

The foil halves split along the center plane calculated from the selected leading and trailing edges. The internal seam therefore follows the actual canted fin.

The exterior foil-mold envelopes remain aligned with global X, Y, and Z for printing and fixturing, even when the internal fin cavity is canted.

### Base split

The base plugs are created first and split lengthwise, or "hot-dog style," in the thickness direction. Each plug contains the full mounting-base length and releases toward its corresponding foil-side mold.

The foil-side molds contain a through-open socket for the assembled base plugs. The socket must remain open at the bottom so the plugs can actually be inserted and removed.

Current default overlap concept:

- Base plugs extend 15 mm below the selected body's minimum Z.
- Foil-side molds extend 5 mm below the selected body's minimum Z.
- Therefore 10 mm of the base plug remains exposed for insertion, clamping, and removal.

The external bottoms of the foil-side mold blocks remain straight in global Z; they do not tilt with fin cant.

### Current mold parameters

The proof-of-concept exposes:

- Root split Z; normally 0 mm for the current Add Base output.
- Chordwise margin.
- Tip margin.
- Bottom margin.
- Mold wall outside foil.
- Base width.
- Base plug border.
- Fin-half overlap below body.
- Base hot-dog split offset.

The bottom margin must be greater than the fin-half overlap so part of the plug remains exposed.

## Intended wet-layup process

The current manufacturing concept is an all-wet assembly:

1. Lay up and wet out cut glass against both foil-side cavities.
2. Close and clamp the two side molds while both skins remain wet.
3. Hold the mold vertically with the fin tip down and base opening up.
4. Add a measured glass-and-resin charge through the base opening.
5. Intentionally overfill the temporary loading volume.
6. Insert and clamp the two-piece base plug to its hard stop.
7. Displace excess resin and air through controlled bleed and vent paths.
8. Cure as one assembly and remove pieces in the validated release order.

Important limitations:

- Plug insertion does not by itself guarantee uniform glass compaction throughout the foil.
- Glass mass and resin mass must be independently measured.
- The resin manufacturer's mix ratio must remain controlled.
- The glass-to-resin target must explicitly state mass or volume basis.
- Reinforcement continuity across LE, TE, and root must be designed; strength cannot depend on an unreinforced resin seam.
- Cure exotherm and printed-tool deformation must be evaluated for the selected resin and tool material.

## Manufacturing compensation policy

Do not globally scale the fin or casually oversize the mounting base.

The mounting interface remains at its physically validated nominal dimensions. Printer error, release allowance, coatings, and resin shrinkage should be implemented later as separate named offsets on the relevant mold-cavity or tooling surfaces.

Fit compensation should be added only after printing, molding, measuring, and test-fitting real parts. An initial study around 0.5 mm may be reasonable, but it is not yet an approved global value.

## V2 development plan

### Phase 1 — Freeze and characterize the baseline

- Preserve the working V1 Onshape document/version.
- Create a V2 Onshape document or branch.
- Commit the exact working FeatureScript files to Git.
- Run the regression matrix below before refactoring.

### Phase 2 — Behavior-preserving refactor

- Rename prototype files to stable production names.
- Remove duplicated planform-boundary and reference-point logic.
- Centralize span stations, foil sample stations, thickness ratio, and rail landmark data.
- Centralize mounting-base dimensions and mold defaults.
- Break long feature bodies into focused helper functions.
- Standardize operation IDs, query names, comments, validation, and error messages.
- Correct stale comments and coordinate-language inconsistencies.
- Preserve the current geometry during this phase.

### Phase 3 — Output cleanup and identity

- Hide section curves, rail bodies, points, trimming tools, and other construction geometry by default.
- Add a `Show debug geometry` option where useful.
- Give the completed fin/base body a stable user-facing name.
- Keep stable mold-body names.
- Add persistent attributes to identify generated fin bodies, planform references, seam edges, base type, and mold pieces where FeatureScript supports it robustly.
- Reduce manual selections in Create Mold without making references fragile.

### Phase 4 — Finish the planform template

Support the practical ways users create a fin outline:

- Start from a reasonable default fin and move control points.
- Trace or visually match an existing fin/image using reference geometry.
- Scale an existing planform uniformly by a percentage.
- Adjust base, height, rake/tip X, and edge curvature numerically.

Prefer controlled Bezier geometry over a free-form interpolating spline because the control polygon is easier to constrain and its convex-hull behavior helps prevent spanwise overshoot beyond the maximum-Z tip.

### Phase 5 — Production mold features

- Define pull direction and removal order for every mold piece.
- Detect or expose undercut/release risks.
- Add local draft only where needed.
- Preserve aerodynamic cavity surfaces and nominal fin-box geometry.
- Add keyed alignment pins/sockets between foil halves and base plugs.
- Use separate alignment clearance, cavity allowance, and sealing-land parameters.
- Add hard stops for foil-shell closure and base-plug insertion.
- Add stiff clamp flanges and accessible clamp locations.
- Add controlled resin bleed channels and a collection reservoir.
- Add vent paths from trapped-air regions, especially the tip.
- Add print-bed-aware splitting or orientation options.

### Phase 6 — Export automation

FeatureScript cannot directly export STL files. Create an external script using the Onshape API that:

- Identifies the named mold bodies.
- Exports each body separately as an STL.
- Uses stable filenames.
- Records the originating document/version and export settings.
- Optionally prepares a batch for slicing or simulation.

Keep API credentials out of Git and load them from environment variables or a local ignored configuration file.

### Phase 7 — Future aerodynamic features

- Add a second normalized foil and prove the data-driven architecture.
- Store a foil-specific near-maximum-thickness rail landmark.
- Support asymmetric foils with an explicit orientation convention.
- Consider spanwise foil blending only after the single-foil architecture is validated.
- Add measurements and derived outputs useful for comparison or simulation.
- Consider FCS and other base standards.

## Regression matrix

Run these cases before and after any structural refactor:

| Area | Cases | Pass condition |
|---|---|---|
| Planform | Short, nominal, and tall fins | Closed region; true tip remains maximum Z |
| Planform | Low and high rake | No spline overshoot or inverted edge |
| Loft | All default stations | One valid solid; no twist or backward tip transition |
| Cant | 0, +10, -10, +15, -15 degrees | Root remains flush; no gap or material below mounting plane |
| Base | Both Futures variants | Correct orientation and valid union |
| Alignment | Front and rear modes | Predictable placement and offset direction |
| Oversized root | Root chord greater than 113.5 mm | 5 mm LE overhang rule applied in rear mode |
| Mold | Zero and both cant signs | Four distinct valid mold solids |
| Mold socket | Default 15 mm / 5 mm offsets | Socket open; 10 mm of plug exposed |
| Mold release | Translate each piece along intended pull direction | No obvious geometric lock in the POC configuration |

## Acceptance criteria

### Fin generator

- Editing any planform parameter regenerates all downstream geometry.
- The logical planform control structure does not change when base or height changes.
- Every foil section starts at the solved LE and ends at the solved TE.
- A 100 mm NACA 0010 chord produces approximately 10 mm maximum thickness.
- The loft follows all four guides and terminates at the true maximum-Z tip.
- The result remains one valid solid through the supported cant range.
- Add Base leaves no cant-induced root gap or clash below the mounting plane.

### Mold proof of concept

- The completed fin is preserved.
- Four named mold solids are generated.
- The assembled cavities reproduce the selected master at zero compensation.
- The foil halves split at the geometry-derived LE/TE center plane.
- The base plugs split lengthwise.
- The plug socket is open and insertable.
- The foil halves overlap the plug without extending past the plug bottom.
- External block faces remain suitable for print orientation and fixturing.

### Production readiness

V1 geometry is not production-ready until:

- Base dimensions are checked against measured hardware.
- A printed base is test-fit.
- Mold pieces are printed and dry-assembled.
- Removal order and undercuts are physically checked.
- Draft, clearance, alignment, hard stops, clamping, vents, and bleed are implemented.
- A controlled wet trial is completed and measured.

## Explicit non-goals for the current MVP

- Live network lookup of foil data from FeatureScript.
- User-configurable section count.
- Independent spanwise thickness law.
- Foil twist or toe inside Build Fin.
- Different foil families blended along one fin.
- CFD-driven surface optimization.
- Automatic structural qualification.
- Automatic claims of Futures compatibility before physical fit validation.

## Onshape and repository workflow

Onshape is the source of truth for executable CAD state and regeneration. Git is the source of truth for reviewable text source, documentation, external automation, and tests.

Recommended cycle:

1. Edit `.fs` and Markdown files in VS Code.
2. Review the Git diff.
3. Copy or synchronize the complete FeatureScript into its Onshape Feature Studio.
4. Regenerate the regression Part Studio.
5. Record failures and fix locally.
6. Commit only after the intended Onshape behavior is confirmed.
7. Create named Onshape versions at significant geometry checkpoints.

Do not assume FeatureScript can be compiled or geometrically validated locally. Onshape remains the authoritative compiler and geometry kernel.

## Instructions for Codex contributors

Before modifying geometry code:

1. Read this complete document.
2. Read the complete affected FeatureScript file.
3. Check `git status` and preserve unrelated user changes.
4. State whether the change is intended to preserve or alter geometry.
5. Keep coordinate conventions explicit.
6. Do not replace working queries or references casually; explain any topology-stability tradeoff.
7. Return complete edited files, not disconnected snippets, unless the user asks for a small patch.
8. Update this document when a design decision, implementation status, parameter meaning, or acceptance criterion changes.
9. Never claim compilation or regeneration success without an actual Onshape test result from the user or an authorized integration.
10. Keep credentials, document access keys, and local secrets out of the repository.

## Immediate next action

The next engineering task is a behavior-preserving V2 refactor of the exact working V1 source. Establish the baseline regression results first, then clean up structure, debug visibility, body naming, and persistent identities before adding draft, alignment, or export automation.

