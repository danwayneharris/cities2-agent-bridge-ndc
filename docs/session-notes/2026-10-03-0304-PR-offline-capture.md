# Offline capture publication ? 2026-10-03

Publish the existing generic geometry capture changes through `52ceb40`,
including the research at `e2472d1`, from a clean worktree. No runtime source
changes are introduced by this publication task. The paired NetworkTools PR
contains the standalone offline consumer; the bridge contains no consumer-
specific runtime dependency. Native capture, terrain/map observations, bounded
managed finishing experiments and generic research launch flags remain opt-in.

This task does not deploy or manipulate the game. Compile/API checks and PR
links will be recorded below. Historical live qualification remains separate.

## Publication verification

Ordinary and opt-in research builds pass with two existing obsolete updater API
warnings. GeometryResearchCaptureTests passes 29 installed type contracts;
GeometryJobCaptureTests inspects 23 helper methods and native job/map contracts.
The paired standalone consumer validates all eight compiled scheduler signatures
and rejects missing/extra sites. Five captured native pipeline cases and the
consumer's negative suites pass; no game mutation/deployment was performed.

First research build used an obsolete local Harmony path and stopped before
compilation. Resolved the installed lib.harmony 2.2.2 net48 path and reran
successfully. New compile/API evidence does not substitute for live execution.

## Published review links

Companion PR: https://github.com/danwayneharris/CS2-NetworkTools/pull/16

NetworkTools #16 targets main; #15 is stacked above it. Bridge #7 targets its
own main. They can be reviewed independently and have no assembly/release
dependency. Archive branch: `archive/combined-before-offline-split-20261003`
in NetworkTools preserves the complete pre-extraction feature/research history.
