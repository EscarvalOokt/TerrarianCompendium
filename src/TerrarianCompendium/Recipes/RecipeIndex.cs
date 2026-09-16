using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TerrarianCompendium.Recipes
{
    internal sealed class RecipeIndex
    {
        private static readonly IReadOnlyList<RecipeCatalogEntry> _emptyRecipes =
            new ReadOnlyCollection<RecipeCatalogEntry>(new List<RecipeCatalogEntry>());

        private readonly Dictionary<int, IReadOnlyList<RecipeCatalogEntry>> _recipesProducingByItemId;
        private readonly Dictionary<int, IReadOnlyList<RecipeCatalogEntry>> _recipesUsingByItemId;

        private RecipeIndex(
            Dictionary<int, IReadOnlyList<RecipeCatalogEntry>> recipesProducingByItemId,
            Dictionary<int, IReadOnlyList<RecipeCatalogEntry>> recipesUsingByItemId)
        {
            _recipesProducingByItemId = recipesProducingByItemId;
            _recipesUsingByItemId = recipesUsingByItemId;
        }

        public static RecipeIndex Create(RecipeCatalog catalog)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));

            var recipesProducingByItemId = new Dictionary<int, List<RecipeCatalogEntry>>();
            var recipesUsingByItemId = new Dictionary<int, List<RecipeCatalogEntry>>();

            foreach (RecipeCatalogEntry recipe in catalog.Recipes)
            {
                AddRelation(recipesProducingByItemId, recipe.ResultItemId, recipe);

                var validIngredientItemIds = new HashSet<int>();

                foreach (RecipeIngredient ingredient in recipe.Ingredients)
                {
                    foreach (int itemId in ingredient.Requirement.ValidItemIds)
                        validIngredientItemIds.Add(itemId);
                }

                foreach (int itemId in validIngredientItemIds)
                    AddRelation(recipesUsingByItemId, itemId, recipe);
            }

            return new RecipeIndex(FreezeRelations(recipesProducingByItemId), FreezeRelations(recipesUsingByItemId));
        }

        public IReadOnlyList<RecipeCatalogEntry> GetRecipesProducing(int itemId)
        {
            return _recipesProducingByItemId.TryGetValue(itemId, out IReadOnlyList<RecipeCatalogEntry> recipes)
                ? recipes
                : _emptyRecipes;
        }

        public IReadOnlyList<RecipeCatalogEntry> GetRecipesUsing(int itemId)
        {
            return _recipesUsingByItemId.TryGetValue(itemId, out IReadOnlyList<RecipeCatalogEntry> recipes)
                ? recipes
                : _emptyRecipes;
        }

        public bool HasRecipeProducing(int itemId)
        {
            return _recipesProducingByItemId.ContainsKey(itemId);
        }

        private static void AddRelation(
            Dictionary<int, List<RecipeCatalogEntry>> relations,
            int itemId,
            RecipeCatalogEntry recipe)
        {
            if (!relations.TryGetValue(itemId, out List<RecipeCatalogEntry> recipes))
            {
                recipes = new List<RecipeCatalogEntry>();
                relations.Add(itemId, recipes);
            }

            recipes.Add(recipe);
        }

        private static Dictionary<int, IReadOnlyList<RecipeCatalogEntry>> FreezeRelations(
            Dictionary<int, List<RecipeCatalogEntry>> relations)
        {
            var frozenRelations = new Dictionary<int, IReadOnlyList<RecipeCatalogEntry>>(relations.Count);

            foreach (KeyValuePair<int, List<RecipeCatalogEntry>> relation in relations)
            {
                var recipes = new List<RecipeCatalogEntry>(relation.Value);
                frozenRelations.Add(relation.Key, new ReadOnlyCollection<RecipeCatalogEntry>(recipes));
            }

            return frozenRelations;
        }
    }
}