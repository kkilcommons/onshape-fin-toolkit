# Onshape Fin Toolkit — Design and Project Context

## Document purpose

This file is the source of truth for the `onshape-fin-toolkit` repository. It records the intended workflow, coordinate conventions, implemented V1 behavior, manufacturing assumptions, validation status, and V2 backlog.

`README.md` covers how to use the tools and how they work. This document maintains the broader technical context, decisions, development status, regression checklist and results, and next steps. Contributor rules live in `AGENTS.md`.

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

Local Onshape target mappings are stored in gitignored `onshape.local.md`, with a tracked `onshape.example.md` template. This supports the local-edit, explicit-upload workflow without committing personal document IDs. The mapping does not configure MCP test targets or provide automatic synchronization. On 2026-09-22, both mapped Feature Studios were read successfully through MCP and matched their local source files after normalizing line endings and trailing whitespace. The active Part Studio remains unverified; the available MCP tools do not expose targeted read-only Part Studio inspection. Source equality does not establish regeneration success.

V1 is a working end-to-end geometric prototype in Onshape.

On 2026-09-20, the user confirmed that the current `fin_generator.fs` and `fin_mold.fs` are the working baseline and that regression tests passed. See the Regression matrix section for the manual Onshape checklist and validation record.

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

The confirmed V1 baseline uses these repository files:

```text
fin_generator.fs
fin_mold.fs
```

`fin_generator.fs` contains three exported features:

- `Fin Planform`
- `Build Fin`
- `Add Base`

`fin_mold.fs` contains the standalone mold feature:

- `Create Mold - 4 Piece POC`

These replace the prototype filenames `FinGenerator_step18_cant_root_trim.fs` and `CreateMold_four_piece_poc.fs`. Preserve baseline behavior during the initial V2 refactor.

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

- Completed per user confirmation on 2026-09-22: V1 baseline preparation, preservation, V2 workspace setup, and source checkpointing are done. No further V1 setup gate is required before refactoring.
- Baseline regression tests were reported passed by the user on 2026-09-20. See the validation record below; this remains user-reported validation rather than an independent geometry test.

### Phase 2 — Behavior-preserving refactor

- Completed: stable repository filenames are `fin_generator.fs` and `fin_mold.fs`.
- Implemented locally, pending Onshape validation: extract shared boundary classification, root-reference, and side-identification helpers in `fin_generator.fs`. Preview, Build Fin, and Add Base retain their original selection queries and feature-specific validation.
- Implemented locally, pending Onshape validation: centralize span stations, foil sample stations, thickness ratio, and rail landmark data. `fin_generator.fs` now defines shared constants consumed by the guide preview and Build Fin, preserving the exact numeric values and sampling order.
- Implemented locally, pending Onshape validation: centralize mounting-base dimensions in `FUTURES_BASE_DIMENSIONS` and `getFinBaseDimensions`, with separate named constants for placement overhang and construction margins. Mold defaults and construction stages are also refactored locally, pending Onshape validation; see the validation record.
- Implemented locally for Create Mold, pending Onshape validation: extract parting-reference, base-plug, foil-side, open-socket, and naming helpers. Further generator helper extraction remains a possible later cleanup.
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

This is the manual Onshape regression checklist, not an automated local test suite.

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

### Validation record

The mold refactor centralizes user-facing defaults in `MOLD_DEFAULTS` while retaining all existing parameter names, bounds, units, and default values. Named constants separately preserve the 0.5 mm tip-join tolerance, 0.1 mm minimum reference length, 0.000001 parting-normal threshold, and 1 mm socket-cutter margin. No new manufacturing features were added.

`createFourPieceMold` now validates the selections and height/overlap inputs, resolves the parting reference, creates and cuts the base plugs, creates/splits/cuts the foil sides, opens the plug socket, and names the bodies. Helpers receive the original feature ID unchanged so operation IDs and body queries remain stable. The existing closest-endpoint reference search and `cutMoldCavity` helper are reused. The socket cutter is retained for Side A and consumed on Side B; the nominal fin remains available after all four cavity cuts. Error text and validation order are preserved, and comments explain the stage boundaries and coordinate assumptions.

Local source comparisons confirmed that all four extracted geometry blocks match the previous implementation after expanding tolerance/margin constants and ignoring comments/whitespace. Operation-ID expressions, error messages, and all four stable body names matched; default values and bounds were reviewed and `git diff --check` passed. This is not compilation or geometry validation. No Onshape API calls or uploads were made. `README.md` needs no update because the workflow and controls are unchanged. The user requested a Git tracking checkpoint before Onshape validation, as with generator checkpoint `f60e19a`.

At combined V2 validation, compile both Feature Studios and run the regression matrix. For the mold, verify four named solids plus the preserved fin, correct seam orientation at zero/positive/negative cant, the lengthwise base split, an open bottom socket, 10 mm default plug exposure, and world-aligned outer walls. Check both base variants, valid nonzero split offsets, and the existing errors for invalid selections, root split height, overlap, disconnected tip edges, degenerate reference geometry, and out-of-range plug splits. Draft, fit compensation, alignment, clamps, vents, and bleed remain future features.

The third local V2 increment centralizes mounting-base dimensions without intended geometry changes. Shared dimensions remain 113.5 mm length, 7.0 mm thickness, rear-slot center X/depth of 3.3/6.4 mm, 2.2 mm slot radius, and 2/1.5 mm bottom/slot fillet radii. The side/front variant retains 17.7 mm depth, 5.5 mm retention-mouth top, depth minus 2.1 mm mouth bottom, and 2.1 mm front inset; the center/rear variant retains 12.7 mm depth, 4.5 mm mouth top, depth minus 0.7 mm mouth bottom, and 0.7 mm inset. Mouth-bottom offsets and front insets remain independent dimensions despite equal tuned values. The selector preserves the existing variant branch behavior.

Placement overhang remains 5 mm. Cutter, root-extension, and root-trim margins remain separate 1 mm constants; they are construction allowances, not mounting-interface compensation. A local source comparison expanded the new dimension references back to their original expressions and restored the relocated dimension-selection block, reproducing the complete pre-increment file exactly after line-ending normalization. Operation IDs, query expressions, and error messages also matched. The relocated values and both variant branches were reviewed, and `git diff --check` passed. No Onshape calls, uploads, compilation, or regeneration were performed for this increment. All three V2 increments still require the combined regression checks below; usage is unchanged, so `README.md` and `fin_mold.fs` remain unchanged.

On 2026-09-26, the second local V2 increment extracted `classifyPlanformBoundary`, `getPlanformRootReference`, and `identifyPlanformSides`. The user explicitly deferred upload and Onshape validation to batch this with the sampling-constants increment and conserve API allocation. No Onshape MCP calls were made for this helper increment.

The helpers preserve the 0.000001 m root-height tolerance, boundary query order, last matching root/side selection, nearest-root-X side classification, and the leading edge's high endpoint as the loft tip. Callers retain their existing error messages and validation order: Build Fin requires three boundaries and two side edges; Add Base only requires a root edge; preview still uses the sketch's known root X coordinates. Equal-X root endpoints retain their original order as in Add Base; Build Fin still rejects that degenerate selection at side identification because both root-X distances tie. No new geometry support or validation rules were added.

Local source comparisons confirmed unchanged operation-ID expressions, query constructor expressions, regeneration errors, foil-section generation, loft construction, base construction, and cant/placement operations relative to the pre-extraction local file. The combined diff was reviewed and `git diff --check` passed. These are source checks, not FeatureScript compilation or kernel validation. At the eventual authorized upload, test preview off/on, nominal/short/tall and low/high-rake planforms, both base types and alignments, signed offsets, oversized-root placement, supported cant cases, and downstream molds using the regression matrix. Also check invalid selections still report the existing errors. `README.md` needs no change because usage is unchanged; `fin_mold.fs` is unchanged.

The first V2 increment centralizes generator sampling data only. Local diff review confirms unchanged station values/order, thickness ratio, rail fraction, operation IDs, queries, and FeatureScript 3070 imports. Compilation and geometry regeneration have not been tested for this edit. At the next authorized upload, check guide preview on/off, regenerate Build Fin for nominal/short/tall and low/high-rake planforms, and run the regression matrix for downstream base and mold behavior. `fin_mold.fs` is unchanged. One read-only FeatureScript notes call was used before editing (conversation MCP count now 6).

On 2026-09-22, two read-only `get_featurescript` calls verified access to the mapped generator and mold Feature Studios. Both matched the repository source after normalizing line endings and trailing whitespace. No code was uploaded, no geometry was changed, and no Part Studio regeneration was tested. The conversation's Onshape MCP call count is 5, including the earlier two usage checks and snippet evaluation; the next scheduled usage report is due at call 50.

| Baseline | Result | Evidence |
|---|---|---|
| Current `fin_generator.fs` and `fin_mold.fs`, confirmed 2026-09-20 | Regression tests passed | User report; no independent agent execution. Individual case results and an Onshape version reference were not supplied. |

Repeat this checklist after the refactor and record the outcome here. The baseline regression result does not establish physical fit or manufacturing readiness; those remain subject to the production-readiness criteria below.

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

V2 Phase 2 is active. The user confirmed V1 work is done and explicitly deferred SimScale on 2026-09-22. Onshape integration and baseline source access are verified; further integration setup is not a prerequisite for the refactor.

The generator refactor is committed as tracking checkpoint `f60e19a`; the user subsequently requested the mold-default and construction-stage refactor plus its own tracking commit before validation. Both tools are now ready for combined Onshape validation. None of these refactors is compiled or geometrically validated yet. At the next authorized upload, re-read both target Feature Studios to check for Onshape-side edits, then compile and validate against the regression matrix and mold checks in the validation record. Record actual results before marking the refactors validated. Start with nominal planform preview off/on, Build Fin, and both base variants; then cover planform extremes, alignment and offsets, oversized-root placement, signed cant, and all four mold bodies. New manufacturing features remain deferred.

SimScale integration is deferred to the longer-term simulation workflow; no connection or automated simulation has been implemented. Debug visibility, body naming, and persistent identities follow the refactor in Phase 3, before draft, alignment, or export automation.
