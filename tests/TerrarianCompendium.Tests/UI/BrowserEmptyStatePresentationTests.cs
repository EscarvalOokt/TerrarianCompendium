using System;
using NUnit.Framework;
using TerrarianCompendium.Localization;
using TerrarianCompendium.UI;

namespace TerrarianCompendium.Tests.UI
{
    [TestFixture]
    public sealed class BrowserEmptyStatePresentationTests
    {
        private static readonly CompendiumLocalization _localization = CompendiumLocalization.LoadFromAssembly(
            typeof(BrowserEmptyStatePresentation).Assembly,
            CompendiumLocalization.SourceCultureName);

        private static readonly object[] _narrowingCases =
        [
            new object[] { false, false, false, null },
            new object[] { true, false, false, CompendiumTextKeys.Common.EmptyReasonSearch },
            new object[] { false, true, false, CompendiumTextKeys.Common.EmptyReasonCategory },
            new object[] { false, false, true, CompendiumTextKeys.Common.EmptyReasonFilters },
            new object[] { true, true, false, CompendiumTextKeys.Common.EmptyReasonSearchCategory },
            new object[] { true, false, true, CompendiumTextKeys.Common.EmptyReasonSearchFilters },
            new object[] { false, true, true, CompendiumTextKeys.Common.EmptyReasonCategoryFilters },
            new object[]
            {
                true,
                true,
                true,
                CompendiumTextKeys.Common.EmptyReasonSearchCategoryFilters
            }
        ];

        private static readonly object[] _bestiaryCases =
        [
            new object[] { false, false, null },
            new object[] { true, false, CompendiumTextKeys.Common.EmptyReasonSearch },
            new object[] { false, true, CompendiumTextKeys.Common.EmptyReasonFilters },
            new object[] { true, true, CompendiumTextKeys.Common.EmptyReasonSearchFilters }
        ];

        private static readonly object[] _resolvedSourceCases =
        [
            new object[]
            {
                BrowserEmptyStateDomain.Recipes,
                false,
                true,
                false,
                true,
                "No results from recipes using this item are available in this category."
            },
            new object[]
            {
                BrowserEmptyStateDomain.Recipes,
                true,
                false,
                false,
                true,
                "No results from recipes using this item match your search."
            },
            new object[]
            {
                BrowserEmptyStateDomain.Recipes,
                false,
                false,
                true,
                true,
                "No results from recipes using this item match the active filters."
            },
            new object[]
            {
                BrowserEmptyStateDomain.Recipes,
                false,
                true,
                true,
                false,
                "No recipe results in this category match the active filters."
            },
            new object[]
            {
                BrowserEmptyStateDomain.Recipes,
                true,
                false,
                false,
                false,
                "No recipe results match your search."
            },
            new object[]
            {
                BrowserEmptyStateDomain.Shimmer,
                false,
                true,
                false,
                true,
                "No Shimmer results from this item are available in this category."
            },
            new object[]
            {
                BrowserEmptyStateDomain.Items,
                false,
                true,
                true,
                false,
                "No items in this category match the active filters."
            },
            new object[]
            {
                BrowserEmptyStateDomain.Bestiary,
                false,
                false,
                true,
                false,
                "No NPCs match the active filters."
            },
            new object[]
            {
                BrowserEmptyStateDomain.Recipes,
                false,
                false,
                false,
                true,
                "No results from recipes using this item."
            },
            new object[]
            {
                BrowserEmptyStateDomain.Shimmer,
                false,
                false,
                false,
                true,
                "No Shimmer results from this item."
            }
        ];

        [TestCaseSource(nameof(_resolvedSourceCases))]
        public void Resolve_SourceLocale_UsesNaturalPhrasing(
            object domainValue,
            bool hasSearch,
            bool hasCategory,
            bool hasFilters,
            bool hasContextItem,
            string expected)
        {
            var presentation = BrowserEmptyStatePresentation.Create(
                (BrowserEmptyStateDomain)domainValue,
                hasSearch,
                hasCategory,
                hasFilters,
                hasContextItem);

            Assert.That(presentation.Resolve(_localization), Is.EqualTo(expected));
        }

        [TestCaseSource(nameof(_narrowingCases))]
        public void Create_Items_UsesExpectedNarrowingPresentation(
            bool hasSearch,
            bool hasCategory,
            bool hasFilters,
            string expectedReasonKey)
        {
            var presentation = BrowserEmptyStatePresentation.Create(
                BrowserEmptyStateDomain.Items,
                hasSearch,
                hasCategory,
                hasFilters,
                hasContextItem: false);

            Assert.Multiple(() =>
            {
                Assert.That(
                    presentation.TextKey,
                    Is.EqualTo(
                        expectedReasonKey == null
                            ? CompendiumTextKeys.Items.EmptyState
                            : CompendiumTextKeys.Items.EmptyStateNarrowed));
                Assert.That(presentation.ReasonKey, Is.EqualTo(expectedReasonKey));
            });
        }

        [TestCaseSource(nameof(_narrowingCases))]
        public void Create_Recipes_Global_UsesExpectedNarrowingPresentation(
            bool hasSearch,
            bool hasCategory,
            bool hasFilters,
            string expectedReasonKey)
        {
            var presentation = BrowserEmptyStatePresentation.Create(
                BrowserEmptyStateDomain.Recipes,
                hasSearch,
                hasCategory,
                hasFilters,
                hasContextItem: false);

            Assert.Multiple(() =>
            {
                Assert.That(
                    presentation.TextKey,
                    Is.EqualTo(
                        expectedReasonKey == null
                            ? CompendiumTextKeys.Recipes.EmptyState
                            : CompendiumTextKeys.Recipes.EmptyStateNarrowed));
                Assert.That(presentation.ReasonKey, Is.EqualTo(expectedReasonKey));
            });
        }

        [TestCaseSource(nameof(_narrowingCases))]
        public void Create_Recipes_Context_UsesExpectedNarrowingPresentation(
            bool hasSearch,
            bool hasCategory,
            bool hasFilters,
            string expectedReasonKey)
        {
            var presentation = BrowserEmptyStatePresentation.Create(
                BrowserEmptyStateDomain.Recipes,
                hasSearch,
                hasCategory,
                hasFilters,
                hasContextItem: true);

            Assert.Multiple(() =>
            {
                Assert.That(
                    presentation.TextKey,
                    Is.EqualTo(
                        expectedReasonKey == null
                            ? CompendiumTextKeys.Recipes.EmptyStateContext
                            : CompendiumTextKeys.Recipes.EmptyStateContextNarrowed));
                Assert.That(presentation.ReasonKey, Is.EqualTo(expectedReasonKey));
            });
        }

        [TestCase(false, false, false)]
        [TestCase(true, false, false)]
        [TestCase(true, true, true)]
        public void Create_Recipes_ContextWithoutRelations_ExplainsMissingRelationsRegardlessOfFilters(
            bool hasSearch,
            bool hasCategory,
            bool hasFilters)
        {
            var presentation = BrowserEmptyStatePresentation.Create(
                BrowserEmptyStateDomain.Recipes,
                hasSearch,
                hasCategory,
                hasFilters,
                hasContextItem: true,
                contextHasNoRecipeRelations: true);

            Assert.Multiple(() =>
            {
                Assert.That(presentation.TextKey, Is.EqualTo(CompendiumTextKeys.Recipes.EmptyStateNoRelations));
                Assert.That(presentation.ReasonKey, Is.Null);
                Assert.That(
                    presentation.Resolve(_localization),
                    Is.EqualTo("No recipe creates this item, and this item is not used in any recipes."));
            });
        }

        [Test]
        public void Resolve_RussianLocale_ContextWithoutRelations_UsesTranslatedMessage()
        {
            var russianLocalization = CompendiumLocalization.LoadFromAssembly(
                typeof(BrowserEmptyStatePresentation).Assembly,
                "ru-RU");
            var presentation = BrowserEmptyStatePresentation.Create(
                BrowserEmptyStateDomain.Recipes,
                hasSearch: false,
                hasCategory: false,
                hasFilters: false,
                hasContextItem: true,
                contextHasNoRecipeRelations: true);

            Assert.That(
                presentation.Resolve(russianLocalization),
                Is.EqualTo("У этого предмета нет рецепта создания, и он не используется ни в одном рецепте."));
        }

        [Test]
        public void Create_Recipes_NoRelationsRequiresContext()
        {
            Assert.Throws<ArgumentException>(() => BrowserEmptyStatePresentation.Create(
                BrowserEmptyStateDomain.Recipes,
                hasSearch: false,
                hasCategory: false,
                hasFilters: false,
                hasContextItem: false,
                contextHasNoRecipeRelations: true));
        }

        [TestCaseSource(nameof(_narrowingCases))]
        public void Create_Shimmer_Global_UsesExpectedNarrowingPresentation(
            bool hasSearch,
            bool hasCategory,
            bool hasFilters,
            string expectedReasonKey)
        {
            var presentation = BrowserEmptyStatePresentation.Create(
                BrowserEmptyStateDomain.Shimmer,
                hasSearch,
                hasCategory,
                hasFilters,
                hasContextItem: false);

            Assert.Multiple(() =>
            {
                Assert.That(
                    presentation.TextKey,
                    Is.EqualTo(
                        expectedReasonKey == null
                            ? CompendiumTextKeys.Shimmer.EmptyState
                            : CompendiumTextKeys.Shimmer.EmptyStateNarrowed));
                Assert.That(presentation.ReasonKey, Is.EqualTo(expectedReasonKey));
            });
        }

        [TestCaseSource(nameof(_narrowingCases))]
        public void Create_Shimmer_Context_UsesExpectedNarrowingPresentation(
            bool hasSearch,
            bool hasCategory,
            bool hasFilters,
            string expectedReasonKey)
        {
            var presentation = BrowserEmptyStatePresentation.Create(
                BrowserEmptyStateDomain.Shimmer,
                hasSearch,
                hasCategory,
                hasFilters,
                hasContextItem: true);

            Assert.Multiple(() =>
            {
                Assert.That(
                    presentation.TextKey,
                    Is.EqualTo(
                        expectedReasonKey == null
                            ? CompendiumTextKeys.Shimmer.EmptyStateContext
                            : CompendiumTextKeys.Shimmer.EmptyStateContextNarrowed));
                Assert.That(presentation.ReasonKey, Is.EqualTo(expectedReasonKey));
            });
        }

        [TestCaseSource(nameof(_bestiaryCases))]
        public void Create_Bestiary_UsesExpectedNarrowingPresentation(
            bool hasSearch,
            bool hasFilters,
            string expectedReasonKey)
        {
            var presentation = BrowserEmptyStatePresentation.Create(
                BrowserEmptyStateDomain.Bestiary,
                hasSearch,
                hasCategory: false,
                hasFilters: hasFilters,
                hasContextItem: false);

            Assert.Multiple(() =>
            {
                Assert.That(
                    presentation.TextKey,
                    Is.EqualTo(
                        expectedReasonKey == null
                            ? CompendiumTextKeys.Bestiary.EmptyState
                            : CompendiumTextKeys.Bestiary.EmptyStateNarrowed));
                Assert.That(presentation.ReasonKey, Is.EqualTo(expectedReasonKey));
            });
        }
    }
}