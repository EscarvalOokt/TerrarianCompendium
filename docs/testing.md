# Terrarian Compendium testing policy

This document defines the automated testing and runtime acceptance boundaries for Terrarian Compendium.

## Testing principle

Automated tests should protect meaningful project-owned contracts and realistic regressions. Test count and broad method-level coverage are not goals by themselves.

## Automated test boundary

The NUnit-based `TerrarianCompendium.Tests` project contains deterministic tests for behavior owned by Terrarian Compendium.

Good candidates for deterministic tests include project-owned parsing, serialization, filtering, ordering, migration, and rule behavior when they can be exercised without reproducing Terraria runtime semantics. Existing UI-host tests exercise Compendium-owned callback mapping, live focus/transient queries, Details-before-section popup closing, wheel-region decisions and cleanup forwarding. They do not prove native UI deactivation or TerrariaWindowKit coordinator behavior; these remain separate runtime/toolkit validation boundaries.

## Development build and source dependency

The production project targets .NET Framework 4.8 and imports TerrariaWindowKit Core and shared sources from a sibling checkout via `TerrariaWindowKit.Source.projitems`. The 1.4.0 development configuration uses WindowKit `v1.1.0`, but the source import does not pin that checkout; identify the actual Compendium and WindowKit source revisions for a validation run. See the [README development setup](../README.md#development) for the checkout, compiler and local properties instructions.

From the Compendium repository root:

```powershell
dotnet build src/TerrarianCompendium/TerrarianCompendium.csproj -c Release -p:DeployToGame=false
dotnet test tests/TerrarianCompendium.Tests/TerrarianCompendium.Tests.csproj -c Release -p:DeployToGame=false
```

## Runtime acceptance boundary

Behavior that depends on the complete Terraria, TerrariaModder, XNA, or Harmony runtime should be validated separately in game rather than reproduced through fragile simulations.

Examples may include:

- whether the mod is discovered and loaded by the current TerrariaModder version;
- lifecycle and configuration integration;
- Harmony patch application when the project uses Harmony;
- gameplay behavior whose semantics are owned by Terraria;
- inventory or entity mutation in the live game;
- Vanilla Terraria UI / TerrariaModder in-game hosting, input, clipping, and integration behavior.

### WindowKit / Vanilla UI acceptance scenarios

For changes to the production window adapter, apply the affected scenarios in the current Terraria 1.4.5.8 / TerrariaModder Core 0.4.2 / TerrariaWindowKit v1.1.0 configuration:

- **Root lifecycle, geometry and pin:** open/close/reopen with `Alt+I` and the close button, preserved session history/filters and bounds, drag/resize, UI scale, mouse position, in-game options, world transitions and mod unload; check pinned/unpinned close behavior, pin reset on reopen, localized Pin/Unpin button layout, and absence of stale registrations and blockers.
- **Keyboard and transient UI:** search Enter/Escape, text focus acquisition/release, popup-before-root Escape priority with and without pinning, physical key release, F6 availability after focus loss, and keyboard ownership when a higher-priority panel is active.
- **Mouse and wheel:** native content update through the WindowKit Core content-input scope while panel bounds remain registered; clicks on the eligible upper window reach its native controls without reaching lower windows or the game. Check higher-priority foreign-panel blocking, appropriate wheel pass-through/delivery/capture, scroll limits, overlap priority and no second action.
- **Presentation:** correct draw order for Vanilla UI, framework ItemTooltip and project SupplementalTooltip, plus clipping, popup anchoring, localized Pin/Unpin presentation, deferred tooltip suppression when a higher-priority panel overlaps, and cleanup on close.
- **Browser regressions:** all six sections, Details, navigation/history, filters, crafting, Journey duplication, ItemSlot shortcuts, texture loading and persistence as applicable to the change.
- **Independent consumers:** run Compendium with a separately compiled compatible WindowKit consumer and check activation in both directions, overlapping bounds, mouse/Escape/focus/wheel priority (including pinned-root Escape pass-through to an eligible unpinned root), foreign Core panels and independent window close/reopen. An in-assembly probe or synthetic ConsumerA/ConsumerB test does not establish this runtime combination.
- **Initialization and teardown boundaries:** where the runtime permits, check both first-provider initialization orders and disposal/unload of one consumer while another remains active. Window close, world exit, backend disposal and mod unload are distinct; do not claim an unload scenario passed if the runtime cannot perform it.
- **Shared runtime/Harmony diagnostics:** when observable, check the single shared runtime family, leases, shared scroll-patch ownership and last-lease cleanup. WindowKit v1.1.0 content-input Harmony prefixes may be installed separately by independently compiled source copies; they are not the same singleton as the shared scroll runtime. Unavailable diagnostics must remain `NOT TESTED`, not be inferred from a successful visual check.

Report observed results and unchecked branches for the actual build and configuration. Unavailable independent unload or low-level diagnostic scenarios remain explicit limitations rather than automatic blockers or claimed successes when the agreed release acceptance relies on accessible in-game scenarios. This list is a reusable acceptance policy, not evidence that its scenarios have already passed.

## Release candidate validation

A release candidate is validated through the existing project boundaries rather than through a separate simulated runtime or package-testing framework.

The release validation sequence is:

1. perform a clean Release build;
2. run the full automated test suite against that clean build;
3. assemble the minimal runtime package from the verified Release output;
4. inspect the package contents: the intended mod assembly and `manifest.json`, without a separate TerrariaWindowKit DLL, toolkit sources, diagnostic probe configuration, tests, symbols, local configuration, or other build/dependency artifacts;
5. install that exact package and perform the required in-game acceptance scenarios, including single-player and Host & Play scenarios where part of the accepted candidate scope.

Before interpreting package acceptance, distinguish the local source/dependency combination, verified build, package contents and installed runtime configuration. Runtime acceptance must exercise the packaged candidate rather than relying on development deployment output. If production code or package contents change after acceptance, the clean Release build and full automated suite must be repeated, followed by the runtime and package checks affected by that change before the candidate is considered release-ready again.

## Adding tests

Add tests together with project-owned behavior when they protect a meaningful contract. Keep runtime acceptance scenarios separate when the relevant semantics are owned by Terraria or TerrariaModder rather than this project.