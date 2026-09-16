using System;

namespace TerrarianCompendium.Navigation
{
    internal enum BrowserDestinationKind
    {
        SectionRoot,
        Item,
        ArmorSet,
        RecipeQuery,
        Recipe,
        Npc
    }

    internal readonly struct BrowserDestination : IEquatable<BrowserDestination>
    {
        private BrowserDestination(
            BrowserDestinationKind kind,
            BrowserSection section,
            int itemId,
            int armorSetId,
            int recipeRuntimeIndex,
            int recipeQueryItemId,
            int npcNetId)
        {
            Kind = kind;
            Section = section;
            ItemId = itemId;
            ArmorSetId = armorSetId;
            RecipeRuntimeIndex = recipeRuntimeIndex;
            RecipeQueryItemId = recipeQueryItemId;
            NpcNetId = npcNetId;
        }

        public BrowserDestinationKind Kind { get; }

        public BrowserSection Section { get; }

        public int ItemId { get; }

        public int ArmorSetId { get; }

        public int RecipeRuntimeIndex { get; }

        public int RecipeQueryItemId { get; }

        public int NpcNetId { get; }

        public bool IsSectionRoot => Kind == BrowserDestinationKind.SectionRoot;

        public bool IsItem => Kind == BrowserDestinationKind.Item;

        public bool IsArmorSet => Kind == BrowserDestinationKind.ArmorSet;

        public bool IsRecipeQuery => Kind == BrowserDestinationKind.RecipeQuery;

        public bool IsRecipe => Kind == BrowserDestinationKind.Recipe;

        public bool IsNpc => Kind == BrowserDestinationKind.Npc;

        public bool HasRecipeQuery => IsRecipeQuery || (IsRecipe && RecipeQueryItemId > 0);

        public static BrowserDestination ForSection(BrowserSection section)
        {
            ValidateSection(section);

            return new BrowserDestination(BrowserDestinationKind.SectionRoot, section, 0, 0, 0, 0, 0);
        }

        public static BrowserDestination ForItem(int itemId)
        {
            if (itemId <= 0)
                throw new ArgumentOutOfRangeException(nameof(itemId), itemId, "Item ID must be greater than zero.");

            return new BrowserDestination(BrowserDestinationKind.Item, BrowserSection.Items, itemId, 0, 0, 0, 0);
        }

        public static BrowserDestination ForArmorSet(int armorSetId)
        {
            if (armorSetId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(armorSetId),
                    armorSetId,
                    "Armor-set ID must be greater than zero.");
            }

            return new BrowserDestination(
                BrowserDestinationKind.ArmorSet,
                BrowserSection.ArmorSets,
                0,
                armorSetId,
                0,
                0,
                0);
        }

        public static BrowserDestination ForRecipeQuery(int itemId)
        {
            ValidateRecipeQueryItemId(itemId, nameof(itemId));

            return new BrowserDestination(
                BrowserDestinationKind.RecipeQuery,
                BrowserSection.Recipes,
                0,
                0,
                0,
                itemId,
                0);
        }

        public static BrowserDestination ForRecipe(int runtimeRecipeIndex)
        {
            ValidateRuntimeRecipeIndex(runtimeRecipeIndex);

            return new BrowserDestination(
                BrowserDestinationKind.Recipe,
                BrowserSection.Recipes,
                0,
                0,
                runtimeRecipeIndex,
                0,
                0);
        }

        public static BrowserDestination ForRecipe(int runtimeRecipeIndex, int queryItemId)
        {
            ValidateRuntimeRecipeIndex(runtimeRecipeIndex);
            ValidateRecipeQueryItemId(queryItemId, nameof(queryItemId));

            return new BrowserDestination(
                BrowserDestinationKind.Recipe,
                BrowserSection.Recipes,
                0,
                0,
                runtimeRecipeIndex,
                queryItemId,
                0);
        }

        public static BrowserDestination ForNpc(int npcNetId)
        {
            return new BrowserDestination(BrowserDestinationKind.Npc, BrowserSection.Bestiary, 0, 0, 0, 0, npcNetId);
        }

        public bool Equals(BrowserDestination other)
        {
            return Kind == other.Kind &&
                   Section == other.Section &&
                   ItemId == other.ItemId &&
                   ArmorSetId == other.ArmorSetId &&
                   RecipeRuntimeIndex == other.RecipeRuntimeIndex &&
                   RecipeQueryItemId == other.RecipeQueryItemId &&
                   NpcNetId == other.NpcNetId;
        }

        public override bool Equals(object obj)
        {
            return obj is BrowserDestination other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)Kind;
                hashCode = (hashCode * 397) ^ (int)Section;
                hashCode = (hashCode * 397) ^ ItemId;
                hashCode = (hashCode * 397) ^ ArmorSetId;
                hashCode = (hashCode * 397) ^ RecipeRuntimeIndex;
                hashCode = (hashCode * 397) ^ RecipeQueryItemId;
                hashCode = (hashCode * 397) ^ NpcNetId;

                return hashCode;
            }
        }

        public static bool operator ==(BrowserDestination left, BrowserDestination right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BrowserDestination left, BrowserDestination right)
        {
            return !left.Equals(right);
        }

        private static void ValidateSection(BrowserSection section)
        {
            switch (section)
            {
                case BrowserSection.Items:
                case BrowserSection.ArmorSets:
                case BrowserSection.Recipes:
                case BrowserSection.Bestiary:
                    return;

                default:
                    throw new ArgumentOutOfRangeException(nameof(section), section, "Unsupported browser section.");
            }
        }

        private static void ValidateRuntimeRecipeIndex(int runtimeRecipeIndex)
        {
            if (runtimeRecipeIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(runtimeRecipeIndex),
                    runtimeRecipeIndex,
                    "Runtime recipe index must not be negative.");
            }
        }

        private static void ValidateRecipeQueryItemId(int itemId, string parameterName)
        {
            if (itemId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    itemId,
                    "Recipe query item ID must be greater than zero.");
            }
        }
    }
}