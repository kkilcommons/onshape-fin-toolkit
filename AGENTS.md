# AGENTS.md

## Project

This repository contains Onshape FeatureScript tools for parametric surfboard fin design and mold generation.

The project currently generates a fin from:

* parametric planform geometry
* selectable foil / airfoil geometry
* multiple foil cross-sections
* leading-edge and trailing-edge rails
* lofted fin geometry
* Futures-style fin bases

The next development phase focuses on mold-generation and manufacturing tools.

## Working Style

* Read and understand the existing implementation before making changes.
* Prefer small, incremental changes over large rewrites.
* Preserve working behavior unless the task explicitly changes it.
* Do not refactor unrelated code while implementing a feature.
* Reuse existing helpers and abstractions where practical.
* Keep FeatureScript code readable and reasonably modular.
* Explain significant design decisions when there are multiple reasonable approaches.
* Work through design tasks incrementally rather than attempting an entire development phase in one large change.

## Safety / Change Control

* Do not delete files unless explicitly asked.
* Do not modify files outside this repository.
* Do not install dependencies or external tools without asking first.
* Do not push to GitHub unless explicitly asked.
* Do not create, delete, or modify git branches unless explicitly asked.
* Do not run destructive git commands such as `git reset --hard`.
* Do not discard uncommitted user changes.
* Before making a broad architectural change, explain the proposed change first.

## Git and Checkpoints

Git is used as a checkpoint and history mechanism.

Before substantial edits:

* Check `git status`.
* Preserve existing uncommitted work.
* Understand what changes already exist before modifying files.

During development:

* Keep changes small enough to review with `git diff`.
* Identify natural milestones where the repository is in a useful, working state.
* Suggest commits at reasonable milestones.
* Prefer frequent, focused commits over large commits containing several unrelated changes.
* A completed feature, working sub-feature, meaningful refactor, or verified bug fix is usually a good commit boundary.
* If work is becoming large enough that losing or reverting it would be painful, suggest creating a checkpoint commit.
* Do not create commits automatically unless explicitly asked.

When suggesting a commit:

* Briefly explain why the current state is a useful checkpoint.
* Suggest a concise commit message describing the change.

After edits:

* Review `git diff` for accidental or unrelated changes.
* Summarize which files changed.
* Describe the important changes.
* Mention anything that still needs to be tested manually in Onshape.
* If the current state represents a useful milestone, suggest committing it.

## Documentation

`README.md` should represent the current state of the project rather than only the original design.

When completing development work:

* Check whether the task affects anything documented in `README.md`.
* Update `README.md` when a feature described in the design plan has been completed, changed, removed, or superseded.
* Mark completed design tasks appropriately.
* Update descriptions when the implementation differs from the original design.
* Add newly discovered constraints or architectural decisions when they are important for future development.
* Keep planned work clearly distinguishable from implemented functionality.
* Do not claim a feature is complete until the implementation has reached the appropriate working state.
* Keep documentation changes in the same logical development milestone as the code they describe.

The goal is that someone opening the repository can use `README.md` to understand:

* what currently works,
* the basic architecture,
* the current development status,
* what remains planned,
* and any important limitations.

## FeatureScript

This project uses Onshape FeatureScript.

When modifying FeatureScript:

* Follow existing naming and formatting conventions.
* Prefer explicit geometry construction over unnecessarily clever abstractions.
* Keep geometry-generation stages logically separated.
* Preserve parameter units and dimensional correctness.
* Be careful with coordinate-system assumptions and X/Y/Z orientation.
* Avoid hard-coded geometry values when an existing parameter should control them.

## Fin Geometry Conventions

Unless the existing code indicates otherwise:

* Fin height is primarily along Z.
* Base/chord direction is primarily along X.
* Foil thickness extends across the transverse direction.
* Leading-edge and trailing-edge geometry define the planform.
* Cross-sections are used to construct the foil/loft.
* Tip geometry should converge cleanly without introducing unstable loft behavior.

Inspect the existing implementation before assuming coordinate conventions.

## Current Development Priorities

The existing V1 fin builder is considered a working checkpoint.

V2 development should focus on:

1. Mold-generation tooling.
2. Mold alignment/registration features.
3. Appropriate mold draft angles.
4. Better separation of mold-generation operations into dedicated features/tools.
5. Hiding debug/construction geometry by default.
6. Eventually automating export of printable mold components.

Do not sacrifice the working fin-generation pipeline while adding these capabilities.

## Definition of Done

For a code change:

1. Existing behavior unrelated to the task remains intact.
2. The implementation is reasonably modular and understandable.
3. No unrelated files were changed.
4. `git diff` has been reviewed for accidental changes.
5. Any behavior that must be manually verified in Onshape is clearly identified.
6. Relevant `README.md` documentation reflects the new state of the project.
7. If the change represents a useful working milestone, a commit should be suggested.
