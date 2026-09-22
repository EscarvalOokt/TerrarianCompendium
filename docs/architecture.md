# Terrarian Compendium architecture

This document defines the accepted product architecture and implementation boundaries of Terrarian Compendium.

It is an architecture specification, not an implementation plan. The current production browser has working Items, Armor Sets, Recipes, and Bestiary domains, shared Details, typed cross-domain navigation, Back/Forward history, contextual Item↔Recipe exploration with requirement-aware and collection-aware Recipe filtering, dedicated Item acquisition indices including potential merchant stock, native-compatible direct crafting, versioned persistent recipe identity, client-local recipe favorites, culture-aware vanilla Item text, project-owned English/Russian browser localization, and a stabilized compact retained-UI presentation.

## Sources of truth

1. The installed Terraria and TerrariaModder runtime define external runtime behavior and API contracts.
2. The current production implementation defines behavior actually available in the built mod.
3. This document records accepted Terrarian Compendium architecture decisions and the current compatibility and product boundaries.
4. `docs/testing.md` defines automated testing and runtime acceptance boundaries.

When documentation and production code disagree about current project behavior, production code is the source of truth until the discrepancy is resolved explicitly.

Implementation order and unresolved product decisions are outside the scope of this architecture specification.

## Product scope

The current product scope is a vanilla Terraria compendium centered on:

- collection tracking and discovery;
- item browsing, filtering, sorting, progress, and details;
- functional vanilla armor-set browsing, set details, and Item↔Armor Set relations;
- recipe-result browsing, recipe details, current craftability, direct crafting, requirement/collection filtering, and Item↔Recipe relations;
- local recipe favorites backed by a versioned persistent recipe identity;
- vanilla NPC/loot browsing and Bestiary details;
- additional item relations that are explicitly researched and accepted for the current scope;
- project-owned browser localization synchronized to the active Terraria culture, with English as the source/fallback locale and Russian as the current release translation.

The following are not required for the current scope:

- recursive Craft Planner / CraftPath;
- automatic multi-step ingredient expansion and path optimization;
- shops, mining, catching, and other acquisition sources as planner nodes;
- nearby/portable-storage material planning;
- multiplayer/team planner or favorites synchronization;
- Magic Storage and custom-content/provider ecosystems.

## Current compatibility contract

The current compatibility boundary is frozen to Terraria 1.4.5.8, TerrariaModder Core 0.4.1, .NET Framework 4.8, and the vanilla-only product scope described above. Moving to another Terraria/TerrariaModder target or changing these compatibility guarantees requires a separate compatibility decision and revalidation.

Persistence and migration contracts:

- collection progress is project-owned per-character sidecar state; its persistence identity includes the normalized active player save path and `IsCloudSave`, so Local and Cloud representations belong to separate identity domains;
- automatic collection-sidecar migration or merge is not guaranteed when that identity changes, including Local↔Cloud transitions, save-path changes, or equivalent moves/renames;
- the controlled legacy collection migration from the old `item-checklist` product root to `terrarian-compendium` remains the supported project-owned migration path;
- recipe favorites are client-local state rooted in `Main.SavePath`, shared across characters using that save root, and are not part of per-character collection persistence.

Multiplayer/runtime ownership contracts:

- browser/session/filter/navigation state remains client-owned, and the dedicated-server composition does not create the browser session/UI stack;
- Terrarian Compendium does not introduce a separate synchronization protocol for collection, favorites, navigation, filters, or other browser-owned state;
- `CraftingAvailabilityState` is a local active-player runtime projection rather than persisted or project-networked state;
- direct crafting performs project validation and delegates execution to Terraria's `CraftingRequests` path; Host & Play runtime acceptance covers inventory-only and currently open vanilla chest contexts for this target;
- Terraria-owned Journey and Bestiary state is consumed through the native runtime contracts and is not duplicated into a parallel Terrarian Compendium persistence/network layer.

Merchant compatibility contract:

- `MerchantSourceIndex` remains a static/content-lifetime potential Merchant↔Item relation for the current target;
- a relation means that the NPC can sell the Item in a confirmed vanilla state, not that the Item is currently present in the live shop;
- current shop availability and ordinary live coin prices remain outside the static relation contract.

These boundaries are part of the current architecture. Expanding them requires an explicit release/compatibility decision rather than being treated as an incidental implementation detail.

## Current implementation baseline

The current implementation provides the following architecture-relevant baseline:

- product/package/project identity `Terrarian Compendium` / `terrarian-compendium` / `TerrarianCompendium` on .NET Framework 4.8, targeting Terraria 1.4.5.8 and TerrariaModder Core 0.4.1;
- immutable content-lifetime Item, Armor Set, Recipe, NPC, acquisition, taxonomy, sorting, and persistent-recipe-identity data, replaced as complete snapshots when refresh is required;
- per-character collection state with versioned sidecar persistence, controlled legacy migration, validated-backup recovery, discovery from supported player/storage sources, and optional Terraria-owned Journey integration;
- a production Item Browser with independent navigation/search/taxonomy/completion/research/crafting/NPC-drop/merchant-source filters, eight taxonomy facets, deterministic sorting, localized Item text, and compact Item Details backed by immutable Details-only base stat metadata and shared icon/value/coin presentation;
- dedicated immutable `WorldLootSourceIndex`, `OpenableItemLootIndex`, `FishingSourceIndex`, and `MerchantSourceIndex` relations consumed by Details/browser projections without changing collection discovery ownership;
- bidirectional Openable Item relations, direct world-source relations for researched chest/pot/tree-shaking acquisition paths, Fishing relations that preserve separate vanilla rule variants with normalized rule conditions and stopper-derived reachability restrictions, and bidirectional potential Merchant↔Item stock relations with normalized sale variants and availability restrictions;
- immutable `ArmorSetCatalog` / `ArmorSetIndex` preserving exact native variants and supporting Item↔Armor Set relations and Armor Set Details;
- immutable `RecipeCatalog` / `RecipeIndex`, versioned `RecipePersistentKey` identity, session-owned current craftability, a global result-item Recipe catalog plus contextual Item-driven `Used in` projection, producing-variant Recipe Details, reusable direct crafting from Item/Recipe Details, requirement/collection/favorite filtering, and client-local persistent favorites;
- immutable `NpcCatalog` / bidirectional `NpcLootIndex`, Terraria-owned encounter progress, Bestiary browsing/filtering/sorting including potential merchant-stock filtering, and NPC Details with static potential loot, native kill/banner statistics, plus merchant-stock projection where applicable;
- a shared `BrowserNavigationState` with typed Item, Armor Set, Recipe-query, exact Recipe, NPC, and section-root destinations plus runtime Back/Forward history;
- optional vanilla Item-slot navigation gated by the client-side `Inventory Item Navigation` setting: fixed `Alt + Right Click` on explicitly supported inventory/storage/equipment/shop surfaces opens the ordinary Item destination through the same navigation owner and consumes the accepted native RMB gesture before `ItemSlot.Handle(...)` processing;
- a shared `BrowserDetailsSurface` that hosts separate domain-specific Item, Armor Set, Recipe, and NPC projections rather than a universal Details model;
- one shared Vanilla/Core UI host adapter for Terraria retained UI inside TerrariaModder-owned lifecycle, panel, input, tooltip, and top-level integration boundaries;
- revision-based invalidation for mutable session/browser owners and deterministic projections, while static relations remain in dedicated immutable catalogs/indices;
- project-owned player-facing browser text externalized through embedded locale resources, with `en-US` as the source/fallback locale, `ru-RU` as the current release translation, and culture changes exposed through a localization revision consumed by UI/model projections;
- project-owned automated tests for deterministic contracts, with Terraria/TerrariaModder/XNA semantics validated through runtime acceptance.

Recipe destinations continue to use runtime recipe identity for navigation. Persistent recipe state uses the separate versioned `RecipePersistentKey` contract described below.

## Composition and lifecycle

`TerrarianCompendiumMod` remains the composition root.

Its responsibilities include:

- obtaining TerrariaModder configuration and logging;
- registering lifecycle/frame callbacks and keybinds;
- creating and retaining the project-owned localization service and synchronizing it with the active Terraria culture;
- creating retained content-lifetime catalogs and indices;
- creating world/character browser-session state;
- creating browser-composition state such as Recipe and Bestiary filters;
- constructing domain-specific browser/detail models and views;
- wiring the shared browser shell and navigation state;
- disposing world/character state and UI composition at the appropriate lifecycle boundary.

Content structures that do not depend on the active character/world remain retained where the current implementation already treats them as content-lifetime data. Mutable character/world/runtime progress remains session-owned. Mutable browser-query/filter state has an explicit browser-composition owner rather than being moved into immutable catalogs or navigation destinations.

A separate dependency-injection container is not required by the current architecture.

## Ownership and lifetime boundaries

### Content-lifetime data

Content-lifetime data is immutable or replaced as a complete snapshot.

Current examples include:

- `ItemCatalog`;
- `ItemTextIndex` snapshots;
- `RecipeCatalog`;
- `RecipeIndex`;
- `RecipePersistentKeyIndex`;
- `ArmorSetCatalog`;
- `ArmorSetIndex`;
- `NpcCatalog`;
- `NpcLootIndex`;
- `WorldLootSourceIndex`;
- `OpenableItemLootIndex`;
- `FishingSourceIndex`;
- `MerchantSourceIndex`;
- immutable taxonomy/sorting/details metadata;
- immutable station-display mappings derived from the current catalogs.

Static relations belong in dedicated catalogs/indices rather than being added to `ItemCatalogEntry` as mutable or cross-domain state.

### Mutable session, client-local, and browser-composition state

Mutable runtime/browser state belongs to one explicit owner.

Current session-owned examples include:

- `ChecklistState`;
- optional `JourneyResearchState`;
- `CraftingAvailabilityState`;
- discovery scanners and persistence-session bookkeeping.

Client-local mutable state includes `RecipeFavoriteState`. It is retained independently from per-character collection/session state, stores `RecipePersistentKey` identities rather than runtime recipe indices, and exposes revision-based invalidation to its consumers.

Current browser-composition state includes `RecipeFilterState`, which owns Recipe filtering criteria and revision state independently of browser destination/history identity, and `BestiaryFilterState`, which owns one native Bestiary criterion, one loot-aware drop criterion, encounter filtering, the project-owned merchant-stock filter, and revision state independently of NPC destination/history identity. Native and loot-aware criteria are mutually exclusive within their own groups but can be active together. `BestiaryBrowserModel` separately owns search text, sort mode, and sort direction. Recipe result-item category navigation is owned separately by `RecipeBrowserModel` through its own `ChecklistNavigationFilter`; it is independent from the Item Browser category state, `RecipeFilterState`, and browser history identity. The contextual Recipe Item is carried by Recipe-query/exact-Recipe destinations and synchronized into the Recipe Browser as a derived catalog context; it is not another `RecipeFilterState` criterion or category-navigation owner.

Models and views consume these owners; they do not become alternative owners of the same state.

Revision-based invalidation is the preferred current pattern for mutable state consumed by deterministic projections.

## Localization architecture

`CompendiumLocalization` is the project-owned localization service for Compendium browser/player-facing text. It is created and retained by `TerrarianCompendiumMod` for the content lifetime and is synchronized to `Language.ActiveCulture`. A culture-name change updates the active project locale and increments `Revision`; consumers use that revision as an invalidation signal rather than treating locale as navigation, filter, or persistence state.

Project locale resources are flat JSON dictionaries embedded in the main assembly. `en-US` is the required source locale and fallback contract. `ru-RU` is the current additional release locale. If an exact project locale resource is unavailable, the source locale is selected. If an active translation does not contain a usable key, lookup falls back to the source entry; if the source entry is also unavailable, the key itself is returned. Formatted translations are used only when their placeholder-index set is compatible with the source template; incompatible or invalid translated formatting falls back to the source template.

Localization ownership remains split by source of the text. Project-owned browser labels, descriptions, filters, conditions, tooltips, and similar presentation text use `CompendiumLocalization`. Terraria-owned Item/NPC names, Item base descriptions, Armor Set bonus text, native Bestiary/environment labels, and other native display text remain resolved through the existing Terraria localization paths rather than being copied into project locale resources.

The current project-owned localization scope does not include TerrariaModder configuration `[Label]` / `[Description]` metadata, keybind metadata, or manifest/package description text. Those surfaces are outside the accepted browser localization contract rather than alternate localization owners.

## Item architecture

### Catalog and taxonomy

`ItemCatalog` is the immutable base item catalog.

The current base-catalog inclusion contract uses positive vanilla Item IDs (`1 .. ItemID.Count - 1`) as candidates. An entry is included only when the target ID is not marked by Terraria as `ItemID.Sets.Deprecated` or `ItemID.Sets.ItemsThatShouldNotBeInInventory`, `SetDefaults` preserves the requested Item identity, and the resulting Item name is non-blank. Journey research eligibility, recipe presence, and loot/acquisition relation presence do not define base-catalog membership.

The project intentionally keeps different identity/taxonomy concepts separate:

- native Journey category membership;
- canonical project semantic membership;
- browser navigation-node identity;
- taxonomy-facet presentation identity.

They must not be collapsed into one universal hierarchy.

Contextual `Other` is derived as a residual of the current browser context rather than stored as a canonical semantic membership.

Item category navigation uses the shared category-navigation control. Ordinary upward navigation follows taxonomy parents; its Alt-modified upward action clears the Item category scope and returns directly to the Items section root through the existing `BrowserNavigationState`.

### Collection and discovery

Collection progress is per-character project state.

Discovery can consume supported runtime sources such as:

- inventory;
- equipment and loadouts;
- the currently open supported vanilla storage.

Discovery updates collection state but does not redefine the base item universe.

Journey research remains Terraria-native state. The project may infer `Found` conservatively from confirmed research progress, but does not replace or duplicate Journey ownership.

The shared semantic Item presentation for a concrete Item consumes the optional `JourneyResearchState` through the existing research-presentation adapter and exposes fully researched status without becoming a second research-state owner. Semantic Item surfaces reuse that presentation consistently, while representative or decorative Item textures may opt out when they illustrate another concept rather than the concrete Item entity.

For fully researched Items in a Journey-compatible context, the same shared semantic Item presentation owns the Compendium interaction policy for native duplication: `Alt + Left Click` requests the native stack duplication behavior, including `OnlyNeedOneInInventory()` semantics, while `Alt + Right Click` requests native one-by-one duplication and preserves Terraria's repeat cadence while held. Execution is delegated to the confirmed Journey context-29 path; Terrarian Compendium does not implement a parallel item-grant, Journey persistence, or networking contract.

### Search and sorting

Item names are always searchable. Base item description text is an explicit opt-in search dimension.

Culture-sensitive vanilla Item names and base descriptions belong to the content-lifetime `ItemTextIndex`. The index is replaced atomically for the active Terraria culture and exposes a revision consumed by search, sorting, Details, and other player-facing Item-name projections. `ItemCatalogEntry.Name` remains bootstrap/fallback metadata rather than the live localized UI source. Dynamic player/world-dependent tooltip output is outside this immutable text snapshot contract.

Common item sorting modes are `Native`, `Item ID`, `Name`, `Value`, and `Rarity`.

Context-specific sorting exposes only metrics meaningful to the current navigation scope, including weapon damage, armor defense, and applicable tool/fishing power metrics.

Sorting direction is independent from sort mode. Ties are deterministic and preserve native ordering where applicable.

Special Quest/Expert/Master rarity values use the project-owned normalized rarity ordering rather than ordinary negative numeric ordering.

### Item filtering

The current deterministic Item Browser filtering pipeline preserves the established scope semantics:

1. search;
2. navigation;
3. taxonomy facets;
4. scope totals;
5. completion;
6. Journey research;
7. crafting;
8. NPC-drop source (`NPC drops`);
9. merchant-source (`Purchasable`);
10. sorting.

Completion/research/crafting/NPC-drop/merchant-source filters therefore do not redefine the filtered-scope denominator. `NPC drops` is a static source-existence predicate backed by `NpcLootIndex.HasNpcSourceForItem(...)`; conditional static drop relations count as sources. `Purchasable` is a potential-source predicate backed by `MerchantSourceIndex.ContainsItem(...)`; it does not evaluate current shop availability.

### Item Details stats and presentation

Base Item stats that belong to Details presentation but are not sorting concerns are owned by immutable `ItemDetailsStats` metadata rather than being added to `ItemSortMetrics`. The current contract covers the display damage type, base knockback, applicable base critical-hit chance, base use-time ticks, and positive standalone tag damage.

These values describe the unprefixed base vanilla Item definition used by the content-lifetime catalog. Prefix-adjusted instance values and player/equipment/buff/runtime modifiers remain outside this immutable metadata. Terraria-dependent extraction is performed before the UI projection; Item Details views consume projected project values rather than recomputing Terraria internals.

Damage type is a player-facing Details projection and does not replace overlapping raw Terraria damage-class flags or project taxonomy memberships. Use time remains the base `useTime` value in game-update ticks and is not treated as a complete attack-cycle or attacks-per-second value. Standalone tag damage represents only the positive intrinsic `WhipTagEffect.TagDamage` contribution and does not imply that an Item with no positive standalone value has no tag effect.

### Acquisition relations

Item acquisition data is represented by dedicated immutable relation indices rather than a universal acquisition graph:

- `WorldLootSourceIndex` maps Items to researched direct world-generation/environment sources such as chest groups, pots, and tree shaking;
- `OpenableItemLootIndex` stores direct Openable Item relations in both directions: target Item→openable sources and openable Item→possible direct item outputs;
- `FishingSourceIndex` maps Items to direct vanilla fishing rule variants and preserves normalized required conditions together with stopper-derived excluded reachability restrictions where those restrictions are part of the accepted current source semantics;
- `MerchantSourceIndex` stores the static potential Merchant NPC↔Item stock relation in both directions. Each merchant offer preserves one or more normalized sale variants, where conditions within a variant are conjunctive and variants are alternatives. Separate availability flags represent random stock and native shop-capacity limitations. Confirmed static special-currency metadata may be retained, while ordinary current coin prices are runtime projections and are not part of the static relation.

These relations are static content-lifetime data. They are separate from collection discovery and from runtime current-storage/shop state: discovering an Item in an open container does not redefine a static acquisition relation, a static container/openable relation does not imply that the Item has been discovered, and a merchant relation means that the NPC can sell the Item in at least one confirmed vanilla state rather than that it is available now. Player sellback/buyback entries are not part of merchant potential stock.

Fishing acquisition semantics and user-facing presentation remain separate concerns. The relation layer retains the accepted normalized required/excluded reachability semantics; Details may simplify technical exclusion combinations for presentation when that does not change the underlying relation or invent a stronger native condition.

Relations remain direct rather than transitively flattened. For example, an Item obtainable from an Openable Item that is itself obtainable through Fishing is not automatically treated as a direct Fishing result. Merchant relations likewise do not introduce planner nodes or recursive acquisition semantics.

Item Details consumes these indices through its domain-specific projection. Merchant sources appear as `Purchasable` potential sellers and navigate through the existing NPC destination contract. The Item acquisition presentation identifies sources and possible direct outputs; it does not expose drop rates as part of the Item Details contract or claim current merchant availability. Exact probability presentation remains a separate domain concern where explicitly supported, such as NPC Details.

The existence of these dedicated indices does not introduce planner semantics, recursive acquisition solving, or a general-purpose acquisition framework.

## Armor Set architecture

### Catalog and exact variants

`ArmorSetCatalog` is the immutable project-owned catalog for functional vanilla armor-set relations. It is derived from Terraria's initialized armor-set registry and does not reconstruct set membership from Item names, taxonomy, defense values, textures, or armor slot metadata.

The authoritative membership input is the exact native Head/Body/Legs combination for each registered set bonus. A zero part remains an absence sentinel for a slot that is not required by that concrete set.

Logical catalog entries may group multiple concrete combinations, but the exact variants remain stored as source-of-truth membership. Grouping first separates entries by shared runtime-derived set-bonus semantics, then forms structural connected components where adjacent variants differ in exactly one armor part. Shared bonus text alone does not merge structurally disconnected armor families, and structural overlap alone does not merge entries with different bonus semantics.

`ArmorSetIndex` owns the static reverse Item→Armor Set relation. Armor-set relations do not belong in mutable collection/session state or in `ItemCatalogEntry`.

Armor Set IDs are runtime catalog/navigation identities. They are not a persistence contract and are not used as stable cross-version storage identities.

### Details and presentation

Armor Set Details resolves the vanilla set-bonus description separately from ordinary Item descriptions. Item base descriptions remain owned by `ItemTextIndex`; a set-bonus description belongs to the Armor Set projection.

The Details projection may collapse exact variants into separate Head/Body/Legs option lists only when those lists reproduce the exact concrete-variant universe as a complete Cartesian product. If they do not, Details preserves explicit concrete combinations so the UI does not imply combinations that Terraria did not register.

The representative Item used by Armor Set grid presentation is deterministic presentation metadata: a native primary part takes precedence when present; otherwise the first concrete variant uses `Head`, then `Body`, then `Legs` as fallback. Representative presentation does not redefine set membership or set identity.

## Recipe architecture

### Catalog and relations

`RecipeCatalog` is the immutable vanilla recipe catalog.

`RecipeIndex` owns static item↔recipe relations:

- recipes producing an item;
- recipes using an item.

Recipe-group relations expand to concrete valid members and must not treat a representative display item as the only valid ingredient.

Static recipe relations do not belong in `ItemCatalog`.

### Persistent recipe identity

Runtime recipe indices remain runtime-only identities for navigation and correlation with Terraria's initialized recipe universe. Persisted recipe-owned state uses `RecipePersistentKey` instead.

`RecipePersistentKey` is a versioned canonical structural descriptor. The current v1 descriptor includes:

- result Item ID and result stack;
- ingredient descriptors in recipe order, including display Item ID and required stack;
- ordinary ingredient identity through its canonical Item ID;
- recipe-group identity through the complete sorted set of valid member Item IDs rather than the runtime `RecipeGroup.RegisteredId`;
- required crafting tile, or the absence of one;
- the supported environment requirement flags in a fixed order;
- alchemy state.

The persistent descriptor excludes the raw runtime recipe index, runtime recipe-group IDs, display labels, and dynamic player/world state. Ingredient order remains significant, while recipe-group member order is normalized. The full canonical key string is the persisted identity; aggregate fingerprints are diagnostic values rather than identity.

`RecipePersistentKeyIndex` maps runtime recipe indices to persistent keys and persistent keys back to matching catalog recipes. Exact structural duplicates intentionally share one persistent identity; the runtime index is not used as a persistence tie-breaker.

The v1 persistent-key contract is validated for the current Terraria 1.4.5.8 compatibility target. It is not a general guarantee of cross-version stability: future Terraria/build changes require explicit revalidation and, where necessary, an explicit migration decision before the compatibility scope is expanded.

### Current craftability and direct crafting

`CraftingAvailabilityState` owns current runtime craftability for the active session.

The existing runtime scanner refreshes Terraria-owned adjacency and owned-item crafting inputs, evaluates catalog recipes through Terraria's current environment/material predicates, and replaces the result as a revisioned snapshot. `Main.availableRecipe` is not treated as the authoritative craftability source.

Browser/detail models read this state through the owner; they do not independently recompute Terraria crafting semantics. `ItemCraftingAvailability` provides the deterministic Item→currently-craftable-producing-recipes projection used by the direct-crafting UI without becoming a second craftability owner.

`VanillaDirectCraftingService` is the project-owned action boundary for direct crafting. Before dispatching a selected runtime recipe it validates the recipe against the retained catalog, reuses the existing native crafting-availability execution bridge to refresh Terraria-owned adjacency/material inputs, rechecks the current environment/material/cursor and player crafting guards, and then delegates execution to Terraria `CraftingRequests` with normal (`quickCraft: false`) semantics. Terrarian Compendium does not consume ingredients, grant results, or implement a parallel multiplayer crafting protocol.

`DirectCraftButton` is a reusable Details control shared by Item Details and Recipe Details. It presents only currently craftable producing variants in an anchored `VanillaPopover`; zero available variants preserve the disabled Craft state. The control observes the crafting revision so an open popover can refresh its variants without becoming a mutable crafting-state owner. Craft execution does not close the popover; closing remains part of the normal transient-surface lifecycle. Hold-to-repeat uses Terraria's stack-split/repeat cadence rather than a project-owned repeat timer.

Recursive ingredient solving and multi-step CraftPath behavior are outside the mandatory current architecture.

### Recipe filtering

`RecipeFilterState` owns the mutable Recipe filter criteria and revision for the current browser composition.

The current contract includes:

- one optional crafting-station filter identified by canonical `RequiredTileId`;
- independent environment requirement flags for `Water`, `Honey`, `Lava`, `Snow biome`, `Graveyard biome`, `Mechdusa`, and `Torch God's Favor`;
- `NoRequirementsOnly`, presented to the player as `By hand`, which is mutually exclusive with explicit station/environment requirement filters;
- result-item completion state `All / Missing / Found`;
- optional Journey research state `All / Researched / Unresearched`;
- `Craftable now` as a recipe-level current-runtime predicate;
- `FavoritesOnly` as a concrete-recipe predicate backed by client-local `RecipeFavoriteState`.

Requirement criteria use AND semantics. Completion and research are result-item predicates. Current craftability and favorites are concrete-recipe predicates.

In the global Recipe catalog, a result Item remains visible when the result-item predicates pass and at least one concrete producing recipe satisfies all active recipe-level predicates. Active criteria must be satisfied by the same candidate recipe; separate recipes do not combine partial matches.

When a contextual Item is active, `RecipeIndex` first supplies the concrete recipes that use that Item, including expanded RecipeGroup members. A result Item enters the contextual catalog only when at least one of those contextual candidate recipes satisfies the same filter contract. All active recipe-level criteria must therefore be satisfied by the same contextual candidate recipe; an unrelated producing recipe cannot make the contextual result visible.

Recipe-query Details independently apply the same producing-recipe matching contract to the query Item when it has producing variants. This keeps producing-variant Details consistent with active filters while the contextual catalog remains a `Used in` projection of the same query Item.

Item-name search and result-item category navigation are separate constraints on visible result Items and are not stored in `RecipeFilterState`. Recipe category navigation reuses the existing Item taxonomy, applies to contextual/global result Items, and is not added to `RecipeFilterMatcher`. Neither search, category scope, nor Recipe filters define validity of the contextual Item itself.

Recipe filter state and Recipe result-item category scope are not part of `BrowserDestination` or browser history identity. An active contextual Item remains valid while it belongs to the Item catalog and participates in at least one producing or using Recipe relation; filters or category scope may reduce its contextual result catalog to empty without clearing that Recipe-query destination.

The canonical crafting-station requirement remains the recipe's tile requirement. User-facing station presentation maps that tile to a representative catalog Item where available; raw tile IDs are not a player-facing contract.

### Recipe Browser and Details

Recipe Browser is a domain-specific browser model/view rather than a generic extension of the Item Browser filter model.

At the Recipes section root, the browser universe contains result Items with at least one producing recipe surviving the active Recipe filters. Multiple producing recipes do not duplicate the result Item in the grid. With a contextual Item, the catalog is narrowed to result Items produced by filtered recipes that use that Item.

A Recipe query carries the contextual Item ID. That Item acts as the catalog's `Used in` anchor; independently, when it has producing recipes, Recipe-query Details expose the concrete producing variants surviving the active filter set. An Item that is only an ingredient can therefore own a contextual catalog without producing Recipe Details. Selecting a concrete producing variant opens an exact Recipe destination while preserving the same query context without creating a new history entry for variant cycling itself. If active filters later invalidate that selected variant while another producing variant for the query Item survives, the current exact destination is normalized to the surviving variant through current-entry replacement rather than by creating a new history step.

Exact Recipe Details resolves recipe-group alternatives, crafting-station presentation, environment requirements, alchemy metadata, current craftability, and favorite state without moving ownership of catalog or mutable state into the UI. Ingredient rows use the shared semantic Item presentation with required stack values; permanent Item names are omitted from the row layout and remain available through the Item tooltip. RecipeGroup requirements keep their concrete alternative Item icons and an explicit alternative-group cue. Favorite toggling applies to the selected concrete recipe variant.

The Recipe Browser exposes a separate favorite-only mode through the shared `RecipeFilterState`. A result Item may also present an aggregate marker when at least one of its producing recipe variants is favorited; this presentation does not make the result Item itself the persistent favorite identity.

Navigation from Recipe Details is available for result Items, ordinary ingredient Items, concrete recipe-group alternatives, and an Item-backed crafting-station requirement.

Recipe result-item category navigation reuses the existing Item category/taxonomy semantics and applies them to the result Item. Items and Recipes keep independent category-navigation state. In contextual Recipes, the contextual Item is presented as an enclosing scope above that taxonomy chain without becoming a taxonomy node. Ordinary upward navigation moves through taxonomy parents and exits the Item context from its top level; the supported Alt-modified upward action returns directly to the global Recipes root. Category selection remains a Recipe-browser scope and is not folded into `RecipeFilterState`, `BrowserDestination`, browser history identity, or concrete-recipe matching.


## Bestiary architecture

### Catalog, identity, and static loot relations

`NpcCatalog` is the immutable Bestiary-domain NPC catalog. Its universe is built from finalized Vanilla Bestiary entries rather than from an independent sweep of all NPC IDs. Each entry keeps the NPC net ID used by runtime/sample data and its finalized Bestiary order; Bestiary credit identity remains a separate Terraria-owned concept used by native encounter progression.

`NpcLootIndex` is a dedicated immutable relation index. It stores static Bestiary NPC→Item relations and the reverse Item→NPC lookup without moving cross-domain relations into `ItemCatalogEntry` or `NpcCatalogEntry`. Coins are not represented as item-drop relations; NPC monetary value belongs to the finalized stats/Details projection.

The static relation layer is intentionally distinct from both current Vanilla Bestiary visibility and actual runtime drop execution. Non-difficulty conditions can remain visible as qualifiers, while selected difficulty determines applicability of difficulty-specific branches.

### Encounter, filters, and browser projection

Terraria remains the owner of Bestiary encounter progress. Terrarian Compendium reads finalized entry-level observation from the native Bestiary provider and does not persist or network a duplicate encounter state. Encounter state is used for silhouettes, overall/filtered progress, Encountered/Unknown filtering, and revision/invalidation of the browser projection; it does not gate finalized names, static potential loot, or Item→NPC source relations.

`BestiaryFilterState` owns four independent browser-filter concerns: one native Bestiary criterion, one loot-aware drop criterion, Encounter filtering, and the project-owned `Has stock` merchant filter. Native criteria are derived from finalized Bestiary filter registrations rather than from a duplicated hardcoded biome/event table. The loot-aware group is project-owned and exposes `All`, `Has drops`, `Has missing drops`, and, when Journey research state is available, `Has unresearched drops`; only one value can be active inside each native/drop criterion group, while the two groups may be combined. `Has missing drops` reads collection state and `Has unresearched drops` reads Journey research state through their existing owners rather than duplicating that state in Bestiary.

`BestiaryBrowserModel` owns search text, sort mode, and sort direction and combines those values with filter-state revisions, collection/research revisions when relevant, and native observation changes to rebuild a deterministic visible-entry projection. Search, the selected native criterion, and the selected drop criterion define the Bestiary scope totals; Encounter and `Has stock` narrow the visible projection afterward. `Has stock` is backed by `MerchantSourceIndex.ContainsMerchant(...)` and represents potential stock rather than current shop availability.

Bestiary search resolves finalized display names independently of Vanilla unknown-name presentation. Sorting exposes the supported Bestiary-facing modes plus the project-specific NPC ID mode and keeps direction as browser state rather than navigation/history identity.

### NPC Details projection

`NpcDetailsModel` is a domain-specific projection over `NpcCatalog`, `NpcLootIndex`, optional `MerchantSourceIndex`, item text/collection state, and a Terraria-facing metadata provider. Terraria-dependent extraction remains behind `VanillaBestiaryNativeBridge`; the model exposes an injectable metadata-provider seam for deterministic tests while the production path uses the native bridge. The native metadata cache is bounded to the current world session and is cleared when a new world is loaded so world-dependent snapshots are not reused across world boundaries.

NPC Details uses a local `NpcDifficultyMode` (`Classic`, `Expert`, `Master`) that is presentation state, not a browser destination or persisted preference. Difficulty-specific stats use native NPC scaling for a single-player override and the current active-world progression state, while finalized Bestiary adjustments and `HideStats` remain authoritative. Difficulty-dependent composite-stat adjustments use the same selected mode; in particular, aggregate Eater of Worlds health uses the segment-count mapping for the selected Details difficulty rather than the difficulty of the currently open world.

The Details projection keeps separate concepts for Bestiary rarity stars, rare-creature metadata, spawn/environment descriptors, base/default debuff immunities, monetary value, static item loot, native kill/banner statistics, and potential merchant stock. `Slain` consumes Terraria's Bestiary kill tracker only when the finalized entry exposes the native kill-counter element. Banner identity, accumulated banner kills, and kills-per-banner threshold remain Terraria-owned `BannerSystem` / Item metadata; the projection exposes the related banner Item and current progress without persisting or networking a duplicate counter. Merchant NPCs expose a `Stock` grid from the shared merchant relation. Conditional/restricted Stock Items present a compact condition/restriction summary through the shared supplemental Item-tooltip layer and expose full per-offer condition details through an anchored Details-level popover opened from that Stock Item; this presentation preserves normalized variant/restriction semantics without evaluating the current runtime shop. Base/default immunity data is a snapshot after native defaults and does not claim to describe dynamic per-AI-phase immunity changes.

## Browser navigation and Details

`BrowserNavigationState` is the single current owner of browser destination/history state.

Current destination kinds are:

- section root;
- Item;
- Armor Set;
- Recipe query;
- Recipe;
- NPC.

Item destinations belong to the Items section. Armor Set destinations belong to the Armor Sets section. Recipe-query and Recipe destinations belong to the Recipes section. NPC destinations belong to the Bestiary section. A Recipe query carries the contextual Item ID used by the Recipes catalog. Recipe destinations reached from such a query preserve that query Item context.

The navigation owner maintains runtime Back/Forward history plus a revision used by consumers such as section views and the shared Details surface. Ordinary navigation appends a destination and discards an obsolete Forward branch after navigating from a historical position. `ReplaceCurrent` normalizes the current destination without creating a normal history entry and coalesces equal neighboring entries.

Browser history is runtime UI state and is not a persistent identity/storage mechanism. Recipe filters and Recipe result-item category scope remain separate from destination/history identity.

Catalog-entry repeat-click deselection is an interaction policy layered above ordinary navigation. Browser catalog views use the shared `BrowserSelectionNavigation` helper so selecting the currently active entry returns to that section root, while ordinary `BrowserNavigationState.Navigate()` calls — including cross-domain navigation from Details — keep normal navigation semantics.

### External vanilla Item navigation

`InventoryItemNavigationHandler` is the scoped runtime integration boundary for opening concrete Items from supported Vanilla `ItemSlot` surfaces. It does not own a second navigation model: a successful action delegates to `BrowserShell.OpenItem(...)`, which navigates to `BrowserDestination.ForItem(...)` through the existing `BrowserNavigationState` and opens the existing host when necessary. Repeated navigation to the current Item therefore follows ordinary navigation equality/history semantics rather than catalog repeat-click deselection.

The supported surface classes are the main player inventory including coin/ammo slots, the currently open world chest or personal storage, active equipment/vanity/dye/misc-equipment slots, and the current NPC shop. Specialized workflow slots such as reforge, trash, Guide input, Journey/Creative actions, and crafting-specific slots are not part of this navigation contract. Exact target-version context/array mappings remain Terraria research evidence rather than architecture identity.

The interaction is gated by the client-side `Inventory Item Navigation` setting and uses the fixed `Alt + Right Click` chord. Because supported slots already have native RMB behavior, the production boundary is a pre-`ItemSlot.Handle(...)` interception: once the navigation action is accepted, original slot handling remains suppressed until physical RMB release so native open/equip/stack-transfer behavior cannot execute as a residual second action. Ordinary RMB without the navigation modifier remains native. This is a narrow Item-slot integration and must not introduce a parallel global input manager or separate browser navigation owner.

Cross-domain navigation uses this same owner:

- Item Details can open a related Armor Set destination;
- Armor Set Details can navigate from set members to Item destinations;
- Item Details can open a contextual Recipe query for an Item that has producing or using Recipe relations; the Recipes catalog owns the corresponding `Used in` exploration rather than Item Details;
- crafting-station Items can navigate to the Recipes root while setting the shared station requirement filter;
- Recipe Details can navigate from result/ingredient/group-alternative Items to Item destinations;
- Recipe Details can navigate from an Item-backed station requirement to the representative station Item;
- NPC Details can navigate from a static drop relation to the corresponding Item;
- Item Details can navigate from `Dropped by` sources to the corresponding NPC destination;
- Item Details can navigate from `Purchasable` merchant sources to the corresponding NPC destination;
- NPC Details can navigate from merchant `Stock` Items to the corresponding Item destination;
- Item Details can navigate between an Item and related Openable Item sources or possible direct outputs through ordinary Item destinations.

Section views must not directly own or open the shared Details surface. They navigate through the shared navigation state.

`BrowserDetailsSurface` is the shared right-side host. Domain-specific Item, Armor Set, Recipe, and NPC detail views remain separate projections instead of being collapsed into a universal details model.

The shared surface owns host-level presentation concerns such as the Details frame, header, scroll state, navigation-driven scroll reset, and the overlay host used by Details-level transient surfaces such as the direct-crafting and merchant-stock-conditions popovers. Domain views own domain-specific content rendering while delegating transient-surface state/close handling through the shared surface.

## UI foundation and hosting boundary

The production browser uses Vanilla Terraria retained UI as its composition foundation:

- `UserInterface` / `UIState` / `UIElement` provide the retained hierarchy and state lifecycle;
- Vanilla layout, clipping, scrolling, and native text-input primitives provide browser composition behavior;
- TerrariaModder remains the framework authority for mod lifecycle, configuration, logging, keybinds, top-level draw/panel integration, shared UI colors, and deferred tooltip services.

Terrarian Compendium owns one shared Vanilla/Core host adapter between those layers. `VanillaUiHost` is infrastructure, not a domain owner. Browser destinations, catalogs, session state, deterministic projections, and domain-specific view models remain outside the renderer/host boundary.

The shared host boundary centralizes the cross-framework concerns required by the mod-owned Vanilla `UserInterface`, including:

- Vanilla UI lifetime and coordinate-context transitions;
- integration with TerrariaModder panel draw, bounds, and z-order ownership;
- mouse and captured-wheel routing between Core and Vanilla UI;
- keyboard/text-input ownership and cleanup;
- framework deferred tooltip lifecycle plus the project-owned supplemental annotation lifecycle around Vanilla drawing.

`SupplementalTooltip` is the shared project-owned layer for annotations that need to coexist with the framework Item tooltip. It participates in the same host-level deferred lifecycle, aggregates project-owned text annotations and compact visual rows into one secondary presentation, applies viewport-aware wrapping/layout, and complements rather than replaces `ItemTooltip` or TerrariaModder's ordinary tooltip services. Domain views register supplemental annotations without taking ownership of framework tooltip state.

The browser input contract handles Escape through the same host/browser boundary with the precedence `focused text input → open transient surface → Compendium window`. Details-level transient surfaces, including the direct-crafting and merchant-stock-conditions popovers, participate in that same contract rather than defining a separate Escape path. The text-input path retains the existing release-tail blocking behavior until physical Escape release so the same key press is not re-observed as a second Terraria action. This policy does not introduce a parallel global input manager.

For TerrariaModder Core 0.4.1, the host maintains framework text-input ownership only while Compendium text input is actually active and explicitly releases it through `UIRenderer.DisableTextInput()` when focus is lost, a higher-priority Core panel blocks Compendium input, or the browser closes. This framework text-input lifecycle is distinct from the project-owned Escape release-tail used by the browser key blocker.

The browser lifecycle treats Terraria's in-game options window as a close condition distinct from the title/menu `gameMenu` state. While the in-game options window is active, the Compendium shell closes instead of updating; this normal close does not destroy the current browser session, navigation history, or filter composition. Closing Terraria's options does not automatically reopen Compendium, so reopening remains an explicit user action.

`VanillaPopover` is the shared production popover primitive. Popover content exposes natural-size measurement through the shared content contract; `VanillaPopover` adds shared chrome, constrains the resulting geometry to the available viewport, and preserves the existing anchoring and transient-surface lifecycle. Production callers should not reintroduce local fixed popover chrome or sizing when the shared measurement contract is sufficient.

Compact Item/NPC value presentation reuses shared retained-UI controls rather than domain-local equivalents. `VanillaIconValueElement` owns the common icon/value row geometry, truncation, and tooltip behavior, while `VanillaCoinValueElement` owns coin-denomination decomposition and compact denomination rendering. Domain views remain responsible for selecting the semantic value and appropriate presentation asset.

Shared vanilla textures are requested through `DeferredTextureLoader`. UI requests preserve `NotLoaded` until a bounded queue performs `ImmediateLoad` on the game update thread: at most four loads per update, stopping before another load once four milliseconds have elapsed. One native load can exceed that time budget. `AsyncLoad` must not be used for these assets: Terraria 1.4.5.8's `Main.LoadItem` skips assets in `Loading`, while world-item drawing immediately dereferences their still-null texture values. The same policy covers Item, NPC, Buff, Tile, Extra, and shared UI textures. Pending requests are deduplicated, rechecked before loading, and cleared on world/mod unload without disposing Terraria-owned assets. Cached UI atlas references are re-enqueued when needed after a queue reset or asset invalidation. Queue scheduling is covered by automated tests; cold-cache joined-client rendering and first-view scrolling require in-game validation.

For mouse input, the host continues to honor `UIRenderer.ShouldBlockForHigherPriorityPanel(...)`. When the Compendium panel is not blocked and its own bounds are registered, the host temporarily unregisters only the Compendium panel's own bounds around `UserInterface.Update()` and restores them afterward. It must not manipulate registrations owned by other Core panels.

The accepted architecture does not introduce a parallel global z-order/input manager, manipulate foreign panel registrations, or duplicate TerrariaModder's global click-through system. A general project-owned UI framework is also not part of the architecture: Vanilla provides retained composition primitives, while TerrariaModder continues to provide shared mod/framework services.

Presentation changes may evolve control composition, spacing, icon framing, scrollbar visuals, and panel geometry, but they must preserve the existing domain/catalog/session/navigation ownership boundaries and reuse the current shared UI/host mechanisms where they satisfy the required behavior.

## Persistence ownership

- Per-character collection progress remains separate from client-local state. The sidecar identity includes the normalized active player save path and `IsCloudSave`; Local and Cloud representations therefore resolve to different project identities. Automatic migration or merge across changed identities is not guaranteed. Its sidecar persistence preserves a validated backup when rewriting a primary file recovered from that backup, while ordinary successful saves retain normal backup rotation. If an existing backup is needed for recovery and cannot be read, loading fails and does not authorize an automatic rewrite that could replace that backup.
- Recipe favorites are client-local and persist versioned `RecipePersistentKey` values separately from per-character collection progress. Valid unresolved keys are preserved rather than heuristically remapped. Duplicate/order normalization and validated-backup recovery are storage concerns; a recovery rewrite preserves the verified backup rather than rotating the corrupt primary over it. If an existing backup is needed for recovery and cannot be read, loading fails and does not authorize an automatic rewrite. Unsupported schema versions disable writes so newer data is not destructively overwritten. No migration from other mods is part of this favorites contract.
- Terraria-native Journey, crafting-availability, and Bestiary state is not duplicated into project persistence without a confirmed need.
- The supported product-rebrand compatibility path is the controlled migration from legacy `item-checklist` collection progress to the current `terrarian-compendium` root.

## Deferred architecture boundary

The following remain intentionally outside the mandatory current architecture:

- recursive Craft Planner / CraftPath;
- automatic multi-step ingredient expansion and path optimization;
- shops, mining, catching, and other acquisition sources as planner nodes;
- nearby/portable-storage material planning;
- multiplayer/team planner or favorites synchronization;
- Magic Storage and custom-content/provider ecosystems.

These deferred systems may consume the current catalogs and relation indices later; they do not justify a speculative acquisition graph in the current core.

## Testing boundary

- Deterministic project-owned behavior belongs in automated tests.
- Terraria, TerrariaModder, XNA, and other runtime-owned semantics are validated through in-game runtime acceptance rather than fragile local simulations.
- Runtime-sensitive integration should be added only together with a concrete acceptance scenario that demonstrates why it is required.