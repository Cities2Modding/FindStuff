using Game.Simulation;
using HarmonyLib;
using Unity.Entities;
using Game.Buildings;
using Game.Common;
using Game.Tools;
using FindStuff.Prefabs;

namespace FindStuff.Patches
{
    public static class PropertyRenterSystemPatch
    {
        private static bool hasInitialize = false;

        public static void Prefix(PropertyRenterSystem __instance)
        {
            if (__instance == null || hasInitialize == true)
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

            UnityEngine.Debug.Log("Original PropertyRenterSystems m_BuildingGroup patched.");
            hasInitialize = true;
        }
    }
}
