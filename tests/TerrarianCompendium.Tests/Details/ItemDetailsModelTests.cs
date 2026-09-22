using System;
using System.Collections.Generic;
using NUnit.Framework;
using TerrarianCompendium.Acquisition;
using TerrarianCompendium.ArmorSets;
using TerrarianCompendium.Bestiary;
using TerrarianCompendium.Catalog;
using TerrarianCompendium.Checklist;
using TerrarianCompendium.Crafting;
using TerrarianCompendium.Details;
using TerrarianCompendium.Journey;
using TerrarianCompendium.Recipes;

namespace TerrarianCompendium.Tests.Details
{
    [TestFixture]
    public sealed class ItemDetailsModelTests
    {
        [Test]
        public void TryGetProjection_KnownItem_ProjectsIdentityAndStats()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(
                    1,
                    "Test Item",
                    sortMetrics: new ItemSortMetrics(
                        value: 1234567,
                        rarity: -12,
                        damage: 42,
                        defense: 8,
                        pickPower: 65,
                        axePower: 20,
                        hammerPower: 55,
                        fishingPower: 25),
                    detailsStats: new ItemDetailsStats(
                        ItemDetailsDamageType.Melee,
                        knockback: 6.5f,
                        baseCriticalHitChance: 11,
                        useTimeTicks: 19,
                        tagDamage: 8)));
            var state = new ChecklistState(catalog);
            var model = new ItemDetailsModel(catalog, state, new ItemTextIndex(catalog));

            bool found = model.TryGetProjection(1, out ItemDetailsProjection projection);

            Assert.That(found, Is.True);
            Assert.That(projection.ItemId, Is.EqualTo(1));
            Assert.That(projection.Name, Is.EqualTo("Test Item"));
            Assert.That(projection.Value, Is.EqualTo(1234567));
            Assert.That(projection.Rarity, Is.EqualTo(-12));
            Assert.That(projection.Damage, Is.EqualTo(42));
            Assert.That(projection.Defense, Is.EqualTo(8));
            Assert.That(projection.PickPower, Is.EqualTo(65));
            Assert.That(projection.AxePower, Is.EqualTo(20));
            Assert.That(projection.HammerPower, Is.EqualTo(55));
            Assert.That(projection.FishingPower, Is.EqualTo(25));
            Assert.That(projection.DamageType, Is.EqualTo(ItemDetailsDamageType.Melee));
            Assert.That(projection.Knockback, Is.EqualTo(6.5f));
            Assert.That(projection.BaseCriticalHitChance, Is.EqualTo(11));
            Assert.That(projection.UseTimeTicks, Is.EqualTo(19));
            Assert.That(projection.TagDamage, Is.EqualTo(8));
            Assert.That(projection.IsFound, Is.False);
            Assert.That(projection.ResearchStatus, Is.EqualTo(ItemResearchStatus.Unavailable));
            Assert.That(projection.RecipeDataAvailable, Is.False);
        }


        [Test]
        public void TryGetProjection_DifferentItems_ProjectTheirOwnStatsIncludingZeroValues()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(1, "First", sortMetrics: new ItemSortMetrics(100, 2, 10, 0, 0, 0, 0, 0)),
                new ItemCatalogEntry(2, "Second"));
            var state = new ChecklistState(catalog);
            var model = new ItemDetailsModel(catalog, state, new ItemTextIndex(catalog));

            Assert.That(model.TryGetProjection(1, out ItemDetailsProjection first), Is.True);
            Assert.That(first.Value, Is.EqualTo(100));
            Assert.That(first.Rarity, Is.EqualTo(2));
            Assert.That(first.Damage, Is.EqualTo(10));

            Assert.That(model.TryGetProjection(2, out ItemDetailsProjection second), Is.True);
            Assert.That(second.Value, Is.Zero);
            Assert.That(second.Rarity, Is.Zero);
            Assert.That(second.Damage, Is.Zero);
            Assert.That(second.Defense, Is.Zero);
            Assert.That(second.PickPower, Is.Zero);
            Assert.That(second.AxePower, Is.Zero);
            Assert.That(second.HammerPower, Is.Zero);
            Assert.That(second.FishingPower, Is.Zero);
            Assert.That(second.DamageType, Is.EqualTo(ItemDetailsDamageType.None));
            Assert.That(second.Knockback, Is.Null);
            Assert.That(second.BaseCriticalHitChance, Is.Null);
            Assert.That(second.UseTimeTicks, Is.Null);
            Assert.That(second.TagDamage, Is.Null);
        }

        [Test]
        public void TryGetProjection_UnknownItem_ReturnsFalse()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "First"));
            var state = new ChecklistState(catalog);
            var model = new ItemDetailsModel(catalog, state, new ItemTextIndex(catalog));

            bool found = model.TryGetProjection(999, out ItemDetailsProjection projection);

            Assert.That(found, Is.False);
            Assert.That(projection, Is.Null);
        }

        [Test]
        public void TryGetProjection_WhenCollectionRevisionChanges_RefreshesFoundState()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "First"));
            var state = new ChecklistState(catalog);
            var model = new ItemDetailsModel(catalog, state, new ItemTextIndex(catalog));

            Assert.That(model.TryGetProjection(1, out ItemDetailsProjection before), Is.True);
            Assert.That(before.IsFound, Is.False);

            Assert.That(state.MarkFound(1), Is.True);
            Assert.That(model.TryGetProjection(1, out ItemDetailsProjection after), Is.True);
            Assert.That(after.IsFound, Is.True);
        }

        [Test]
        public void TryGetProjection_WithoutJourneyState_ReportsResearchUnavailable()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "First"));
            var state = new ChecklistState(catalog);
            var model = new ItemDetailsModel(catalog, state, new ItemTextIndex(catalog));

            Assert.That(model.TryGetProjection(1, out ItemDetailsProjection projection), Is.True);
            Assert.That(projection.ResearchStatus, Is.EqualTo(ItemResearchStatus.Unavailable));
        }

        [Test]
        public void TryGetProjection_WithNonResearchableItem_ReportsNotResearchable()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "First"));
            var state = new ChecklistState(catalog);
            JourneyResearchState researchState = CreateResearchState();
            var model = new ItemDetailsModel(catalog, state, new ItemTextIndex(catalog), researchState);

            Assert.That(model.TryGetProjection(1, out ItemDetailsProjection projection), Is.True);
            Assert.That(projection.ResearchStatus, Is.EqualTo(ItemResearchStatus.NotResearchable));
        }

        [Test]
        public void TryGetProjection_WhenResearchRevisionChanges_RefreshesResearchStatus()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "First"));
            var state = new ChecklistState(catalog);
            JourneyResearchState researchState =
                CreateResearchState(new JourneyResearchDefinition(1, 1, 2, hasSharedResearchIdentity: false));
            var model = new ItemDetailsModel(catalog, state, new ItemTextIndex(catalog), researchState);

            Assert.That(model.TryGetProjection(1, out ItemDetailsProjection before), Is.True);
            Assert.That(before.ResearchStatus, Is.EqualTo(ItemResearchStatus.Unresearched));

            Assert.That(researchState.ReplaceProgressSnapshot([new KeyValuePair<int, int>(1, 2)]), Is.True);
            Assert.That(model.TryGetProjection(1, out ItemDetailsProjection after), Is.True);
            Assert.That(after.ResearchStatus, Is.EqualTo(ItemResearchStatus.Researched));
        }

        [Test]
        public void TryGetProjection_WithRecipeIndex_ProjectsRecipeRelationAvailability()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(1, "Produced"),
                new ItemCatalogEntry(2, "Ingredient"),
                new ItemCatalogEntry(3, "Other Ingredient"),
                new ItemCatalogEntry(4, "Unrelated"));
            var state = new ChecklistState(catalog);
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1, 2), CreateRecipeEntry(1, 1, 3));
            var recipeIndex = RecipeIndex.Create(recipeCatalog);
            var model = new ItemDetailsModel(catalog, state, new ItemTextIndex(catalog), recipeIndex: recipeIndex);

            Assert.That(model.TryGetProjection(1, out ItemDetailsProjection produced), Is.True);
            Assert.That(produced.RecipeDataAvailable, Is.True);
            Assert.That(produced.HasRecipe, Is.True);
            Assert.That(produced.ProducingRecipeCount, Is.EqualTo(2));
            Assert.That(produced.HasRecipeRelations, Is.True);

            Assert.That(model.TryGetProjection(2, out ItemDetailsProjection ingredient), Is.True);
            Assert.That(ingredient.RecipeDataAvailable, Is.True);
            Assert.That(ingredient.HasRecipe, Is.False);
            Assert.That(ingredient.ProducingRecipeCount, Is.Zero);
            Assert.That(ingredient.HasRecipeRelations, Is.True);

            Assert.That(model.TryGetProjection(4, out ItemDetailsProjection unrelated), Is.True);
            Assert.That(unrelated.RecipeDataAvailable, Is.True);
            Assert.That(unrelated.HasRecipe, Is.False);
            Assert.That(unrelated.ProducingRecipeCount, Is.Zero);
            Assert.That(unrelated.HasRecipeRelations, Is.False);
        }

        [Test]
        public void TryGetProjection_RecipeGroupConcreteMember_HasRecipeRelations()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(1, "Result"),
                new ItemCatalogEntry(2, "Member"),
                new ItemCatalogEntry(3, "Other Member"));
            var state = new ChecklistState(catalog);
            var groupIngredient = new RecipeIngredient(2, 1, RecipeIngredientRequirement.ForRecipeGroup(25, [2, 3]));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(
                new RecipeCatalogEntry(
                    0,
                    1,
                    1,
                    [groupIngredient],
                    new RecipeEnvironmentRequirements(null, false, false, false, false, false, false, false),
                    isAlchemy: false));
            var recipeIndex = RecipeIndex.Create(recipeCatalog);
            var model = new ItemDetailsModel(catalog, state, new ItemTextIndex(catalog), recipeIndex: recipeIndex);

            Assert.That(model.TryGetProjection(3, out ItemDetailsProjection projection), Is.True);
            Assert.That(projection.HasRecipe, Is.False);
            Assert.That(projection.ProducingRecipeCount, Is.Zero);
            Assert.That(projection.HasRecipeRelations, Is.True);
        }

        [Test]
        public void TryGetProjection_WithoutRecipeIndex_ReportsRecipeDataUnavailable()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "First"));
            var state = new ChecklistState(catalog);
            var model = new ItemDetailsModel(catalog, state, new ItemTextIndex(catalog));

            Assert.That(model.TryGetProjection(1, out ItemDetailsProjection projection), Is.True);
            Assert.That(projection.RecipeDataAvailable, Is.False);
            Assert.That(projection.HasRecipeRelations, Is.False);
            Assert.That(projection.HasRecipe, Is.False);
            Assert.That(projection.ProducingRecipeCount, Is.Zero);
            Assert.That(projection.IsCraftableNow, Is.False);
        }

        [Test]
        public void TryGetProjection_WithRecipeIndexButNoAvailability_ReportsNotCraftableNow()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "First"));
            var state = new ChecklistState(catalog);
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1));
            var recipeIndex = RecipeIndex.Create(recipeCatalog);
            var model = new ItemDetailsModel(catalog, state, new ItemTextIndex(catalog), recipeIndex: recipeIndex);

            Assert.That(model.TryGetProjection(1, out ItemDetailsProjection projection), Is.True);
            Assert.That(projection.RecipeDataAvailable, Is.True);
            Assert.That(projection.HasRecipeRelations, Is.True);
            Assert.That(projection.HasRecipe, Is.True);
            Assert.That(projection.ProducingRecipeCount, Is.EqualTo(1));
            Assert.That(projection.IsCraftableNow, Is.False);
        }

        [Test]
        public void TryGetProjection_WithMultipleProducingRecipes_UsesAnyAvailableRecipe()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "First"));
            var state = new ChecklistState(catalog);
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1), CreateRecipeEntry(1, 1));
            var recipeIndex = RecipeIndex.Create(recipeCatalog);
            var craftingState = new CraftingAvailabilityState(recipeCatalog);
            craftingState.ReplaceSnapshot([1]);
            var model = new ItemDetailsModel(
                catalog,
                state,
                new ItemTextIndex(catalog),
                recipeIndex: recipeIndex,
                craftingAvailabilityState: craftingState);

            Assert.That(model.TryGetProjection(1, out ItemDetailsProjection projection), Is.True);
            Assert.That(projection.IsCraftableNow, Is.True);
        }

        [Test]
        public void TryGetProjection_WhenCraftingRevisionChanges_RefreshesCraftability()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "First"));
            var state = new ChecklistState(catalog);
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1));
            var recipeIndex = RecipeIndex.Create(recipeCatalog);
            var craftingState = new CraftingAvailabilityState(recipeCatalog);
            var model = new ItemDetailsModel(
                catalog,
                state,
                new ItemTextIndex(catalog),
                recipeIndex: recipeIndex,
                craftingAvailabilityState: craftingState);

            Assert.That(model.TryGetProjection(1, out ItemDetailsProjection before), Is.True);
            Assert.That(before.IsCraftableNow, Is.False);

            Assert.That(craftingState.ReplaceSnapshot([0]), Is.True);
            Assert.That(model.TryGetProjection(1, out ItemDetailsProjection available), Is.True);
            Assert.That(available.IsCraftableNow, Is.True);

            Assert.That(craftingState.ReplaceSnapshot([]), Is.True);
            Assert.That(model.TryGetProjection(1, out ItemDetailsProjection unavailable), Is.True);
            Assert.That(unavailable.IsCraftableNow, Is.False);
        }

        [Test]
        public void TryGetProjection_CraftingStationItem_ProjectsRequiredTileId()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(1, "Work Bench"),
                new ItemCatalogEntry(2, "Result"));
            var state = new ChecklistState(catalog);
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(
                CreateRecipeEntryWithRequiredTile(0, 2, requiredTileId: 18));
            var recipeIndex = RecipeIndex.Create(recipeCatalog);
            var stationIndex = RecipeStationDisplayIndex.Create(
                catalog,
                recipeCatalog,
                itemId => itemId == 1 ? 18 : -1);
            var model = new ItemDetailsModel(
                catalog,
                state,
                new ItemTextIndex(catalog),
                recipeIndex: recipeIndex,
                recipeStationDisplayIndex: stationIndex);

            Assert.That(model.TryGetProjection(1, out ItemDetailsProjection projection), Is.True);
            Assert.That(projection.CraftingStationRequiredTileId, Is.EqualTo(18));
        }

        [Test]
        public void TryGetProjection_AlternativeItemForSameStation_ProjectsSameRequiredTileId()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(1, "First Work Bench"),
                new ItemCatalogEntry(2, "Second Work Bench"),
                new ItemCatalogEntry(3, "Result"));
            var state = new ChecklistState(catalog);
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(
                CreateRecipeEntryWithRequiredTile(0, 3, requiredTileId: 18));
            var recipeIndex = RecipeIndex.Create(recipeCatalog);
            var stationIndex = RecipeStationDisplayIndex.Create(
                catalog,
                recipeCatalog,
                itemId => itemId is 1 or 2 ? 18 : -1);
            var model = new ItemDetailsModel(
                catalog,
                state,
                new ItemTextIndex(catalog),
                recipeIndex: recipeIndex,
                recipeStationDisplayIndex: stationIndex);

            Assert.That(model.TryGetProjection(1, out ItemDetailsProjection first), Is.True);
            Assert.That(model.TryGetProjection(2, out ItemDetailsProjection second), Is.True);
            Assert.That(first.CraftingStationRequiredTileId, Is.EqualTo(18));
            Assert.That(second.CraftingStationRequiredTileId, Is.EqualTo(18));
        }

        [Test]
        public void TryGetProjection_PlaceableItemWhoseTileIsNotARecipeStation_ProjectsNoStationRequirement()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(1, "Placeable"),
                new ItemCatalogEntry(2, "Result"));
            var state = new ChecklistState(catalog);
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(
                CreateRecipeEntryWithRequiredTile(0, 2, requiredTileId: 18));
            var recipeIndex = RecipeIndex.Create(recipeCatalog);
            var stationIndex = RecipeStationDisplayIndex.Create(
                catalog,
                recipeCatalog,
                itemId => itemId == 1 ? 20 : -1);
            var model = new ItemDetailsModel(
                catalog,
                state,
                new ItemTextIndex(catalog),
                recipeIndex: recipeIndex,
                recipeStationDisplayIndex: stationIndex);

            Assert.That(model.TryGetProjection(1, out ItemDetailsProjection projection), Is.True);
            Assert.That(projection.CraftingStationRequiredTileId, Is.Null);
        }

        [Test]
        public void TryGetProjection_DoesNotMutateSourceStates()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "First"));
            var state = new ChecklistState(catalog);
            JourneyResearchState researchState =
                CreateResearchState(new JourneyResearchDefinition(1, 1, 1, hasSharedResearchIdentity: false));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1));
            var recipeIndex = RecipeIndex.Create(recipeCatalog);
            var craftingState = new CraftingAvailabilityState(recipeCatalog);
            var itemTextIndex = new ItemTextIndex(catalog);
            var model = new ItemDetailsModel(catalog, state, itemTextIndex, researchState, recipeIndex, craftingState);
            long checklistRevision = state.Revision;
            long researchRevision = researchState.Revision;
            long craftingRevision = craftingState.Revision;
            long itemTextRevision = itemTextIndex.Revision;

            Assert.That(model.TryGetProjection(1, out _), Is.True);

            Assert.That(state.Revision, Is.EqualTo(checklistRevision));
            Assert.That(researchState.Revision, Is.EqualTo(researchRevision));
            Assert.That(craftingState.Revision, Is.EqualTo(craftingRevision));
            Assert.That(itemTextIndex.Revision, Is.EqualTo(itemTextRevision));
            Assert.That(recipeIndex.GetRecipesProducing(1), Has.Count.EqualTo(1));
        }

        [Test]
        public void TryGetProjection_WithoutNpcLootBackend_ReportsNpcLootUnavailable()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "Drop"));
            var model = new ItemDetailsModel(catalog, new ChecklistState(catalog), new ItemTextIndex(catalog));

            model.TryGetProjection(1, out ItemDetailsProjection projection);

            Assert.That(projection.NpcLootDataAvailable, Is.False);
            Assert.That(projection.DroppedByNpcSources, Is.Empty);
        }

        [Test]
        public void Constructor_WithNpcLootBackendButNoNameProvider_Throws()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "Drop"));
            var npcCatalog = NpcCatalog.Create([new NpcCatalogEntry(10, 0)]);
            var lootIndex = NpcLootIndex.Create(npcCatalog, []);

            Assert.Throws<ArgumentNullException>(() =>
            {
                _ = new ItemDetailsModel(
                    catalog,
                    new ChecklistState(catalog),
                    new ItemTextIndex(catalog),
                    npcCatalog: npcCatalog,
                    npcLootIndex: lootIndex);
            });
        }

        [Test]
        public void TryGetProjection_UnknownNpcSource_IsProjectedWithoutEncounterGating()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "Drop"));
            var npcCatalog = NpcCatalog.Create([new NpcCatalogEntry(10, 0)]);
            var lootIndex = NpcLootIndex.Create(npcCatalog, [new NpcLootRelation(10, 1, 1, 1, 0.25f)]);
            var model = new ItemDetailsModel(
                catalog,
                new ChecklistState(catalog),
                new ItemTextIndex(catalog),
                npcCatalog: npcCatalog,
                npcLootIndex: lootIndex,
                npcNameProvider: _ => "Blue Slime");

            model.TryGetProjection(1, out ItemDetailsProjection projection);

            Assert.That(projection.NpcLootDataAvailable, Is.True);
            Assert.That(projection.DroppedByNpcSources, Has.Count.EqualTo(1));
            Assert.That(projection.DroppedByNpcSources[0].NpcNetId, Is.EqualTo(10));
            Assert.That(projection.DroppedByNpcSources[0].Name, Is.EqualTo("Blue Slime"));
        }

        [Test]
        public void TryGetProjection_StaticRelation_IsProjectedRegardlessOfCurrentWorldCondition()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "Drop"));
            var npcCatalog = NpcCatalog.Create([new NpcCatalogEntry(10, 0)]);
            var condition = new NpcLootCondition(_ => true, "Only in another world state");
            var lootIndex = NpcLootIndex.Create(npcCatalog, [new NpcLootRelation(10, 1, 1, 1, 0.25f, [condition])]);
            var model = new ItemDetailsModel(
                catalog,
                new ChecklistState(catalog),
                new ItemTextIndex(catalog),
                npcCatalog: npcCatalog,
                npcLootIndex: lootIndex,
                npcNameProvider: _ => "Slime");

            model.TryGetProjection(1, out ItemDetailsProjection projection);

            Assert.That(projection.DroppedByNpcSources, Has.Count.EqualTo(1));
        }

        [Test]
        public void TryGetProjection_DuplicateNpcRows_AreDeduplicatedAndSortedByBestiaryOrder()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "Drop"));
            var npcCatalog = NpcCatalog.Create(
            [
                new NpcCatalogEntry(20, 1),
                new NpcCatalogEntry(10, 0)
            ]);
            var lootIndex = NpcLootIndex.Create(
                npcCatalog,
                [
                    new NpcLootRelation(20, 1, 1, 1, 0.25f),
                    new NpcLootRelation(10, 1, 1, 1, 0.50f),
                    new NpcLootRelation(10, 1, 2, 2, 0.10f)
                ]);
            var model = new ItemDetailsModel(
                catalog,
                new ChecklistState(catalog),
                new ItemTextIndex(catalog),
                npcCatalog: npcCatalog,
                npcLootIndex: lootIndex,
                npcNameProvider: entry => $"NPC {entry.NetId}");

            model.TryGetProjection(1, out ItemDetailsProjection projection);

            Assert.That(projection.DroppedByNpcSources, Has.Count.EqualTo(2));
            Assert.That(projection.DroppedByNpcSources[0].NpcNetId, Is.EqualTo(10));
            Assert.That(projection.DroppedByNpcSources[1].NpcNetId, Is.EqualTo(20));
        }

        [Test]
        public void TryGetProjection_NpcNameChange_InvalidatesCachedProjection()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "Drop"));
            var npcCatalog = NpcCatalog.Create([new NpcCatalogEntry(10, 0)]);
            var lootIndex = NpcLootIndex.Create(npcCatalog, [new NpcLootRelation(10, 1, 1, 1, 0.25f)]);
            var names = new Dictionary<int, string> { [10] = "Slime" };
            var model = new ItemDetailsModel(
                catalog,
                new ChecklistState(catalog),
                new ItemTextIndex(catalog),
                npcCatalog: npcCatalog,
                npcLootIndex: lootIndex,
                npcNameProvider: entry => names[entry.NetId]);

            model.TryGetProjection(1, out ItemDetailsProjection before);
            names[10] = "Blue Slime";
            model.TryGetProjection(1, out ItemDetailsProjection after);

            Assert.That(after, Is.Not.SameAs(before));
            Assert.That(after.DroppedByNpcSources[0].Name, Is.EqualTo("Blue Slime"));
        }

        [Test]
        public void TryGetProjection_WithoutMerchantBackend_ReportsMerchantSourcesUnavailable()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "Item"));
            var model = new ItemDetailsModel(catalog, new ChecklistState(catalog), new ItemTextIndex(catalog));

            model.TryGetProjection(1, out ItemDetailsProjection projection);

            Assert.That(projection.MerchantSourceDataAvailable, Is.False);
            Assert.That(projection.PurchasableFromMerchants, Is.Empty);
        }

        [Test]
        public void TryGetProjection_WithMerchantBackendButNoRelation_ReportsAvailableEmptySources()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "Item"));
            var npcCatalog = NpcCatalog.Create([new NpcCatalogEntry(10, 0)]);
            var merchantIndex = new MerchantSourceIndex([]);
            var model = new ItemDetailsModel(
                catalog,
                new ChecklistState(catalog),
                new ItemTextIndex(catalog),
                npcCatalog: npcCatalog,
                npcNameProvider: _ => "Merchant",
                merchantSourceIndex: merchantIndex);

            model.TryGetProjection(1, out ItemDetailsProjection projection);

            Assert.That(projection.MerchantSourceDataAvailable, Is.True);
            Assert.That(projection.PurchasableFromMerchants, Is.Empty);
        }

        [Test]
        public void Constructor_WithMerchantBackendButNoNpcCatalog_Throws()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "Item"));
            var merchantIndex = new MerchantSourceIndex(
            [
                new MerchantSourceRelation(10, 1, new MerchantSourceVariant(1, []))
            ]);

            Assert.Throws<ArgumentException>(() =>
            {
                _ = new ItemDetailsModel(
                    catalog,
                    new ChecklistState(catalog),
                    new ItemTextIndex(catalog),
                    merchantSourceIndex: merchantIndex);
            });
        }

        [Test]
        public void Constructor_WithMerchantBackendButNoNameProvider_Throws()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "Item"));
            var npcCatalog = NpcCatalog.Create([new NpcCatalogEntry(10, 0)]);
            var merchantIndex = new MerchantSourceIndex(
            [
                new MerchantSourceRelation(10, 1, new MerchantSourceVariant(1, []))
            ]);

            Assert.Throws<ArgumentNullException>(() =>
            {
                _ = new ItemDetailsModel(
                    catalog,
                    new ChecklistState(catalog),
                    new ItemTextIndex(catalog),
                    npcCatalog: npcCatalog,
                    merchantSourceIndex: merchantIndex);
            });
        }

        [Test]
        public void TryGetProjection_MerchantSources_ProjectPotentialSellersSortedByBestiaryOrder()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "Item"));
            var npcCatalog = NpcCatalog.Create(
            [
                new NpcCatalogEntry(20, 1),
                new NpcCatalogEntry(10, 0)
            ]);
            var merchantIndex = new MerchantSourceIndex(
            [
                new MerchantSourceRelation(
                    20,
                    1,
                    new MerchantSourceVariant(
                        2,
                        [new MerchantSourceCondition(MerchantSourceConditionKind.DayTime, isNegated: true)])),
                new MerchantSourceRelation(10, 1, new MerchantSourceVariant(1, [])),
                new MerchantSourceRelation(
                    10,
                    1,
                    new MerchantSourceVariant(1, [new MerchantSourceCondition(MerchantSourceConditionKind.HardMode)]))
            ]);
            var model = new ItemDetailsModel(
                catalog,
                new ChecklistState(catalog),
                new ItemTextIndex(catalog),
                npcCatalog: npcCatalog,
                npcNameProvider: entry => $"Merchant {entry.NetId}",
                merchantSourceIndex: merchantIndex);

            model.TryGetProjection(1, out ItemDetailsProjection projection);

            Assert.That(projection.MerchantSourceDataAvailable, Is.True);
            Assert.That(projection.PurchasableFromMerchants, Has.Count.EqualTo(2));
            Assert.That(projection.PurchasableFromMerchants[0].NpcNetId, Is.EqualTo(10));
            Assert.That(projection.PurchasableFromMerchants[0].Name, Is.EqualTo("Merchant 10"));
            Assert.That(projection.PurchasableFromMerchants[1].NpcNetId, Is.EqualTo(20));
            Assert.That(projection.PurchasableFromMerchants[1].Name, Is.EqualTo("Merchant 20"));
        }

        [Test]
        public void TryGetProjection_MerchantRelationWithCatalogMissingNpc_Throws()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "Item"));
            var npcCatalog = NpcCatalog.Create([new NpcCatalogEntry(10, 0)]);
            var merchantIndex = new MerchantSourceIndex(
            [
                new MerchantSourceRelation(20, 1, new MerchantSourceVariant(1, []))
            ]);
            var model = new ItemDetailsModel(
                catalog,
                new ChecklistState(catalog),
                new ItemTextIndex(catalog),
                npcCatalog: npcCatalog,
                npcNameProvider: _ => "Merchant",
                merchantSourceIndex: merchantIndex);

            Assert.Throws<InvalidOperationException>(() => model.TryGetProjection(1, out _));
        }

        [Test]
        public void TryGetProjection_MerchantSourceNameChange_InvalidatesCachedProjection()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "Item"));
            var npcCatalog = NpcCatalog.Create([new NpcCatalogEntry(10, 0)]);
            var merchantIndex = new MerchantSourceIndex(
            [
                new MerchantSourceRelation(10, 1, new MerchantSourceVariant(1, []))
            ]);
            var names = new Dictionary<int, string> { [10] = "Merchant" };
            var model = new ItemDetailsModel(
                catalog,
                new ChecklistState(catalog),
                new ItemTextIndex(catalog),
                npcCatalog: npcCatalog,
                npcNameProvider: entry => names[entry.NetId],
                merchantSourceIndex: merchantIndex);

            model.TryGetProjection(1, out ItemDetailsProjection before);
            names[10] = "Renamed Merchant";
            model.TryGetProjection(1, out ItemDetailsProjection after);

            Assert.That(after, Is.Not.SameAs(before));
            Assert.That(after.PurchasableFromMerchants[0].Name, Is.EqualTo("Renamed Merchant"));
        }

        [Test]
        public void TryGetProjection_ItemTextDescription_IsProjectedAndRefreshesWithTextRevision()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "First"));
            var state = new ChecklistState(catalog);
            var textIndex = new ItemTextIndex(catalog);
            textIndex.ReplaceSnapshot(
                "en-US",
                new Dictionary<int, string> { [1] = "First" },
                new Dictionary<int, string> { [1] = "First description" });
            var model = new ItemDetailsModel(catalog, state, textIndex);

            model.TryGetProjection(1, out ItemDetailsProjection before);

            textIndex.ReplaceSnapshot(
                "uk-UA",
                new Dictionary<int, string> { [1] = "Перший" },
                new Dictionary<int, string> { [1] = "Оновлений опис" });
            model.TryGetProjection(1, out ItemDetailsProjection after);

            Assert.That(before.Description, Is.EqualTo("First description"));
            Assert.That(after, Is.Not.SameAs(before));
            Assert.That(after.Name, Is.EqualTo("Перший"));
            Assert.That(after.Description, Is.EqualTo("Оновлений опис"));
        }

        [Test]
        public void TryGetProjection_WithoutBaseDescription_ProjectsEmptyDescription()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "First"));
            var model = new ItemDetailsModel(catalog, new ChecklistState(catalog), new ItemTextIndex(catalog));

            model.TryGetProjection(1, out ItemDetailsProjection projection);

            Assert.That(projection.Description, Is.Empty);
        }

        [Test]
        public void TryGetProjection_WithoutArmorSetBackend_ProjectsNoArmorSetRelations()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "First"));
            var model = new ItemDetailsModel(catalog, new ChecklistState(catalog), new ItemTextIndex(catalog));

            model.TryGetProjection(1, out ItemDetailsProjection projection);

            Assert.That(projection.ArmorSets, Is.Empty);
        }

        [Test]
        public void TryGetProjection_SharedItemAcrossVariants_ProjectsLogicalArmorSetOnce()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(1, "Head A"),
                new ItemCatalogEntry(2, "Head B"),
                new ItemCatalogEntry(10, "Body"),
                new ItemCatalogEntry(20, "Legs"));
            var armorSetCatalog = ArmorSetCatalog.Create(
            [
                new ArmorSetCatalogEntry(
                    1,
                    "ArmorSetBonus.Test",
                    10,
                    [
                        new ArmorSetVariant(1, 10, 20),
                        new ArmorSetVariant(2, 10, 20)
                    ])
            ]);
            var armorSetIndex = ArmorSetIndex.Create(armorSetCatalog);
            var model = new ItemDetailsModel(
                catalog,
                new ChecklistState(catalog),
                new ItemTextIndex(catalog),
                armorSetCatalog: armorSetCatalog,
                armorSetIndex: armorSetIndex);

            model.TryGetProjection(10, out ItemDetailsProjection projection);

            Assert.That(projection.ArmorSets, Has.Count.EqualTo(1));
            Assert.That(projection.ArmorSets[0].ArmorSetId, Is.EqualTo(1));
            Assert.That(projection.ArmorSets[0].RepresentativeItemId, Is.EqualTo(10));
        }

        [Test]
        public void TryGetProjection_ItemSharedByDifferentBonusEntries_ProjectsEachLogicalArmorSet()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(1, "Head A"),
                new ItemCatalogEntry(2, "Head B"),
                new ItemCatalogEntry(10, "Body"),
                new ItemCatalogEntry(20, "Legs"));
            var armorSetCatalog = ArmorSetCatalog.Create(
            [
                new ArmorSetCatalogEntry(1, "ArmorSetBonus.First", 1, [new ArmorSetVariant(1, 10, 20)]),
                new ArmorSetCatalogEntry(2, "ArmorSetBonus.Second", 2, [new ArmorSetVariant(2, 10, 20)])
            ]);
            var armorSetIndex = ArmorSetIndex.Create(armorSetCatalog);
            var model = new ItemDetailsModel(
                catalog,
                new ChecklistState(catalog),
                new ItemTextIndex(catalog),
                armorSetCatalog: armorSetCatalog,
                armorSetIndex: armorSetIndex);

            model.TryGetProjection(10, out ItemDetailsProjection projection);

            Assert.That(projection.ArmorSets, Has.Count.EqualTo(2));
            Assert.That(projection.ArmorSets[0].ArmorSetId, Is.EqualTo(1));
            Assert.That(projection.ArmorSets[1].ArmorSetId, Is.EqualTo(2));
        }

        [Test]
        public void Constructor_WithOnlyOneArmorSetDependency_Throws()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "First"));
            var armorSetCatalog = ArmorSetCatalog.Create(
            [
                new ArmorSetCatalogEntry(1, "ArmorSetBonus.Test", 1, [new ArmorSetVariant(1, 0, 0)])
            ]);

            Assert.Throws<ArgumentException>(() =>
            {
                _ = new ItemDetailsModel(
                    catalog,
                    new ChecklistState(catalog),
                    new ItemTextIndex(catalog),
                    armorSetCatalog: armorSetCatalog);
            });
        }

        [Test]
        public void TryGetProjection_WithWorldLootIndex_ProjectsOrderedWorldSources()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "Loot"));
            var late = new WorldLootSource("late", "Acquisition.World.LateChest", WorldLootSourceKind.Chest, 20, 48);
            var early = new WorldLootSource("early", "Acquisition.World.EarlyPot", WorldLootSourceKind.Pot, 10, null);
            var tree = new WorldLootSource(
                "tree",
                "Acquisition.World.TreeShaking",
                WorldLootSourceKind.TreeShaking,
                15,
                9);
            var worldIndex = new WorldLootSourceIndex(
            [
                new WorldLootRelation(late, 1),
                new WorldLootRelation(tree, 1),
                new WorldLootRelation(early, 1),
                new WorldLootRelation(early, 1)
            ]);
            var model = new ItemDetailsModel(
                catalog,
                new ChecklistState(catalog),
                new ItemTextIndex(catalog),
                worldLootSourceIndex: worldIndex);

            Assert.That(model.TryGetProjection(1, out ItemDetailsProjection projection), Is.True);
            Assert.That(projection.WorldLootDataAvailable, Is.True);
            Assert.That(projection.WorldSources, Has.Count.EqualTo(3));
            Assert.That(projection.WorldSources[0].Key, Is.EqualTo("early"));
            Assert.That(projection.WorldSources[0].DisplayNameKey, Is.EqualTo("Acquisition.World.EarlyPot"));
            Assert.That(projection.WorldSources[0].Kind, Is.EqualTo(WorldLootSourceKind.Pot));
            Assert.That(projection.WorldSources[0].RepresentativeItemId, Is.Null);
            Assert.That(projection.WorldSources[1].Key, Is.EqualTo("tree"));
            Assert.That(projection.WorldSources[1].Kind, Is.EqualTo(WorldLootSourceKind.TreeShaking));
            Assert.That(projection.WorldSources[1].RepresentativeItemId, Is.EqualTo(9));
            Assert.That(projection.WorldSources[2].Key, Is.EqualTo("late"));
            Assert.That(projection.WorldSources[2].RepresentativeItemId, Is.EqualTo(48));
        }

        [Test]
        public void TryGetProjection_WithOpenableIndex_ProjectsSourceItemsAndRefreshesFoundState()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(1, "Loot"),
                new ItemCatalogEntry(10, "Bag"),
                new ItemCatalogEntry(20, "Crate"));
            var state = new ChecklistState(catalog);
            var openableIndex = new OpenableItemLootIndex(
            [
                new OpenableItemLootRelation(20, 1),
                new OpenableItemLootRelation(10, 1),
                new OpenableItemLootRelation(10, 1)
            ]);
            var model = new ItemDetailsModel(
                catalog,
                state,
                new ItemTextIndex(catalog),
                openableItemLootIndex: openableIndex);

            Assert.That(model.TryGetProjection(1, out ItemDetailsProjection before), Is.True);
            Assert.That(before.OpenableItemLootDataAvailable, Is.True);
            Assert.That(before.OpenableSources, Has.Count.EqualTo(2));
            Assert.That(before.OpenableSources[0].ItemId, Is.EqualTo(10));
            Assert.That(before.OpenableSources[0].Name, Is.EqualTo("Bag"));
            Assert.That(before.OpenableSources[0].IsFound, Is.False);

            Assert.That(state.MarkFound(10), Is.True);
            Assert.That(model.TryGetProjection(1, out ItemDetailsProjection after), Is.True);
            Assert.That(after, Is.Not.SameAs(before));
            Assert.That(after.OpenableSources[0].IsFound, Is.True);
        }

        [Test]
        public void TryGetProjection_OpenableSourceNameRefreshesWithItemTextRevision()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "Loot"), new ItemCatalogEntry(10, "Bag"));
            var textIndex = new ItemTextIndex(catalog);
            var openableIndex = new OpenableItemLootIndex([new OpenableItemLootRelation(10, 1)]);
            var model = new ItemDetailsModel(
                catalog,
                new ChecklistState(catalog),
                textIndex,
                openableItemLootIndex: openableIndex);

            Assert.That(model.TryGetProjection(1, out ItemDetailsProjection before), Is.True);
            Assert.That(before.OpenableSources[0].Name, Is.EqualTo("Bag"));

            ReplaceItemTextNames(textIndex, catalog, new Dictionary<int, string> { [10] = "Localized Bag" }, "test");

            Assert.That(model.TryGetProjection(1, out ItemDetailsProjection after), Is.True);
            Assert.That(after, Is.Not.SameAs(before));
            Assert.That(after.OpenableSources[0].Name, Is.EqualTo("Localized Bag"));
        }

        [Test]
        public void TryGetProjection_OpenableSource_ProjectsContentsAndRefreshesPresentationState()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(1, "First Loot"),
                new ItemCatalogEntry(2, "Second Loot"),
                new ItemCatalogEntry(10, "Bag"));
            var state = new ChecklistState(catalog);
            var textIndex = new ItemTextIndex(catalog);
            var openableIndex = new OpenableItemLootIndex(
            [
                new OpenableItemLootRelation(10, 2),
                new OpenableItemLootRelation(10, 1),
                new OpenableItemLootRelation(10, 1)
            ]);
            var model = new ItemDetailsModel(catalog, state, textIndex, openableItemLootIndex: openableIndex);

            Assert.That(model.TryGetProjection(10, out ItemDetailsProjection before), Is.True);
            Assert.That(before.OpenableContents, Has.Count.EqualTo(2));
            Assert.That(before.OpenableContents[0].ItemId, Is.EqualTo(1));
            Assert.That(before.OpenableContents[0].Name, Is.EqualTo("First Loot"));
            Assert.That(before.OpenableContents[0].IsFound, Is.False);

            Assert.That(state.MarkFound(1), Is.True);
            ReplaceItemTextNames(textIndex, catalog, new Dictionary<int, string> { [1] = "Localized Loot" }, "test");

            Assert.That(model.TryGetProjection(10, out ItemDetailsProjection after), Is.True);
            Assert.That(after, Is.Not.SameAs(before));
            Assert.That(after.OpenableContents[0].Name, Is.EqualTo("Localized Loot"));
            Assert.That(after.OpenableContents[0].IsFound, Is.True);
        }

        [Test]
        public void TryGetProjection_FishingSourceIsDirectAndDoesNotFollowOpenableContentsTransitively()
        {
            ItemCatalog catalog = CreateCatalog(
                new ItemCatalogEntry(1, "Direct Catch"),
                new ItemCatalogEntry(2, "Crate Content"),
                new ItemCatalogEntry(10, "Fishing Crate"));
            var fishingIndex = new FishingSourceIndex(
            [
                new FishingSourceRelation(
                    1,
                    new FishingSourceVariant(
                    [
                        FishingSourceConditionKind.Ocean,
                        FishingSourceConditionKind.HardMode
                    ])),
                new FishingSourceRelation(10, new FishingSourceVariant([FishingSourceConditionKind.Ocean]))
            ]);
            var openableIndex = new OpenableItemLootIndex([new OpenableItemLootRelation(10, 2)]);
            var model = new ItemDetailsModel(
                catalog,
                new ChecklistState(catalog),
                new ItemTextIndex(catalog),
                openableItemLootIndex: openableIndex,
                fishingSourceIndex: fishingIndex);

            Assert.That(model.TryGetProjection(1, out ItemDetailsProjection directCatch), Is.True);
            Assert.That(directCatch.FishingSourceDataAvailable, Is.True);
            Assert.That(directCatch.FishingVariants, Has.Count.EqualTo(2));
            Assert.That(
                directCatch.FishingVariants[0].Conditions,
                Is.EqualTo([FishingSourceConditionKind.HardMode, FishingSourceConditionKind.OriginalOcean]));
            Assert.That(
                directCatch.FishingVariants[1].Conditions,
                Is.EqualTo([FishingSourceConditionKind.HardMode, FishingSourceConditionKind.RemixOcean]));

            Assert.That(model.TryGetProjection(2, out ItemDetailsProjection crateContent), Is.True);
            Assert.That(crateContent.OpenableSources, Has.Count.EqualTo(1));
            Assert.That(crateContent.OpenableSources[0].ItemId, Is.EqualTo(10));
            Assert.That(crateContent.FishingVariants, Is.Empty);
        }

        [Test]
        public void TryGetProjection_ExpandsHeight1And2IntoSeparatePresentationAlternatives()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "Catch"));
            var fishingIndex = new FishingSourceIndex(
            [
                new FishingSourceRelation(
                    1,
                    new FishingSourceVariant(
                    [
                        FishingSourceConditionKind.Jungle,
                        FishingSourceConditionKind.Height1And2
                    ]))
            ]);
            var model = new ItemDetailsModel(
                catalog,
                new ChecklistState(catalog),
                new ItemTextIndex(catalog),
                fishingSourceIndex: fishingIndex);

            Assert.That(model.TryGetProjection(1, out ItemDetailsProjection projection), Is.True);
            Assert.That(projection.FishingVariants, Has.Count.EqualTo(2));
            Assert.That(
                projection.FishingVariants[0].Conditions,
                Is.EqualTo([FishingSourceConditionKind.Height1, FishingSourceConditionKind.Jungle]));
            Assert.That(
                projection.FishingVariants[1].Conditions,
                Is.EqualTo([FishingSourceConditionKind.Height2, FishingSourceConditionKind.Jungle]));
        }

        [Test]
        public void TryGetProjection_ExpandsMultipleCompoundFishingConditionsAsCartesianProduct()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "Catch"));
            var fishingIndex = new FishingSourceIndex(
            [
                new FishingSourceRelation(
                    1,
                    new FishingSourceVariant(
                    [
                        FishingSourceConditionKind.Height1And2,
                        FishingSourceConditionKind.Ocean
                    ]))
            ]);
            var model = new ItemDetailsModel(
                catalog,
                new ChecklistState(catalog),
                new ItemTextIndex(catalog),
                fishingSourceIndex: fishingIndex);

            Assert.That(model.TryGetProjection(1, out ItemDetailsProjection projection), Is.True);
            Assert.That(projection.FishingVariants, Has.Count.EqualTo(4));
            Assert.That(
                projection.FishingVariants[0].Conditions,
                Is.EqualTo([FishingSourceConditionKind.Height1, FishingSourceConditionKind.OriginalOcean]));
            Assert.That(
                projection.FishingVariants[1].Conditions,
                Is.EqualTo([FishingSourceConditionKind.Height1, FishingSourceConditionKind.RemixOcean]));
            Assert.That(
                projection.FishingVariants[2].Conditions,
                Is.EqualTo([FishingSourceConditionKind.Height2, FishingSourceConditionKind.OriginalOcean]));
            Assert.That(
                projection.FishingVariants[3].Conditions,
                Is.EqualTo([FishingSourceConditionKind.Height2, FishingSourceConditionKind.RemixOcean]));
        }

        [Test]
        public void TryGetProjection_DoesNotExpandNonCompoundFishingRangeConditions()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "Catch"));
            var fishingIndex = new FishingSourceIndex(
            [
                new FishingSourceRelation(1, new FishingSourceVariant([FishingSourceConditionKind.HeightAbove1]))
            ]);
            var model = new ItemDetailsModel(
                catalog,
                new ChecklistState(catalog),
                new ItemTextIndex(catalog),
                fishingSourceIndex: fishingIndex);

            Assert.That(model.TryGetProjection(1, out ItemDetailsProjection projection), Is.True);
            Assert.That(projection.FishingVariants, Has.Count.EqualTo(1));
            Assert.That(
                projection.FishingVariants[0].Conditions,
                Is.EqualTo([FishingSourceConditionKind.HeightAbove1]));
        }

        [Test]
        public void TryGetProjection_CollapsesEqualRawFishingRulesIntoOnePresentationAlternative()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "Catch"));
            var fishingIndex = new FishingSourceIndex(
            [
                new FishingSourceRelation(
                    1,
                    new FishingSourceVariant(
                    [
                        FishingSourceConditionKind.HardMode,
                        FishingSourceConditionKind.Jungle
                    ])),
                new FishingSourceRelation(
                    1,
                    new FishingSourceVariant(
                    [
                        FishingSourceConditionKind.HardMode,
                        FishingSourceConditionKind.Jungle
                    ]))
            ]);
            var model = new ItemDetailsModel(
                catalog,
                new ChecklistState(catalog),
                new ItemTextIndex(catalog),
                fishingSourceIndex: fishingIndex);

            Assert.That(model.TryGetProjection(1, out ItemDetailsProjection projection), Is.True);
            Assert.That(projection.FishingVariants, Has.Count.EqualTo(1));
            Assert.That(
                projection.FishingVariants[0].Conditions,
                Is.EqualTo([FishingSourceConditionKind.HardMode, FishingSourceConditionKind.Jungle]));
        }

        [Test]
        public void TryGetProjection_CollapsesPresentationDuplicatesCreatedAcrossRawFishingRules()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "Catch"));
            var fishingIndex = new FishingSourceIndex(
            [
                new FishingSourceRelation(1, new FishingSourceVariant([FishingSourceConditionKind.Height1])),
                new FishingSourceRelation(1, new FishingSourceVariant([FishingSourceConditionKind.Height1And2]))
            ]);
            var model = new ItemDetailsModel(
                catalog,
                new ChecklistState(catalog),
                new ItemTextIndex(catalog),
                fishingSourceIndex: fishingIndex);

            Assert.That(model.TryGetProjection(1, out ItemDetailsProjection projection), Is.True);
            Assert.That(projection.FishingVariants, Has.Count.EqualTo(2));
            Assert.That(projection.FishingVariants[0].Conditions, Is.EqualTo([FishingSourceConditionKind.Height1]));
            Assert.That(projection.FishingVariants[1].Conditions, Is.EqualTo([FishingSourceConditionKind.Height2]));
        }

        [Test]
        public void TryGetProjection_ProjectsAdditionalNativeFishingQualifiersUnchanged()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "Catch"));
            var fishingIndex = new FishingSourceIndex(
            [
                new FishingSourceRelation(
                    1,
                    new FishingSourceVariant(
                    [
                        FishingSourceConditionKind.Junk,
                        FishingSourceConditionKind.Crate,
                        FishingSourceConditionKind.Water1000,
                        FishingSourceConditionKind.DidNotUseCombatBook
                    ]))
            ]);
            var model = new ItemDetailsModel(
                catalog,
                new ChecklistState(catalog),
                new ItemTextIndex(catalog),
                fishingSourceIndex: fishingIndex);

            Assert.That(model.TryGetProjection(1, out ItemDetailsProjection projection), Is.True);
            Assert.That(projection.FishingVariants, Has.Count.EqualTo(1));
            Assert.That(
                projection.FishingVariants[0].Conditions,
                Is.EqualTo(
                [
                    FishingSourceConditionKind.Junk,
                    FishingSourceConditionKind.Crate,
                    FishingSourceConditionKind.Water1000,
                    FishingSourceConditionKind.DidNotUseCombatBook
                ]));
        }

        [Test]
        public void TryGetProjection_CombinesLavaAndHoneyStopperExclusionsWithoutExpandingNegativeOcean()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "Catch"));
            var fishingIndex = new FishingSourceIndex(
            [
                new FishingSourceRelation(
                    1,
                    new FishingSourceVariant(
                        Array.Empty<FishingSourceConditionKind>(),
                        [
                            FishingSourceConditionKind.InLava,
                            FishingSourceConditionKind.InHoney,
                            FishingSourceConditionKind.Ocean
                        ]))
            ]);
            var model = new ItemDetailsModel(
                catalog,
                new ChecklistState(catalog),
                new ItemTextIndex(catalog),
                fishingSourceIndex: fishingIndex);

            Assert.That(model.TryGetProjection(1, out ItemDetailsProjection projection), Is.True);
            Assert.That(projection.FishingVariants, Has.Count.EqualTo(1));
            Assert.That(projection.FishingVariants[0].Conditions, Is.Empty);
            Assert.That(projection.FishingVariants[0].ExcludesLavaAndHoney, Is.True);
            Assert.That(
                projection.FishingVariants[0].ExcludedConditions,
                Is.EqualTo([FishingSourceConditionKind.Ocean]));
            Assert.That(
                projection.FishingVariants[0].ExcludedConditions,
                Does.Not.Contain(FishingSourceConditionKind.OriginalOcean));
            Assert.That(
                projection.FishingVariants[0].ExcludedConditions,
                Does.Not.Contain(FishingSourceConditionKind.RemixOcean));
        }

        [Test]
        public void TryGetProjection_HoneyConditionSuppressesInheritedLavaExclusionInPresentation()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "Honey Catch"));
            var fishingIndex = new FishingSourceIndex(
            [
                new FishingSourceRelation(
                    1,
                    new FishingSourceVariant([FishingSourceConditionKind.InHoney], [FishingSourceConditionKind.InLava]))
            ]);
            var model = new ItemDetailsModel(
                catalog,
                new ChecklistState(catalog),
                new ItemTextIndex(catalog),
                fishingSourceIndex: fishingIndex);

            Assert.That(model.TryGetProjection(1, out ItemDetailsProjection projection), Is.True);
            Assert.That(projection.FishingVariants, Has.Count.EqualTo(1));
            Assert.That(projection.FishingVariants[0].Conditions, Is.EqualTo([FishingSourceConditionKind.InHoney]));
            Assert.That(projection.FishingVariants[0].ExcludedConditions, Is.Empty);
            Assert.That(projection.FishingVariants[0].ExcludesLavaAndHoney, Is.False);
        }

        [Test]
        public void TryGetProjection_DoesNotCollapseCombinedLiquidExclusionWithUnrestrictedVariant()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "Catch"));
            var fishingIndex = new FishingSourceIndex(
            [
                new FishingSourceRelation(
                    1,
                    new FishingSourceVariant(
                        Array.Empty<FishingSourceConditionKind>(),
                        [FishingSourceConditionKind.InLava, FishingSourceConditionKind.InHoney])),
                new FishingSourceRelation(1, new FishingSourceVariant(Array.Empty<FishingSourceConditionKind>()))
            ]);
            var model = new ItemDetailsModel(
                catalog,
                new ChecklistState(catalog),
                new ItemTextIndex(catalog),
                fishingSourceIndex: fishingIndex);

            Assert.That(model.TryGetProjection(1, out ItemDetailsProjection projection), Is.True);
            Assert.That(projection.FishingVariants, Has.Count.EqualTo(2));
            Assert.That(projection.FishingVariants[0].ExcludesLavaAndHoney, Is.True);
            Assert.That(projection.FishingVariants[0].ExcludedConditions, Is.Empty);
            Assert.That(projection.FishingVariants[1].ExcludesLavaAndHoney, Is.False);
            Assert.That(projection.FishingVariants[1].ExcludedConditions, Is.Empty);
        }

        [Test]
        public void TryGetProjection_ExpandsPositiveCompoundFishingConditionsAndPreservesExclusions()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "Catch"));
            var fishingIndex = new FishingSourceIndex(
            [
                new FishingSourceRelation(
                    1,
                    new FishingSourceVariant(
                        [FishingSourceConditionKind.Height1And2],
                        [FishingSourceConditionKind.InLava]))
            ]);
            var model = new ItemDetailsModel(
                catalog,
                new ChecklistState(catalog),
                new ItemTextIndex(catalog),
                fishingSourceIndex: fishingIndex);

            Assert.That(model.TryGetProjection(1, out ItemDetailsProjection projection), Is.True);
            Assert.That(projection.FishingVariants, Has.Count.EqualTo(2));
            Assert.That(projection.FishingVariants[0].Conditions, Is.EqualTo([FishingSourceConditionKind.Height1]));
            Assert.That(projection.FishingVariants[1].Conditions, Is.EqualTo([FishingSourceConditionKind.Height2]));
            Assert.That(
                projection.FishingVariants[0].ExcludedConditions,
                Is.EqualTo([FishingSourceConditionKind.InLava]));
            Assert.That(
                projection.FishingVariants[1].ExcludedConditions,
                Is.EqualTo([FishingSourceConditionKind.InLava]));
        }

        [Test]
        public void TryGetProjection_DoesNotCollapseFishingVariantsWithDifferentExclusions()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "Catch"));
            var fishingIndex = new FishingSourceIndex(
            [
                new FishingSourceRelation(
                    1,
                    new FishingSourceVariant([FishingSourceConditionKind.Jungle], [FishingSourceConditionKind.InLava])),
                new FishingSourceRelation(
                    1,
                    new FishingSourceVariant([FishingSourceConditionKind.Jungle], [FishingSourceConditionKind.InHoney]))
            ]);
            var model = new ItemDetailsModel(
                catalog,
                new ChecklistState(catalog),
                new ItemTextIndex(catalog),
                fishingSourceIndex: fishingIndex);

            Assert.That(model.TryGetProjection(1, out ItemDetailsProjection projection), Is.True);
            Assert.That(projection.FishingVariants, Has.Count.EqualTo(2));
            Assert.That(
                projection.FishingVariants[0].ExcludedConditions,
                Is.EqualTo([FishingSourceConditionKind.InLava]));
            Assert.That(
                projection.FishingVariants[1].ExcludedConditions,
                Is.EqualTo([FishingSourceConditionKind.InHoney]));
        }

        [Test]
        public void TryGetProjection_CollapsesFishingVariantsWithEqualConditionsAndExclusions()
        {
            ItemCatalog catalog = CreateCatalog(new ItemCatalogEntry(1, "Catch"));
            var fishingIndex = new FishingSourceIndex(
            [
                new FishingSourceRelation(
                    1,
                    new FishingSourceVariant([FishingSourceConditionKind.Jungle], [FishingSourceConditionKind.InLava])),
                new FishingSourceRelation(
                    1,
                    new FishingSourceVariant([FishingSourceConditionKind.Jungle], [FishingSourceConditionKind.InLava]))
            ]);
            var model = new ItemDetailsModel(
                catalog,
                new ChecklistState(catalog),
                new ItemTextIndex(catalog),
                fishingSourceIndex: fishingIndex);

            Assert.That(model.TryGetProjection(1, out ItemDetailsProjection projection), Is.True);
            Assert.That(projection.FishingVariants, Has.Count.EqualTo(1));
            Assert.That(projection.FishingVariants[0].Conditions, Is.EqualTo([FishingSourceConditionKind.Jungle]));
            Assert.That(
                projection.FishingVariants[0].ExcludedConditions,
                Is.EqualTo([FishingSourceConditionKind.InLava]));
        }

        private static ItemCatalog CreateCatalog(params ItemCatalogEntry[] entries)
        {
            return ItemCatalog.Create(entries);
        }

        private static void ReplaceItemTextNames(
            ItemTextIndex itemTextIndex,
            ItemCatalog catalog,
            IReadOnlyDictionary<int, string> overrides,
            string cultureName)
        {
            var names = new Dictionary<int, string>(catalog.Count);
            var descriptions = new Dictionary<int, string>(catalog.Count);

            foreach (ItemCatalogEntry entry in catalog.Items)
            {
                names.Add(
                    entry.Id,
                    overrides != null && overrides.TryGetValue(entry.Id, out string name) ? name : entry.Name);
                descriptions.Add(entry.Id, string.Empty);
            }

            itemTextIndex.ReplaceSnapshot(cultureName, names, descriptions);
        }

        private static JourneyResearchState CreateResearchState(params JourneyResearchDefinition[] definitions)
        {
            return new JourneyResearchState(definitions);
        }

        private static RecipeCatalog CreateRecipeCatalog(params RecipeCatalogEntry[] recipes)
        {
            return RecipeCatalog.Create(recipes);
        }

        private static RecipeCatalogEntry CreateRecipeEntryWithRequiredTile(
            int runtimeIndex,
            int resultItemId,
            int requiredTileId)
        {
            return new RecipeCatalogEntry(
                runtimeIndex,
                resultItemId,
                1,
                [],
                new RecipeEnvironmentRequirements(
                    requiredTileId,
                    requiresWater: false,
                    requiresHoney: false,
                    requiresLava: false,
                    requiresSnowBiome: false,
                    requiresGraveyardBiome: false,
                    requiresMechdusa: false,
                    requiresTorchGodsFavor: false),
                isAlchemy: false);
        }

        private static RecipeCatalogEntry CreateRecipeEntry(
            int runtimeIndex,
            int resultItemId,
            params int[] ingredientItemIds)
        {
            var ingredients = new RecipeIngredient[ingredientItemIds.Length];

            for (var index = 0; index < ingredientItemIds.Length; index++)
            {
                int itemId = ingredientItemIds[index];
                ingredients[index] = new RecipeIngredient(itemId, 1, RecipeIngredientRequirement.ForItem(itemId));
            }

            return new RecipeCatalogEntry(
                runtimeIndex,
                resultItemId,
                1,
                ingredients,
                new RecipeEnvironmentRequirements(null, false, false, false, false, false, false, false),
                isAlchemy: false);
        }
    }
}