using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria.GameContent.FishDropRules;

namespace TerrarianCompendium.Acquisition
{
    internal sealed class VanillaFishingSourceIndexBuilder
    {
        private const string RulesFieldName = "_rules";
        private const string IsStopperPropertyName = "IsStopper";

        private static readonly HashSet<string> _sourceReachabilityStopperFieldNames = new(StringComparer.Ordinal)
        {
            "InLava",
            "InHoney",
            "Ocean"
        };

        private static readonly HashSet<string> _controlOnlyStopperFieldNames = new(StringComparer.Ordinal)
        {
            "AnyEnemies",
            "Junk",
            "Crate"
        };

        private static readonly IReadOnlyDictionary<string, FishingSourceConditionKind> _conditionKindsByFieldName =
            new Dictionary<string, FishingSourceConditionKind>(StringComparer.Ordinal)
            {
                ["HardMode"] = FishingSourceConditionKind.HardMode,
                ["EarlyMode"] = FishingSourceConditionKind.EarlyMode,
                ["InLava"] = FishingSourceConditionKind.InLava,
                ["InHoney"] = FishingSourceConditionKind.InHoney,
                ["CanFishInLava"] = FishingSourceConditionKind.CanFishInLava,
                ["Dungeon"] = FishingSourceConditionKind.Dungeon,
                ["Beach"] = FishingSourceConditionKind.Beach,
                ["Hallow"] = FishingSourceConditionKind.Hallow,
                ["GlowingMushrooms"] = FishingSourceConditionKind.GlowingMushrooms,
                ["TrueDesert"] = FishingSourceConditionKind.TrueDesert,
                ["TrueSnow"] = FishingSourceConditionKind.TrueSnow,
                ["Remix"] = FishingSourceConditionKind.Remix,
                ["Height1"] = FishingSourceConditionKind.Height1,
                ["Height1And2"] = FishingSourceConditionKind.Height1And2,
                ["HeightAbove1"] = FishingSourceConditionKind.HeightAbove1,
                ["HeightAboveAnd1"] = FishingSourceConditionKind.HeightAboveAnd1,
                ["HeightUnder2"] = FishingSourceConditionKind.HeightUnder2,
                ["HeightAbove2"] = FishingSourceConditionKind.HeightAbove2,
                ["Height0"] = FishingSourceConditionKind.Height0,
                ["Height2"] = FishingSourceConditionKind.Height2,
                ["Height3"] = FishingSourceConditionKind.Height3,
                ["UnderRockLayer"] = FishingSourceConditionKind.UnderRockLayer,
                ["Corruption"] = FishingSourceConditionKind.Corruption,
                ["Crimson"] = FishingSourceConditionKind.Crimson,
                ["Jungle"] = FishingSourceConditionKind.Jungle,
                ["Snow"] = FishingSourceConditionKind.Snow,
                ["Desert"] = FishingSourceConditionKind.Desert,
                ["RolledHallowDesert"] = FishingSourceConditionKind.HallowDesert,
                ["OriginalOcean"] = FishingSourceConditionKind.OriginalOcean,
                ["RemixOcean"] = FishingSourceConditionKind.RemixOcean,
                ["Ocean"] = FishingSourceConditionKind.Ocean,
                ["BloodMoon"] = FishingSourceConditionKind.BloodMoon,
                ["Junk"] = FishingSourceConditionKind.Junk,
                ["Crate"] = FishingSourceConditionKind.Crate,
                ["Water1000"] = FishingSourceConditionKind.Water1000,
                ["DidNotUseCombatBook"] = FishingSourceConditionKind.DidNotUseCombatBook
            };

        public FishingSourceIndex Build()
        {
            var ruleList = new FishDropRuleList();
            var populator = new GameContentFishDropPopulator(ruleList);
            Dictionary<AFishingCondition, string> knownConditionFieldNames = BuildKnownConditionFieldNameMap(populator);
            Dictionary<AFishingCondition, FishingSourceConditionKind> knownConditions =
                BuildKnownConditionMap(knownConditionFieldNames);

            populator.Populate();
            IReadOnlyList<FishDropRule> rules = GetRules(ruleList);
            PropertyInfo isStopperProperty = GetIsStopperProperty();
            var relations = new List<FishingSourceRelation>();
            IReadOnlyList<FishingSourceVariant> reachabilityAlternatives =
            [
                new FishingSourceVariant(Array.Empty<FishingSourceConditionKind>())
            ];

            foreach (FishDropRule rule in rules)
            {
                if (reachabilityAlternatives.Count == 0)
                    break;
                if (rule == null)
                    continue;

                IReadOnlyList<FishingSourceConditionKind> ruleConditions = BuildConditions(
                    rule.Conditions,
                    knownConditions,
                    requireCompleteMapping: false);
                int[] possibleItems = rule.PossibleItems;

                if (possibleItems == null)
                {
                    throw new InvalidOperationException("Terraria fishing rule has no PossibleItems collection.");
                }

                if (possibleItems.Length > 0)
                {
                    AddRuleRelations(relations, possibleItems, ruleConditions, reachabilityAlternatives);
                    continue;
                }

                if (!GetIsStopper(rule, isStopperProperty))
                {
                    throw new InvalidOperationException(
                        "Terraria fishing rule with no possible items was expected to be a stopper.");
                }

                if (!ShouldApplyStopperToSourceReachability(rule.Conditions, knownConditionFieldNames))
                    continue;

                IReadOnlyList<FishingSourceConditionKind> stopperConditions = BuildConditions(
                    rule.Conditions,
                    knownConditions,
                    requireCompleteMapping: true);
                reachabilityAlternatives = FishingSourceVariantComposer.ApplyStopper(
                    reachabilityAlternatives,
                    stopperConditions);
            }

            return new FishingSourceIndex(relations);
        }

        private static Dictionary<AFishingCondition, string> BuildKnownConditionFieldNameMap(
            GameContentFishDropPopulator populator)
        {
            var result = new Dictionary<AFishingCondition, string>();
            FieldInfo[] fields = typeof(AFishDropRulePopulator).GetFields(
                BindingFlags.Instance | BindingFlags.NonPublic);

            foreach (FieldInfo field in fields)
            {
                if (!typeof(AFishingCondition).IsAssignableFrom(field.FieldType))
                    continue;

                if (field.GetValue(populator) is AFishingCondition condition)
                    result[condition] = field.Name;
            }

            return result;
        }

        private static Dictionary<AFishingCondition, FishingSourceConditionKind> BuildKnownConditionMap(
            IReadOnlyDictionary<AFishingCondition, string> knownConditionFieldNames)
        {
            var result = new Dictionary<AFishingCondition, FishingSourceConditionKind>();

            foreach (KeyValuePair<AFishingCondition, string> pair in knownConditionFieldNames)
            {
                if (_conditionKindsByFieldName.TryGetValue(pair.Value, out FishingSourceConditionKind kind))
                    result[pair.Key] = kind;
            }

            return result;
        }

        private static bool ShouldApplyStopperToSourceReachability(
            AFishingCondition[] conditions,
            IReadOnlyDictionary<AFishingCondition, string> knownConditionFieldNames)
        {
            if (conditions == null || conditions.Length != 1 || conditions[0] == null)
            {
                throw new InvalidOperationException(
                    "Expected Terraria 1.4.5.8 AddStopper rule to contain exactly one fishing condition.");
            }

            if (!knownConditionFieldNames.TryGetValue(conditions[0], out string fieldName))
            {
                throw new InvalidOperationException(
                    $"Terraria fishing stopper condition '{conditions[0].GetType().FullName}' has no known field identity.");
            }

            return ShouldApplyStopperToSourceReachability(fieldName);
        }

        internal static bool ShouldApplyStopperToSourceReachability(string conditionFieldName)
        {
            if (conditionFieldName == null)
                throw new ArgumentNullException(nameof(conditionFieldName));

            if (_sourceReachabilityStopperFieldNames.Contains(conditionFieldName))
                return true;

            if (_controlOnlyStopperFieldNames.Contains(conditionFieldName))
                return false;

            throw new InvalidOperationException(
                $"Terraria fishing stopper condition field '{conditionFieldName}' has no classified source-reachability behavior.");
        }

        private static void AddRuleRelations(
            ICollection<FishingSourceRelation> relations,
            IEnumerable<int> possibleItems,
            IReadOnlyList<FishingSourceConditionKind> ruleConditions,
            IReadOnlyList<FishingSourceVariant> reachabilityAlternatives)
        {
            var seenItemIds = new HashSet<int>();
            var variants = new List<FishingSourceVariant>();

            foreach (FishingSourceVariant reachabilityAlternative in reachabilityAlternatives)
            {
                if (FishingSourceVariantComposer.TryCompose(
                        reachabilityAlternative,
                        ruleConditions,
                        out FishingSourceVariant variant))
                {
                    variants.Add(variant);
                }
            }

            if (variants.Count == 0)
                return;

            foreach (int itemId in possibleItems)
            {
                if (itemId <= 0 || !seenItemIds.Add(itemId))
                    continue;

                for (var variantIndex = 0; variantIndex < variants.Count; variantIndex++)
                    relations.Add(new FishingSourceRelation(itemId, variants[variantIndex]));
            }
        }

        private static IReadOnlyList<FishingSourceConditionKind> BuildConditions(
            AFishingCondition[] conditions,
            IReadOnlyDictionary<AFishingCondition, FishingSourceConditionKind> knownConditions,
            bool requireCompleteMapping)
        {
            var normalized = new List<FishingSourceConditionKind>();

            if (conditions != null)
            {
                foreach (AFishingCondition condition in conditions)
                {
                    if (condition == null)
                        continue;

                    if (knownConditions.TryGetValue(condition, out FishingSourceConditionKind kind))
                    {
                        normalized.Add(kind);
                        continue;
                    }

                    if (condition is FishingConditions.QuestFishCondition)
                    {
                        normalized.Add(FishingSourceConditionKind.AnglerQuest);
                        continue;
                    }

                    if (condition is FishingConditions.QuestFishConditionRemix)
                    {
                        normalized.Add(FishingSourceConditionKind.AnglerQuest);
                        normalized.Add(FishingSourceConditionKind.Remix);
                        continue;
                    }

                    if (requireCompleteMapping)
                    {
                        throw new InvalidOperationException(
                            $"Terraria fishing stopper condition '{condition.GetType().FullName}' has no normalized mapping.");
                    }
                }
            }

            return new FishingSourceVariant(normalized).Conditions;
        }

        private static PropertyInfo GetIsStopperProperty()
        {
            PropertyInfo property = typeof(FishDropRule).GetProperty(
                IsStopperPropertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (property == null || property.PropertyType != typeof(bool) || property.GetIndexParameters().Length != 0)
            {
                throw new InvalidOperationException(
                    $"Expected Terraria 1.4.5.8 fishing rule property '{IsStopperPropertyName}' was not found with the expected type.");
            }

            return property;
        }

        private static bool GetIsStopper(FishDropRule rule, PropertyInfo property)
        {
            if (property.GetValue(rule) is not bool isStopper)
            {
                throw new InvalidOperationException(
                    $"Terraria fishing rule property '{IsStopperPropertyName}' did not contain the expected Boolean value.");
            }

            return isStopper;
        }

        private static IReadOnlyList<FishDropRule> GetRules(FishDropRuleList ruleList)
        {
            FieldInfo rulesField = typeof(FishDropRuleList).GetField(
                RulesFieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);

            if (rulesField == null || rulesField.FieldType != typeof(List<FishDropRule>))
            {
                throw new InvalidOperationException(
                    $"Expected Terraria 1.4.5.8 fishing rule field '{RulesFieldName}' was not found with the expected type.");
            }

            if (rulesField.GetValue(ruleList) is not List<FishDropRule> rules)
            {
                throw new InvalidOperationException(
                    $"Terraria fishing rule field '{RulesFieldName}' did not contain the expected rule list.");
            }

            return rules;
        }
    }
}