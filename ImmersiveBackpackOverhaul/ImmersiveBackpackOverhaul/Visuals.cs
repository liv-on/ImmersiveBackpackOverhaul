using HarmonyLib;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;


namespace ImmersiveBackpackOverhaul
{
    public class BagDisplaySystem : ModSystem
    {
        private Harmony? harmony;

        public override void Start(ICoreAPI api)
        {
            harmony = new Harmony("immersivebackpackoverhaul.bagdisplay");
            harmony.PatchAll();
        }

        public override void StartClientSide(ICoreClientAPI capi)
        {
            capi.Event.PlayerJoin += player =>
            {
                if (player.PlayerUID == capi.World.Player.PlayerUID)
                {
                    ApplyBackpackIconsToPlayer(player);
                }
            };
        }

        public override void Dispose()
        {
            harmony?.UnpatchAll(harmony.Id);
            harmony = null;
        }

        private static readonly string[] IconByEquipIndex =
        {
            "belt",
            "belt",
            "basket",
            "cape",
        };

        private static void ApplyBackpackIconsToPlayer(IPlayer player)
        {
            if (player?.InventoryManager == null) return;

            var inv = player.InventoryManager.GetOwnInventory(GlobalConstants.backpackInvClassName);
            if (inv == null) return;

            int seen = 0;
            for (int i = 0; i < inv.Count; i++)
            {
                if (inv[i] is ItemSlotBackpack bpSlot)
                {
                    if (seen < IconByEquipIndex.Length)
                    {
                        bpSlot.BackgroundIcon = IconByEquipIndex[seen];
                        inv.MarkSlotDirty(i);
                    }
                    seen++;
                    if (seen >= IconByEquipIndex.Length) break;
                }
            }
        }

        private const string TooltipMarker = "\u200B";
        private const string BagSizeAttr = "iboBagSize";

        [HarmonyPatch(typeof(CollectibleObject), nameof(CollectibleObject.GetHeldItemInfo))]
        public static class Patch_Collectible_GetHeldItemInfo
        {
            public static void Postfix(CollectibleObject __instance, ItemSlot inSlot, StringBuilder dsc)
            {
                if (inSlot?.Itemstack == null || dsc == null) return;

                if (__instance.GetCollectibleInterface<IHeldBag>() == null) return;

                if (ContainsMarker(dsc)) return;

                string line = LineForSize(GetBagSize(inSlot.Itemstack));
                if (line.Length == 0) return;

                dsc.AppendLine(TooltipMarker + line);
            }

            private static bool ContainsMarker(StringBuilder sb)
            {
                for (int i = 0; i < sb.Length; i++)
                {
                    if (sb[i] == '\u200B') return true;
                }
                return false;
            }
        }

        private static string LineForSize(string? size)
        {
            switch (size)
            {
                case "bagsmall":
                    return "Can be worn on all slots.";
                case "bagmedium":
                    return "Can be worn on slot 3 and 4.";
                default:
                    return "Can be worn on the back.";
            }
        }

        private static string? GetBagSize(ItemStack? stack)
        {
            var attrs = stack?.Collectible?.Attributes;
            if (attrs == null) return null;
            if (!attrs.KeyExists(BagSizeAttr)) return null;

            var s = attrs[BagSizeAttr].AsString("");
            return string.IsNullOrEmpty(s) ? null : s;
        }
    }
}