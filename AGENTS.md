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

## Onshape MCP API Usage

* Consult `onshape.local.md` for local file-to-Feature-Studio mappings and the active Part Studio before working with Onshape. If missing, use `onshape.example.md` as the setup template.
* Keep personal target URLs and IDs in the ignored local file rather than tracked documentation. Mappings do not authorize uploads; read the current Feature Studio and check for Onshape-side edits before an authorized upload.
* Track Onshape MCP calls during the conversation, including failed calls and usage checks; preserve the count across context compaction.
* After every 50 Onshape MCP calls, call `get_api_usage` and report API requests used, the allocation limit, and remaining requests. Count this reporting call toward the next interval.
* Distinguish MCP calls from API requests: one MCP call can consume multiple API requests. Use the usage tool's reported figures rather than estimating usage from the call count.
* If the usage check fails or figures are unavailable, report that limitation without inventing values.

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
* Suggest a concise commit title and a clear explanatory body using the style below.

Commit message style:

* Use a short, imperative title beginning with a verb such as `Add`, `Fix`, `Update`, or `Remove`, with no trailing period.
* Aim for about 50 characters in the title, but prioritize clarity over a strict length limit.
* Describe the concrete change. Do not use prefixes such as `feat:` or `fix:` unless the user requests that convention.
* Prefer including a body, separated from the title by a blank line. Err on the side of clarity rather than omitting useful context for brevity.
* Explain what changed and why so a future reader can understand the commit without the conversation. Include relevant design decisions, constraints, validation results, and remaining manual checks when applicable.
* Keep the body factual and proportional to the change; do not claim tests or verification that were not performed.

After edits:

* Review `git diff` for accidental or unrelated changes.
* Summarize which files changed.
* Describe the important changes.
* Mention anything that still needs to be tested manually in Onshape.
* If the current state represents a useful milestone, suggest committing it.

## Documentation

`README.md` explains how to use the tools and how they work.

`design.md` tracks project context, architecture and design decisions, implementation status, constraints, validation and regression results, planned work, and next steps. Keep it up to date as development progresses. It is the authoritative project planning and status document.

When completing development work:

* Check whether the task affects either document.
* Update `README.md` when tool usage or explanations of how the tools work change.
* Update `design.md` when features are completed, changed, removed, or superseded; mark completed design tasks appropriately.
* Record design decisions, newly discovered constraints, validation results, remaining work, and next steps in `design.md`.
* Update design descriptions when the implementation differs from the original design.
* Keep planned work clearly distinguishable from implemented functionality in `design.md`.
* Do not claim a feature is complete until the implementation has reached the appropriate working state.
* Keep documentation changes in the same logical development milestone as the code they describe.

Someone opening the repository should use `README.md` to learn the tools and `design.md` to understand project status, technical context, validation, and future work. Keep the roadmap and next steps in `design.md` rather than `README.md`.

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

Follow the current development plan and immediate next action in `design.md`. Preserve the working fin-generation pipeline while implementing that plan.

## Definition of Done

For a code change:

1. Existing behavior unrelated to the task remains intact.
2. The implementation is reasonably modular and understandable.
3. No unrelated files were changed.
4. `git diff` has been reviewed for accidental changes.
5. Any behavior that must be manually verified in Onshape is clearly identified.
6. `design.md` reflects relevant design, status, validation, and next-step changes; `README.md` reflects any changes to tool usage or how the tools work.
7. If the change represents a useful working milestone, a commit should be suggested.
