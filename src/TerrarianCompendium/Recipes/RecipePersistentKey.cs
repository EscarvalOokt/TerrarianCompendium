using System;
using System.Globalization;
using System.Text;

namespace TerrarianCompendium.Recipes
{
    internal sealed class RecipePersistentKey : IEquatable<RecipePersistentKey>
    {
        public const int CurrentFormatVersion = 1;

        private const string CurrentVersionToken = "v1";
        private const string CurrentPrefix = CurrentVersionToken + "|";

        private RecipePersistentKey(string value)
        {
            Value = value;
        }

        public string Value { get; }

        public bool Equals(RecipePersistentKey other)
        {
            return other != null && string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public static RecipePersistentKey Create(RecipeCatalogEntry recipe)
        {
            if (recipe == null)
                throw new ArgumentNullException(nameof(recipe));

            var builder = new StringBuilder();
            builder.Append(CurrentPrefix);
            builder.Append("r:");
            AppendInt(builder, recipe.ResultItemId);
            builder.Append(':');
            AppendInt(builder, recipe.ResultStack);
            builder.Append("|n:");
            AppendInt(builder, recipe.Ingredients.Count);

            foreach (RecipeIngredient ingredient in recipe.Ingredients)
                AppendIngredient(builder, ingredient);

            RecipeEnvironmentRequirements environment = recipe.EnvironmentRequirements;
            builder.Append("|t:");

            if (environment.RequiredTileId.HasValue)
                AppendInt(builder, environment.RequiredTileId.Value);
            else
                builder.Append('-');

            builder.Append("|e:");
            AppendBool(builder, environment.RequiresWater);
            AppendBool(builder, environment.RequiresHoney);
            AppendBool(builder, environment.RequiresLava);
            AppendBool(builder, environment.RequiresSnowBiome);
            AppendBool(builder, environment.RequiresGraveyardBiome);
            AppendBool(builder, environment.RequiresMechdusa);
            AppendBool(builder, environment.RequiresTorchGodsFavor);
            builder.Append("|a:");
            AppendBool(builder, recipe.IsAlchemy);

            return new RecipePersistentKey(builder.ToString());
        }

        public static bool TryParse(string value, out RecipePersistentKey key)
        {
            key = null;

            if (string.IsNullOrEmpty(value))
                return false;

            string[] parts = value.Split('|');

            if (parts.Length < 6 || !string.Equals(parts[0], CurrentVersionToken, StringComparison.Ordinal))
                return false;

            if (!TryValidateResultPart(parts[1]) || !TryParseIngredientCount(parts[2], out int ingredientCount))
                return false;

            int expectedPartCount = 6 + ingredientCount;

            if (parts.Length != expectedPartCount)
                return false;

            for (var ingredientIndex = 0; ingredientIndex < ingredientCount; ingredientIndex++)
            {
                if (!TryValidateIngredientPart(parts[3 + ingredientIndex]))
                    return false;
            }

            int tailIndex = 3 + ingredientCount;

            if (!TryValidateTilePart(parts[tailIndex]) ||
                !TryValidateEnvironmentPart(parts[tailIndex + 1]) ||
                !TryValidateAlchemyPart(parts[tailIndex + 2]))
            {
                return false;
            }

            key = new RecipePersistentKey(value);

            return true;
        }

        public override bool Equals(object obj)
        {
            return obj is RecipePersistentKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value);
        }

        public override string ToString()
        {
            return Value;
        }

        public static bool operator ==(RecipePersistentKey left, RecipePersistentKey right)
        {
            if (ReferenceEquals(left, right))
                return true;

            return left?.Equals(right) == true;
        }

        public static bool operator !=(RecipePersistentKey left, RecipePersistentKey right)
        {
            return !(left == right);
        }

        private static void AppendIngredient(StringBuilder builder, RecipeIngredient ingredient)
        {
            builder.Append("|x:");

            switch (ingredient.Requirement.Kind)
            {
                case RecipeIngredientRequirementKind.Item:
                    builder.Append("i:");
                    AppendInt(builder, ingredient.DisplayItemId);
                    builder.Append(':');
                    AppendInt(builder, ingredient.Stack);
                    builder.Append(':');
                    AppendInt(builder, ingredient.Requirement.IdentityId);
                    break;

                case RecipeIngredientRequirementKind.RecipeGroup:
                    builder.Append("g:");
                    AppendInt(builder, ingredient.DisplayItemId);
                    builder.Append(':');
                    AppendInt(builder, ingredient.Stack);
                    builder.Append(':');
                    AppendRecipeGroupMembers(builder, ingredient.Requirement);
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Unsupported recipe ingredient requirement kind '{ingredient.Requirement.Kind}'.");
            }
        }

        private static void AppendRecipeGroupMembers(StringBuilder builder, RecipeIngredientRequirement requirement)
        {
            var memberIds = new int[requirement.ValidItemIds.Count];

            for (var index = 0; index < requirement.ValidItemIds.Count; index++)
                memberIds[index] = requirement.ValidItemIds[index];

            Array.Sort(memberIds);

            for (var index = 0; index < memberIds.Length; index++)
            {
                if (index > 0)
                    builder.Append(',');

                AppendInt(builder, memberIds[index]);
            }
        }

        private static void AppendBool(StringBuilder builder, bool value)
        {
            builder.Append(value ? '1' : '0');
        }

        private static void AppendInt(StringBuilder builder, int value)
        {
            builder.Append(value.ToString(CultureInfo.InvariantCulture));
        }

        private static bool TryValidateResultPart(string part)
        {
            string[] tokens = part.Split(':');

            return tokens.Length == 3 &&
                   string.Equals(tokens[0], "r", StringComparison.Ordinal) &&
                   IsCanonicalPositiveInt(tokens[1]) &&
                   IsCanonicalPositiveInt(tokens[2]);
        }

        private static bool TryParseIngredientCount(string part, out int ingredientCount)
        {
            ingredientCount = 0;
            string[] tokens = part.Split(':');

            if (tokens.Length != 2 || !string.Equals(tokens[0], "n", StringComparison.Ordinal))
                return false;

            return TryParseCanonicalNonNegativeInt(tokens[1], out ingredientCount);
        }

        private static bool TryValidateIngredientPart(string part)
        {
            string[] tokens = part.Split(':');

            if (tokens.Length != 5 || !string.Equals(tokens[0], "x", StringComparison.Ordinal))
                return false;

            if (!IsCanonicalPositiveInt(tokens[2]) || !IsCanonicalPositiveInt(tokens[3]))
                return false;

            if (string.Equals(tokens[1], "i", StringComparison.Ordinal))
                return IsCanonicalPositiveInt(tokens[4]);

            if (!string.Equals(tokens[1], "g", StringComparison.Ordinal))
                return false;

            string[] memberTokens = tokens[4].Split(',');

            if (memberTokens.Length == 0)
                return false;

            var previousMemberId = 0;

            for (var index = 0; index < memberTokens.Length; index++)
            {
                if (!TryParseCanonicalPositiveInt(memberTokens[index], out int memberId) ||
                    memberId <= previousMemberId)
                    return false;

                previousMemberId = memberId;
            }

            return true;
        }

        private static bool TryValidateTilePart(string part)
        {
            string[] tokens = part.Split(':');

            if (tokens.Length != 2 || !string.Equals(tokens[0], "t", StringComparison.Ordinal))
                return false;

            if (string.Equals(tokens[1], "-", StringComparison.Ordinal))
                return true;

            return TryParseCanonicalNonNegativeInt(tokens[1], out _);
        }

        private static bool TryValidateEnvironmentPart(string part)
        {
            if (!part.StartsWith("e:", StringComparison.Ordinal) || part.Length != 9)
                return false;

            for (var index = 2; index < part.Length; index++)
            {
                if (part[index] != '0' && part[index] != '1')
                    return false;
            }

            return true;
        }

        private static bool TryValidateAlchemyPart(string part)
        {
            return string.Equals(part, "a:0", StringComparison.Ordinal) ||
                   string.Equals(part, "a:1", StringComparison.Ordinal);
        }

        private static bool IsCanonicalPositiveInt(string value)
        {
            return TryParseCanonicalPositiveInt(value, out _);
        }

        private static bool TryParseCanonicalPositiveInt(string value, out int parsedValue)
        {
            parsedValue = 0;

            return TryParseCanonicalNonNegativeInt(value, out parsedValue) && parsedValue > 0;
        }

        private static bool TryParseCanonicalNonNegativeInt(string value, out int parsedValue)
        {
            parsedValue = 0;

            if (string.IsNullOrEmpty(value) ||
                !int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out parsedValue) ||
                parsedValue < 0)
            {
                return false;
            }

            return string.Equals(value, parsedValue.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        }
    }
}