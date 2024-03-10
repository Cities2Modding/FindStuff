using Game.Buildings;

namespace FindStuff.Patches
{
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
