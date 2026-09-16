using System;
using Terraria.GameContent.ItemDropRules;

namespace TerrarianCompendium.Bestiary
{
    internal sealed class NpcLootCondition
    {
        private readonly Func<string> _descriptionProvider;
        private readonly Func<NpcDifficultyMode, bool> _difficultyMatcher;
        private readonly bool _showAsQualifier;

        public NpcLootCondition(IItemDropRuleCondition condition)
        {
            if (condition == null)
                throw new ArgumentNullException(nameof(condition));

            _descriptionProvider = condition.GetConditionDescription;

            switch (condition)
            {
                case Conditions.NotExpert:
                case Conditions.LegacyHack_IsBossAndNotExpert:
                    _difficultyMatcher = mode => mode == NpcDifficultyMode.Classic;
                    _showAsQualifier = false;
                    break;

                case Conditions.IsExpert:
                case Conditions.LegacyHack_IsBossAndExpert:
                    _difficultyMatcher = mode => mode is NpcDifficultyMode.Expert or NpcDifficultyMode.Master;
                    _showAsQualifier = false;
                    break;

                case Conditions.NotMasterMode:
                    _difficultyMatcher = mode => mode is NpcDifficultyMode.Classic or NpcDifficultyMode.Expert;
                    _showAsQualifier = false;
                    break;

                case Conditions.IsMasterMode:
                    _difficultyMatcher = mode => mode == NpcDifficultyMode.Master;
                    _showAsQualifier = false;
                    break;

                case Conditions.IsCrimsonAndNotExpert:
                case Conditions.IsCorruptionAndNotExpert:
                    _difficultyMatcher = mode => mode == NpcDifficultyMode.Classic;
                    _showAsQualifier = true;
                    break;

                default:
                    _difficultyMatcher = _ => true;
                    _showAsQualifier = true;
                    break;
            }
        }

        internal NpcLootCondition(
            Func<NpcDifficultyMode, bool> difficultyMatcher,
            string description = null,
            bool showAsQualifier = true)
        {
            _difficultyMatcher = difficultyMatcher ?? throw new ArgumentNullException(nameof(difficultyMatcher));
            _descriptionProvider = () => description;
            _showAsQualifier = showAsQualifier;
        }

        public bool AppliesToDifficulty(NpcDifficultyMode difficulty)
        {
            ValidateDifficulty(difficulty);
            return _difficultyMatcher(difficulty);
        }

        public bool ShouldShowAsQualifier(NpcDifficultyMode difficulty)
        {
            ValidateDifficulty(difficulty);
            return _showAsQualifier && AppliesToDifficulty(difficulty);
        }

        public string GetDescription()
        {
            return _descriptionProvider();
        }

        private static void ValidateDifficulty(NpcDifficultyMode difficulty)
        {
            if (difficulty is NpcDifficultyMode.Classic or NpcDifficultyMode.Expert or NpcDifficultyMode.Master)
                return;

            throw new ArgumentOutOfRangeException(nameof(difficulty), difficulty, "Unsupported NPC difficulty mode.");
        }
    }
}