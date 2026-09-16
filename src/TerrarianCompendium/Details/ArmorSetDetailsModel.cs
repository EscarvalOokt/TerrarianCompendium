using System;
using System.Collections.Generic;
using TerrarianCompendium.ArmorSets;
using TerrarianCompendium.Catalog;
using TerrarianCompendium.Checklist;

namespace TerrarianCompendium.Details
{
    internal sealed class ArmorSetDetailsModel(
        ArmorSetCatalog armorSetCatalog,
        ItemCatalog itemCatalog,
        ItemTextIndex itemTextIndex,
        ChecklistState checklistState,
        Func<string, string> bonusTextResolver)
    {
        private readonly ArmorSetCatalog _armorSetCatalog =
            armorSetCatalog ?? throw new ArgumentNullException(nameof(armorSetCatalog));

        private readonly Func<string, string> _bonusTextResolver =
            bonusTextResolver ?? throw new ArgumentNullException(nameof(bonusTextResolver));

        private readonly ChecklistState _checklistState =
            checklistState ?? throw new ArgumentNullException(nameof(checklistState));

        private readonly ItemCatalog _itemCatalog = itemCatalog ?? throw new ArgumentNullException(nameof(itemCatalog));

        private readonly ItemTextIndex _itemTextIndex =
            itemTextIndex ?? throw new ArgumentNullException(nameof(itemTextIndex));

        private int _cachedArmorSetId;
        private long _cachedChecklistRevision = -1;
        private long _cachedItemTextRevision = -1;
        private ArmorSetDetailsProjection _cachedProjection;

        public bool TryGetProjection(int armorSetId, out ArmorSetDetailsProjection projection)
        {
            if (!_armorSetCatalog.TryGet(armorSetId, out ArmorSetCatalogEntry entry))
            {
                projection = null;
                return false;
            }

            long checklistRevision = _checklistState.Revision;
            long itemTextRevision = _itemTextIndex.Revision;

            if (_cachedProjection != null &&
                _cachedArmorSetId == armorSetId &&
                _cachedChecklistRevision == checklistRevision &&
                _cachedItemTextRevision == itemTextRevision)
            {
                projection = _cachedProjection;
                return true;
            }

            var variants = new List<ArmorSetDetailsVariant>(entry.Variants.Count);

            foreach (ArmorSetVariant variant in entry.Variants)
            {
                variants.Add(
                    new ArmorSetDetailsVariant(
                        BuildItemReference(variant.HeadItemId),
                        BuildItemReference(variant.BodyItemId),
                        BuildItemReference(variant.LegItemId)));
            }

            projection = new ArmorSetDetailsProjection(
                entry.Id,
                entry.RepresentativeItemId,
                _bonusTextResolver(entry.BonusTextKey) ?? string.Empty,
                variants);

            _cachedArmorSetId = armorSetId;
            _cachedChecklistRevision = checklistRevision;
            _cachedItemTextRevision = itemTextRevision;
            _cachedProjection = projection;
            return true;
        }

        private ArmorSetDetailsItemReference BuildItemReference(int itemId)
        {
            if (itemId == 0)
                return null;

            if (!_itemCatalog.Contains(itemId))
                throw new InvalidOperationException($"Armor set references catalog-missing item ID {itemId}.");

            return new ArmorSetDetailsItemReference(
                itemId,
                _itemTextIndex.GetName(itemId),
                _checklistState.IsFound(itemId));
        }
    }
}