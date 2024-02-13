using FindStuff.Systems;
using Game;
using Game.Common;
using HarmonyLib;

namespace FindStuff.Patches
{
    [HarmonyPatch(typeof(SystemOrder))]
    internal class SystemOrderPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(SystemOrder), nameof(SystemOrder.Initialize))]
        public static void GetSystemOrder(UpdateSystem updateSystem)
        {
            updateSystem?.UpdateAt<PloppableRICOSystem>(SystemUpdatePhase.GameSimulation);
        }
    }
}
