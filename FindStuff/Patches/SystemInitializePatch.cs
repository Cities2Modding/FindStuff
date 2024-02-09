using FindStuff.Systems;
using Game;
using Game.Common;
using HarmonyLib;

namespace FindStuff.Patches
{
    [HarmonyPatch(typeof(SystemOrder))]
    public static class SystemOrder_Patches
    {
        [HarmonyPatch(nameof(SystemOrder.Initialize))]
        [HarmonyPostfix]
        public static void Postfix(UpdateSystem updateSystem)
        {
            updateSystem?.UpdateAt<PickerToolSystem>(SystemUpdatePhase.UIUpdate);
        }
    }
}
