using Game.Buildings;
using HarmonyLib;

namespace FindStuff.Patches
{
    [HarmonyPatch(typeof(ZoneCheckSystem), "OnCreate")]
    public static class ZoneCheckSystem_Patches
    {
        public static void Postfix(ZoneCheckSystem __instance)
        {
            if (__instance == null)
                return;

            __instance.Enabled = false;
        }
    }
}
