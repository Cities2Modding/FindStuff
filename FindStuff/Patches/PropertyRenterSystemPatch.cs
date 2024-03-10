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
        public static void Postfix(PropertyRenterSystem __instance)
        {
            if (__instance == null)
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
        }
    }
}
