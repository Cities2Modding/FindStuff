using Game.Buildings;
using HarmonyLib;
using Unity.Entities;

namespace FindStuff.Patches
{
    [HarmonyPatch(typeof(ZoneCheckSystem), "OnCreate")]
    public static class ZoneCheckSystem_Patches
    {
        public static void Postfix()
        {
            ZoneCheckSystem zoneCheckSystem = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<ZoneCheckSystem>();
            zoneCheckSystem.Enabled = false;
        }
    }
}
