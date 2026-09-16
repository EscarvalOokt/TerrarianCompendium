using System;
using TerrarianCompendium.Filtering;

namespace TerrarianCompendium.Recipes
{
    internal sealed class RecipeFilterState
    {
        private const RecipeEnvironmentRequirementFilter SupportedEnvironmentRequirements =
            RecipeEnvironmentRequirementFilter.Water |
            RecipeEnvironmentRequirementFilter.Honey |
            RecipeEnvironmentRequirementFilter.Lava |
            RecipeEnvironmentRequirementFilter.SnowBiome |
            RecipeEnvironmentRequirementFilter.GraveyardBiome |
            RecipeEnvironmentRequirementFilter.Mechdusa |
            RecipeEnvironmentRequirementFilter.TorchGodsFavor;

        private ChecklistCompletionFilter _completionFilter = ChecklistCompletionFilter.All;
        private bool _craftableNowOnly;
        private RecipeEnvironmentRequirementFilter _environmentRequirements;
        private bool _favoritesOnly;
        private bool _noRequirementsOnly;
        private int? _requiredTileId;
        private ChecklistResearchFilter _researchFilter = ChecklistResearchFilter.All;
        private long _revision;

        public int? RequiredTileId
        {
            get => _requiredTileId;
            set
            {
                if (value is < 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(value),
                        value,
                        "Required tile ID must not be negative when present.");
                }

                bool disablesNoRequirements = value.HasValue && _noRequirementsOnly;

                if (_requiredTileId == value && !disablesNoRequirements)
                    return;

                _requiredTileId = value;

                if (disablesNoRequirements)
                    _noRequirementsOnly = false;

                _revision++;
            }
        }

        public RecipeEnvironmentRequirementFilter EnvironmentRequirements
        {
            get => _environmentRequirements;
            set
            {
                ValidateEnvironmentRequirements(value, nameof(value));
                bool disablesNoRequirements = value != RecipeEnvironmentRequirementFilter.None && _noRequirementsOnly;

                if (_environmentRequirements == value && !disablesNoRequirements)
                    return;

                _environmentRequirements = value;

                if (disablesNoRequirements)
                    _noRequirementsOnly = false;

                _revision++;
            }
        }

        public bool NoRequirementsOnly
        {
            get => _noRequirementsOnly;
            set
            {
                if (_noRequirementsOnly == value)
                    return;

                _noRequirementsOnly = value;

                if (value)
                {
                    _requiredTileId = null;
                    _environmentRequirements = RecipeEnvironmentRequirementFilter.None;
                }

                _revision++;
            }
        }

        public ChecklistCompletionFilter CompletionFilter
        {
            get => _completionFilter;
            set
            {
                ValidateCompletionFilter(value, nameof(value));

                if (_completionFilter == value)
                    return;

                _completionFilter = value;
                _revision++;
            }
        }

        public ChecklistResearchFilter ResearchFilter
        {
            get => _researchFilter;
            set
            {
                ValidateResearchFilter(value, nameof(value));

                if (_researchFilter == value)
                    return;

                _researchFilter = value;
                _revision++;
            }
        }

        public bool CraftableNowOnly
        {
            get => _craftableNowOnly;
            set
            {
                if (_craftableNowOnly == value)
                    return;

                _craftableNowOnly = value;
                _revision++;
            }
        }

        public bool FavoritesOnly
        {
            get => _favoritesOnly;
            set
            {
                if (_favoritesOnly == value)
                    return;

                _favoritesOnly = value;
                _revision++;
            }
        }

        public bool IsActive =>
            _noRequirementsOnly ||
            _requiredTileId.HasValue ||
            _environmentRequirements != RecipeEnvironmentRequirementFilter.None ||
            _completionFilter != ChecklistCompletionFilter.All ||
            _researchFilter != ChecklistResearchFilter.All ||
            _craftableNowOnly ||
            _favoritesOnly;

        public int ActiveFilterCount
        {
            get
            {
                var count = 0;

                if (_noRequirementsOnly)
                {
                    count++;
                }
                else
                {
                    if (_requiredTileId.HasValue)
                        count++;

                    var requirements = (int)_environmentRequirements;

                    while (requirements != 0)
                    {
                        count += requirements & 1;
                        requirements >>= 1;
                    }
                }

                if (_completionFilter != ChecklistCompletionFilter.All)
                    count++;

                if (_researchFilter != ChecklistResearchFilter.All)
                    count++;

                if (_craftableNowOnly)
                    count++;

                if (_favoritesOnly)
                    count++;

                return count;
            }
        }

        public bool UsesChecklistState => _completionFilter != ChecklistCompletionFilter.All;

        public bool UsesJourneyResearchState => _researchFilter != ChecklistResearchFilter.All;

        public bool UsesCraftingAvailabilityState => _craftableNowOnly;

        public bool UsesRecipeFavoriteState => _favoritesOnly;

        public long Revision => _revision;

        public void Clear()
        {
            if (!IsActive)
                return;

            _requiredTileId = null;
            _environmentRequirements = RecipeEnvironmentRequirementFilter.None;
            _noRequirementsOnly = false;
            _completionFilter = ChecklistCompletionFilter.All;
            _researchFilter = ChecklistResearchFilter.All;
            _craftableNowOnly = false;
            _favoritesOnly = false;
            _revision++;
        }

        public bool IsEnvironmentRequirementEnabled(RecipeEnvironmentRequirementFilter requirement)
        {
            ValidateSingleEnvironmentRequirement(requirement, nameof(requirement));
            return (_environmentRequirements & requirement) != 0;
        }

        public void SetEnvironmentRequirement(RecipeEnvironmentRequirementFilter requirement, bool enabled)
        {
            ValidateSingleEnvironmentRequirement(requirement, nameof(requirement));

            RecipeEnvironmentRequirementFilter next = enabled
                ? _environmentRequirements | requirement
                : _environmentRequirements & ~requirement;

            EnvironmentRequirements = next;
        }

        public bool MatchesRequirements(RecipeCatalogEntry recipe)
        {
            if (recipe == null)
                throw new ArgumentNullException(nameof(recipe));

            RecipeEnvironmentRequirements requirements = recipe.EnvironmentRequirements;

            if (_noRequirementsOnly)
                return HasNoRequirements(requirements);

            if (_requiredTileId.HasValue && requirements.RequiredTileId != _requiredTileId)
                return false;

            if ((_environmentRequirements & RecipeEnvironmentRequirementFilter.Water) != 0 &&
                !requirements.RequiresWater)
                return false;

            if ((_environmentRequirements & RecipeEnvironmentRequirementFilter.Honey) != 0 &&
                !requirements.RequiresHoney)
                return false;

            if ((_environmentRequirements & RecipeEnvironmentRequirementFilter.Lava) != 0 && !requirements.RequiresLava)
                return false;

            if ((_environmentRequirements & RecipeEnvironmentRequirementFilter.SnowBiome) != 0 &&
                !requirements.RequiresSnowBiome)
            {
                return false;
            }

            if ((_environmentRequirements & RecipeEnvironmentRequirementFilter.GraveyardBiome) != 0 &&
                !requirements.RequiresGraveyardBiome)
            {
                return false;
            }

            if ((_environmentRequirements & RecipeEnvironmentRequirementFilter.Mechdusa) != 0 &&
                !requirements.RequiresMechdusa)
            {
                return false;
            }

            if ((_environmentRequirements & RecipeEnvironmentRequirementFilter.TorchGodsFavor) != 0 &&
                !requirements.RequiresTorchGodsFavor)
            {
                return false;
            }

            return true;
        }

        private static bool HasNoRequirements(RecipeEnvironmentRequirements requirements)
        {
            return !requirements.RequiredTileId.HasValue &&
                   !requirements.RequiresWater &&
                   !requirements.RequiresHoney &&
                   !requirements.RequiresLava &&
                   !requirements.RequiresSnowBiome &&
                   !requirements.RequiresGraveyardBiome &&
                   !requirements.RequiresMechdusa &&
                   !requirements.RequiresTorchGodsFavor;
        }

        private static void ValidateCompletionFilter(ChecklistCompletionFilter filter, string parameterName)
        {
            if (filter != ChecklistCompletionFilter.All &&
                filter != ChecklistCompletionFilter.Missing &&
                filter != ChecklistCompletionFilter.Found)
            {
                throw new ArgumentOutOfRangeException(parameterName, filter, "Unsupported completion filter.");
            }
        }

        private static void ValidateResearchFilter(ChecklistResearchFilter filter, string parameterName)
        {
            if (filter != ChecklistResearchFilter.All &&
                filter != ChecklistResearchFilter.Researched &&
                filter != ChecklistResearchFilter.Unresearched)
            {
                throw new ArgumentOutOfRangeException(parameterName, filter, "Unsupported research filter.");
            }
        }

        private static void ValidateEnvironmentRequirements(
            RecipeEnvironmentRequirementFilter requirements,
            string parameterName)
        {
            if ((requirements & ~SupportedEnvironmentRequirements) != 0)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    requirements,
                    "Unsupported recipe environment requirement filter.");
            }
        }

        private static void ValidateSingleEnvironmentRequirement(
            RecipeEnvironmentRequirementFilter requirement,
            string parameterName)
        {
            ValidateEnvironmentRequirements(requirement, parameterName);

            var numericRequirement = (int)requirement;

            if (requirement == RecipeEnvironmentRequirementFilter.None ||
                (numericRequirement & (numericRequirement - 1)) != 0)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    requirement,
                    "Exactly one supported recipe environment requirement must be specified.");
            }
        }
    }
}