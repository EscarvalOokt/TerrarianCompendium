using System;
using System.Reflection;
using HarmonyLib;
using Terraria;
using Terraria.UI;
using TerrariaModder.Core.Logging;
using TerrariaModder.Core.UI.Widgets;
using TerrarianCompendium.Catalog;

namespace TerrarianCompendium.Navigation
{
    internal sealed class InventoryItemNavigationHandler(
        ItemCatalog itemCatalog,
        Func<bool> isEnabled,
        Action<int> openItem,
        ILogger logger)
    {
        private const string HarmonyId = "terrarian-compendium.navigation.inventory-items";

        private static InventoryItemNavigationHandler _activeHandler;

        private readonly Func<bool> _isEnabled = isEnabled ?? throw new ArgumentNullException(nameof(isEnabled));
        private readonly ItemCatalog _itemCatalog = itemCatalog ?? throw new ArgumentNullException(nameof(itemCatalog));
        private readonly ILogger _logger = logger;
        private readonly Action<int> _openItem = openItem ?? throw new ArgumentNullException(nameof(openItem));
        private bool _consumeRightClickUntilRelease;
        private Harmony _harmony;
        private MethodInfo _itemSlotHandleMethod;

        public void Start()
        {
            if (_harmony != null)
                return;

            MethodInfo itemSlotHandleMethod = AccessTools.Method(
                typeof(ItemSlot),
                nameof(ItemSlot.Handle),
                [
                    typeof(Item[]),
                    typeof(int),
                    typeof(int),
                    typeof(bool)
                ]);

            if (itemSlotHandleMethod == null)
                throw new MissingMethodException(typeof(ItemSlot).FullName, nameof(ItemSlot.Handle));

            var prefix = new HarmonyMethod(typeof(InventoryItemNavigationHandler), nameof(ItemSlotHandlePrefix))
            {
                priority = Priority.Low
            };
            var harmony = new Harmony(HarmonyId);
            harmony.Patch(itemSlotHandleMethod, prefix: prefix);

            _itemSlotHandleMethod = itemSlotHandleMethod;
            _harmony = harmony;
            _activeHandler = this;
        }

        public void Stop()
        {
            if (_harmony == null)
                return;

            try
            {
                if (_itemSlotHandleMethod != null)
                    _harmony.Unpatch(_itemSlotHandleMethod, HarmonyPatchType.All, HarmonyId);
            }
            finally
            {
                if (ReferenceEquals(_activeHandler, this))
                    _activeHandler = null;

                _consumeRightClickUntilRelease = false;
                _itemSlotHandleMethod = null;
                _harmony = null;
            }
        }

        private static bool ItemSlotHandlePrefix(Item[] inv, int context, int slot, bool allowInteract)
        {
            InventoryItemNavigationHandler handler = _activeHandler;
            if (handler == null)
                return true;

            try
            {
                return handler.ShouldRunOriginal(inv, context, slot, allowInteract);
            }
            catch (Exception exception)
            {
                handler._logger?.Error(
                    $"[{TerrarianCompendiumMod.ModName}] Inventory Item navigation failed while handling ItemSlot.",
                    exception);
                return true;
            }
        }

        private bool ShouldRunOriginal(Item[] inv, int context, int slot, bool allowInteract)
        {
            bool rightClick = WidgetInput.MouseRightClick;
            bool rightHeld = WidgetInput.MouseRight;

            if (rightClick || !rightHeld)
                _consumeRightClickUntilRelease = false;

            if (!IsSupportedVanillaItemSlot(inv, context, slot, allowInteract))
                return true;

            if (_consumeRightClickUntilRelease)
                return false;

            if (!rightClick ||
                !_isEnabled() ||
                !WidgetInput.IsAltHeld ||
                WidgetInput.IsCtrlHeld ||
                WidgetInput.IsShiftHeld ||
                Main.LocalPlayerHasPendingInventoryActions())
            {
                return true;
            }

            Item item = inv[slot];
            if (item == null || item.IsAir || !_itemCatalog.Contains(item.type))
                return true;

            _openItem(item.type);
            _consumeRightClickUntilRelease = true;
            return false;
        }

        private static bool IsSupportedVanillaItemSlot(Item[] inv, int context, int slot, bool allowInteract)
        {
            if (!allowInteract || !Main.playerInventory || inv == null || slot < 0 || slot >= inv.Length)
            {
                return false;
            }

            Player player = Main.LocalPlayer;
            if (player == null)
                return false;

            switch (context)
            {
                case ItemSlot.Context.InventoryItem:
                case ItemSlot.Context.InventoryCoin:
                case ItemSlot.Context.InventoryAmmo:
                    return ReferenceEquals(inv, player.inventory);

                case ItemSlot.Context.ChestItem:
                    return player.chest >= 0 && IsCurrentContainerArray(player, inv);

                case ItemSlot.Context.BankItem:
                    return (player.chest == -2 || player.chest == -3 || player.chest == -4) &&
                           IsCurrentContainerArray(player, inv);

                case ItemSlot.Context.VoidItem:
                    return player.chest == -5 && IsCurrentContainerArray(player, inv);

                case ItemSlot.Context.EquipArmor:
                case ItemSlot.Context.EquipArmorVanity:
                case ItemSlot.Context.EquipAccessory:
                case ItemSlot.Context.EquipAccessoryVanity:
                    return ReferenceEquals(inv, player.armor);

                case ItemSlot.Context.EquipDye:
                    return ReferenceEquals(inv, player.dye);

                case ItemSlot.Context.EquipGrapple:
                case ItemSlot.Context.EquipMount:
                case ItemSlot.Context.EquipMinecart:
                case ItemSlot.Context.EquipPet:
                case ItemSlot.Context.EquipLight:
                    return ReferenceEquals(inv, player.miscEquips);

                case ItemSlot.Context.EquipMiscDye:
                    return ReferenceEquals(inv, player.miscDyes);

                case ItemSlot.Context.ShopItem:
                    return IsCurrentShopArray(inv);

                default:
                    return false;
            }
        }

        private static bool IsCurrentContainerArray(Player player, Item[] inv)
        {
            Chest currentContainer = player.GetCurrentContainer();
            return currentContainer?.item != null && ReferenceEquals(inv, currentContainer.item);
        }

        private static bool IsCurrentShopArray(Item[] inv)
        {
            Main main = Main.instance;
            if (main?.shop == null || Main.npcShop <= 0 || Main.npcShop >= main.shop.Length)
                return false;

            Chest shop = main.shop[Main.npcShop];
            return shop?.item != null && ReferenceEquals(inv, shop.item);
        }
    }
}