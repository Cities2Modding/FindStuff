using FindStuff.Systems;
using Game;
using Game.Buildings;
using Game.Common;
using HarmonyLib;

namespace FindStuff.Patches
{
    [HarmonyPatch( typeof( SystemOrder ) )]
    internal class SystemOrderPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch( typeof( SystemOrder ), nameof( SystemOrder.Initialize ) )]
        public static void GetSystemOrder( UpdateSystem updateSystem )
        {
            updateSystem?.UpdateAt<CheckPloppableRICOSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem?.UpdateAt<PloppableRICOSystem>(SystemUpdatePhase.Modification5);
            updateSystem?.UpdateAt<CustomZoneCheckSystem>(SystemUpdatePhase.ModificationEnd);
        }
    }
}
