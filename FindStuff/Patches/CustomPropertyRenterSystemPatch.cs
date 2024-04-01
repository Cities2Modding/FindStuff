using Game.Simulation;
using HarmonyLib;
using Unity.Entities;
using Game.Buildings;
using Game.Common;
using Game.Tools;
using FindStuff.Prefabs;

namespace FindStuff.Patches
{
    /// <summary>
    /// This patch only runs if LandValueOverhaul is detected
    /// </summary>
    public static class CustomPropertyRenterSystemPatch
    {
        private static bool hasInitialize = false;

        public static void Prefix()
        {
            ComponentSystemBase __instance = World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged(Mod.customPropertyRenterSystemType);
            if (__instance == null || hasInitialize)
                return;

            Traverse.Create(__instance).Field("m_BuildingGroup").SetValue(__instance.EntityManager.CreateEntityQuery(new EntityQueryDesc[]
            {
                new() {
                    All =
                    [
                        ComponentType.ReadOnly<Building>(),
                        ComponentType.ReadOnly<Renter>(),
                        ComponentType.ReadOnly<UpdateFrame>()
                    ],
                    Any = [ComponentType.ReadWrite<BuildingCondition>()],
                    None =
                    [
                        ComponentType.ReadOnly<Historical>(),
                        ComponentType.ReadOnly<Deleted>(),
                        ComponentType.ReadOnly<Temp>()
                    ]
                }
            }));

            hasInitialize = true;
        }
    }
}
