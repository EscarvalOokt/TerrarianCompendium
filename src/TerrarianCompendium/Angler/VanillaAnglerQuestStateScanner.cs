using System;
using Terraria;

namespace TerrarianCompendium.Angler
{
    internal sealed class VanillaAnglerQuestStateScanner(AnglerRewardCatalog rewardCatalog, AnglerQuestState questState)
    {
        private readonly AnglerQuestState _questState =
            questState ?? throw new ArgumentNullException(nameof(questState));

        private readonly AnglerRewardCatalog _rewardCatalog =
            rewardCatalog ?? throw new ArgumentNullException(nameof(rewardCatalog));

        public void Update(Player player)
        {
            if (player == null)
                throw new ArgumentNullException(nameof(player));

            int[] questItemIds = Main.anglerQuestItemNetIDs;

            if (questItemIds == null || questItemIds.Length == 0)
                throw new InvalidOperationException("Terraria Angler quest Item array is not initialized.");

            int questIndex = Main.anglerQuest;

            if (questIndex < 0 || questIndex >= questItemIds.Length)
            {
                throw new InvalidOperationException(
                    $"Terraria Angler quest index {questIndex} is outside the quest Item array bounds.");
            }

            int questItemId = questItemIds[questIndex];

            if (questItemId <= 0)
            {
                throw new InvalidOperationException(
                    $"Terraria Angler quest index {questIndex} resolved to invalid Item ID {questItemId}.");
            }

            AnglerRewardOwnership ownedRewardComponents = CollectOwnedRewardComponents(player);

            _questState.ReplaceSnapshot(
                new AnglerQuestSnapshot(
                    questItemId,
                    Main.anglerQuestFinished,
                    player.anglerQuestsFinished,
                    Main.hardMode,
                    Main.expertMode,
                    ownedRewardComponents));
        }

        private AnglerRewardOwnership CollectOwnedRewardComponents(Player player)
        {
            AnglerRewardOwnership result = AnglerRewardOwnership.None;

            ScanItems(player.inventory, ref result);
            ScanItems(player.armor, ref result);
            ScanItems(player.bank?.item, ref result);
            ScanItems(player.bank2?.item, ref result);
            ScanItems(player.bank3?.item, ref result);
            ScanItems(player.bank4?.item, ref result);

            EquipmentLoadout[] loadouts = player.Loadouts;

            if (loadouts == null)
                return result;

            foreach (EquipmentLoadout loadout in loadouts)
            {
                if (loadout == null)
                    continue;

                ScanItems(loadout.Armor, ref result);
            }

            return result;
        }

        private void ScanItems(Item[] items, ref AnglerRewardOwnership result)
        {
            if (items == null)
                return;

            foreach (Item item in items)
            {
                if (item == null || item.IsAir)
                    continue;

                result |= _rewardCatalog.GetOwnedMissingRewardFlagsForItem(item.type);
            }
        }
    }
}