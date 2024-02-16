using HarmonyLib;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using Game.Prefabs;
using Colossal.Entities;
using FindStuff.Prefabs;

namespace FindStuff.Patches
{
    [HarmonyPatch(typeof(BulldozeToolSystem))]
    public static class BulldozeToolSystem_Patches
    {
        [HarmonyPatch("ConfirmationNeeded")]
        [HarmonyPrefix]
        public static bool Prefix(BulldozeToolSystem __instance, ref bool __result)
        {
            EntityQuery buildingQuery = Traverse.Create(__instance).Field<EntityQuery>("m_BuildingQuery").Value;
            EntityManager entityManager = __instance.EntityManager;

            NativeArray<Entity> nativeArray = buildingQuery.ToEntityArray(Allocator.TempJob);
            bool flag = false;
            for (int i = 0; i < nativeArray.Length; i++)
            {
                Entity entity = nativeArray[i];
                PrefabRef prefabRef;
                if ((entityManager.GetComponentData<Temp>(entity).m_Flags & TempFlags.Delete) != (TempFlags)0U && 
                    entityManager.TryGetComponent(entity, out prefabRef) &&
                    (!entityManager.HasComponent<SpawnableBuildingData>(prefabRef.m_Prefab) || (!entityManager.HasComponent<PloppableBuilding>(prefabRef.m_Prefab) &&
                    entityManager.HasComponent<SignatureBuildingData>(prefabRef.m_Prefab))))
                {
                    flag = true;
                }
            }

            nativeArray.Dispose();
            __result = flag;

            return false;
        }
    }
}
