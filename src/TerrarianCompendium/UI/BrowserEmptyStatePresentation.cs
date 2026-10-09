using System;
using TerrarianCompendium.Localization;

namespace TerrarianCompendium.UI
{
    internal enum BrowserEmptyStateDomain
    {
        Items,
        Recipes,
        Shimmer,
        Bestiary
    }

    internal readonly struct BrowserEmptyStatePresentation
    {
        private BrowserEmptyStatePresentation(string textKey, string reasonKey)
        {
            TextKey = textKey ?? throw new ArgumentNullException(nameof(textKey));
            ReasonKey = reasonKey;
        }

        public string TextKey { get; }

        public string ReasonKey { get; }

        public static BrowserEmptyStatePresentation Create(
            BrowserEmptyStateDomain domain,
            bool hasSearch,
            bool hasCategory,
            bool hasFilters,
            bool hasContextItem,
            bool contextHasNoRecipeRelations = false)
        {
            if (contextHasNoRecipeRelations && (domain != BrowserEmptyStateDomain.Recipes || !hasContextItem))
            {
                throw new ArgumentException(
                    "Only contextual Recipes empty-state presentation supports missing recipe relations.",
                    nameof(contextHasNoRecipeRelations));
            }

            string reasonKey = GetReasonKey(hasSearch, hasCategory, hasFilters);
            bool narrowed = reasonKey != null;

            switch (domain)
            {
                case BrowserEmptyStateDomain.Items:
                    if (hasContextItem)
                        throw new ArgumentException("Items empty-state presentation does not support item context.");

                    return new BrowserEmptyStatePresentation(
                        narrowed ? CompendiumTextKeys.Items.EmptyStateNarrowed : CompendiumTextKeys.Items.EmptyState,
                        reasonKey);

                case BrowserEmptyStateDomain.Recipes:
                    if (contextHasNoRecipeRelations)
                    {
                        return new BrowserEmptyStatePresentation(
                            CompendiumTextKeys.Recipes.EmptyStateNoRelations,
                            null);
                    }

                    return new BrowserEmptyStatePresentation(
                        hasContextItem
                            ?
                            narrowed
                                ? CompendiumTextKeys.Recipes.EmptyStateContextNarrowed
                                : CompendiumTextKeys.Recipes.EmptyStateContext
                            : narrowed
                                ? CompendiumTextKeys.Recipes.EmptyStateNarrowed
                                : CompendiumTextKeys.Recipes.EmptyState,
                        reasonKey);

                case BrowserEmptyStateDomain.Shimmer:
                    return new BrowserEmptyStatePresentation(
                        hasContextItem
                            ?
                            narrowed
                                ? CompendiumTextKeys.Shimmer.EmptyStateContextNarrowed
                                : CompendiumTextKeys.Shimmer.EmptyStateContext
                            : narrowed
                                ? CompendiumTextKeys.Shimmer.EmptyStateNarrowed
                                : CompendiumTextKeys.Shimmer.EmptyState,
                        reasonKey);

                case BrowserEmptyStateDomain.Bestiary:
                    if (hasCategory || hasContextItem)
                    {
                        throw new ArgumentException(
                            "Bestiary empty-state presentation does not support category or item context.");
                    }

                    return new BrowserEmptyStatePresentation(
                        narrowed
                            ? CompendiumTextKeys.Bestiary.EmptyStateNarrowed
                            : CompendiumTextKeys.Bestiary.EmptyState,
                        reasonKey);

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(domain),
                        domain,
                        "Unsupported browser empty-state domain.");
            }
        }

        public string Resolve(CompendiumLocalization localization)
        {
            if (localization == null)
                throw new ArgumentNullException(nameof(localization));

            return ReasonKey == null
                ? localization.Get(TextKey)
                : localization.Format(TextKey, localization.Get(ReasonKey));
        }

        private static string GetReasonKey(bool hasSearch, bool hasCategory, bool hasFilters)
        {
            if (hasSearch)
            {
                if (hasCategory)
                {
                    return hasFilters
                        ? CompendiumTextKeys.Common.EmptyReasonSearchCategoryFilters
                        : CompendiumTextKeys.Common.EmptyReasonSearchCategory;
                }

                return hasFilters
                    ? CompendiumTextKeys.Common.EmptyReasonSearchFilters
                    : CompendiumTextKeys.Common.EmptyReasonSearch;
            }

            if (hasCategory)
            {
                return hasFilters
                    ? CompendiumTextKeys.Common.EmptyReasonCategoryFilters
                    : CompendiumTextKeys.Common.EmptyReasonCategory;
            }

            return hasFilters ? CompendiumTextKeys.Common.EmptyReasonFilters : null;
        }
    }
}