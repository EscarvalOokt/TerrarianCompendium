# Terrarian Compendium testing policy

This document defines the automated testing and runtime acceptance boundaries for Terrarian Compendium.

## Testing principle

Automated tests should protect meaningful project-owned contracts and realistic regressions. Test count and broad method-level coverage are not goals by themselves.

## Automated test boundary

The NUnit-based `TerrarianCompendium.Tests` project contains deterministic tests for behavior owned by Terrarian Compendium.

Good candidates for deterministic tests include project-owned parsing, serialization, filtering, ordering, migration, and rule behavior when they can be exercised without reproducing Terraria runtime semantics.

## Runtime acceptance boundary

Behavior that depends on the complete Terraria, TerrariaModder, XNA, or Harmony runtime should be validated separately in game rather than reproduced through fragile simulations.

Examples may include:

- whether the mod is discovered and loaded by the current TerrariaModder version;
- lifecycle and configuration integration;
- Harmony patch application when the project uses Harmony;
- gameplay behavior whose semantics are owned by Terraria;
- inventory or entity mutation in the live game;
- Vanilla Terraria UI / TerrariaModder in-game hosting, input, clipping, and integration behavior.

## Release candidate validation

A release candidate is validated through the existing project boundaries rather than through a separate simulated runtime or package-testing framework.

The release validation sequence is:

1. perform a clean Release build;
2. run the full automated test suite against that clean build;
3. assemble the minimal runtime package from the verified Release output;
4. inspect the package contents for unintended build, test, local-configuration, symbol, or runtime-dependency artifacts;
5. install that exact package and perform the required in-game acceptance scenarios.

Runtime acceptance must exercise the packaged candidate rather than relying on development deployment output. If production code or package contents change after acceptance, the clean Release build and full automated suite must be repeated, followed by the runtime and package checks affected by that change before the candidate is considered release-ready again.

## Adding tests

Add tests together with project-owned behavior when they protect a meaningful contract. Keep runtime acceptance scenarios separate when the relevant semantics are owned by Terraria or TerrariaModder rather than this project.