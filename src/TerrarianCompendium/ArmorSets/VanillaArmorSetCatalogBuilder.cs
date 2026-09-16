using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria.DataStructures;
using TerrarianCompendium.Catalog;

namespace TerrarianCompendium.ArmorSets
{
    internal sealed class VanillaArmorSetCatalogBuilder
    {
        public ArmorSetCatalog Build(ItemCatalog itemCatalog)
        {
            if (itemCatalog == null)
                throw new ArgumentNullException(nameof(itemCatalog));

            if (ArmorSetBonuses.All == null || ArmorSetBonuses.All.Count == 0)
                throw new InvalidOperationException("Vanilla armor-set registry is not initialized or is empty.");

            var sourceEntries = new List<ArmorSetCatalogSourceEntry>(ArmorSetBonuses.All.Count);

            foreach (ArmorSetBonus set in ArmorSetBonuses.All)
            {
                if (set == null)
                    throw new InvalidOperationException("Vanilla armor-set registry contains a null entry.");

                ValidateItemId(itemCatalog, set.Head, "head");
                ValidateItemId(itemCatalog, set.Body, "body");
                ValidateItemId(itemCatalog, set.Legs, "legs");

                string bonusTextKey = set.Description?.Key;

                if (string.IsNullOrWhiteSpace(bonusTextKey))
                    throw new InvalidOperationException("Vanilla armor-set entry has no localization key.");

                string bonusIdentity = BuildBonusIdentity(set, bonusTextKey);
                sourceEntries.Add(
                    new ArmorSetCatalogSourceEntry(
                        bonusIdentity,
                        bonusTextKey,
                        ConvertPrimaryPart(set.PrimaryPart),
                        new ArmorSetVariant(set.Head, set.Body, set.Legs)));
            }

            return new ArmorSetCatalogBuilder().Build(sourceEntries);
        }

        private static string BuildBonusIdentity(ArmorSetBonus set, string bonusTextKey)
        {
            Delegate effect = set.Effect;

            if (effect == null)
                throw new InvalidOperationException("Vanilla armor-set entry has no effect delegate.");

            MethodInfo method = effect.Method;
            string declaringType = method.DeclaringType?.FullName ?? string.Empty;
            string targetIdentity = effect.Target?.GetType().FullName ?? string.Empty;

            return string.Concat(
                declaringType,
                "::",
                method.Name,
                "|",
                targetIdentity,
                "|",
                bonusTextKey,
                "|",
                ((int)set.PrimaryPart).ToString());
        }

        private static ArmorSetPrimaryPart ConvertPrimaryPart(ArmorSetBonus.PartType part)
        {
            return part switch
            {
                ArmorSetBonus.PartType.None => ArmorSetPrimaryPart.None,
                ArmorSetBonus.PartType.Head => ArmorSetPrimaryPart.Head,
                ArmorSetBonus.PartType.Body => ArmorSetPrimaryPart.Body,
                ArmorSetBonus.PartType.Legs => ArmorSetPrimaryPart.Legs,
                _ => throw new InvalidOperationException($"Unsupported vanilla armor-set primary part {part}.")
            };
        }

        private static void ValidateItemId(ItemCatalog catalog, int itemId, string partName)
        {
            if (itemId == 0)
                return;

            if (itemId < 0 || !catalog.Contains(itemId))
            {
                throw new InvalidOperationException(
                    $"Vanilla armor-set {partName} item ID {itemId} is not present in the item catalog.");
            }
        }
    }
}