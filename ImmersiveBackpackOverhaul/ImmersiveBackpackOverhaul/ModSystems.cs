using System;
using HarmonyLib;
using Vintagestory.API.Common;


namespace ImmersiveBackpackOverhaul
{
    public class BagRestrictionSystem : ModSystem
    {

        private Harmony? harmony;

        public override void Start(ICoreAPI api)
        {
            harmony = new Harmony("immersivebackpackoverhaul.bagsizegate");
            harmony.PatchAll();
        }

        public override void Dispose()
        {
            harmony?.UnpatchAll(harmony.Id);
            harmony = null;
        }

        [HarmonyPatch(typeof(ItemSlot), nameof(ItemSlot.CanHold))]
        public static class Patch_ItemSlot_CanHold
        {

            public static bool Prefix(ItemSlot __instance, ItemSlot sourceSlot, ref bool __result)
            {
                if (!TryGetEquipIndex(__instance, out int equipIndex)) return true;

                if (!IsAllowedInEquipSlot(equipIndex, sourceSlot?.Itemstack))
                {
                    __result = false;
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(ItemSlot), nameof(ItemSlot.CanTakeFrom))]
        public static class Patch_ItemSlot_CanTakeFrom
        {
            public static bool Prefix(ItemSlot __instance, ItemSlot sourceSlot, ref bool __result)
            {
                if (!TryGetEquipIndex(__instance, out int equipIndex)) return true;

                if (!IsAllowedInEquipSlot(equipIndex, sourceSlot?.Itemstack))
                {
                    __result = false;
                    return false;
                }

                return true;
            }
        }

        private readonly struct SlotRule
        {
            private readonly string[] sizes;
            private readonly bool deny;

            private SlotRule(string[] sizes, bool deny)
            {
                this.sizes = sizes;
                this.deny = deny;
            }

            public static SlotRule AllowOnly(params string[] sizes) => new SlotRule(sizes, false);
            public static SlotRule AllowExcept(params string[] sizes) => new SlotRule(sizes, true);

            public bool Accepts(string? size)
            {
                bool inSet = size != null && Array.IndexOf(sizes, size) >= 0;
                return deny ? !inSet : inSet;
            }
        }

        private static readonly SlotRule[] SlotRules =
        {
            SlotRule.AllowExcept("bagsmall"),
            SlotRule.AllowOnly("bagmedium"),
            SlotRule.AllowOnly("bagsmall"),
            SlotRule.AllowOnly("bagsmall"),
        };

        private const string BagSizeAttr = "iboBagSize";

        private static string? GetBagSize(ItemStack? stack)
        {
            var attrs = stack?.Collectible?.Attributes;
            if (attrs == null) return null;
            if (!attrs.KeyExists(BagSizeAttr)) return null;

            var s = attrs[BagSizeAttr].AsString("");
            return string.IsNullOrEmpty(s) ? null : s;
        }

        private static bool TryGetEquipIndex(ItemSlot slot, out int equipIndex)
        {
            equipIndex = -1;

            if (slot is not ItemSlotBackpack) return false;
            if (slot.Inventory is not InventoryBase inv) return false;

            int seen = 0;
            for (int i = 0; i < inv.Count; i++)
            {
                var s = inv[i];
                if (s is ItemSlotBackpack)
                {
                    if (ReferenceEquals(s, slot)) { equipIndex = seen; return true; }
                    seen++;
                    if (seen >= 4) break;
                }
            }

            return false;
        }

        private static bool IsAllowedInEquipSlot(int equipIndex, ItemStack? incomingStack)
        {
            if (equipIndex < 0 || equipIndex >= SlotRules.Length) return true;
            return SlotRules[equipIndex].Accepts(GetBagSize(incomingStack));
        }

    }
}