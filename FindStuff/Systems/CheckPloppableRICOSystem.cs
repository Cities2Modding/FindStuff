using Colossal.Serialization.Entities;
using FindStuff.Prefabs;
using Game;
using Game.Common;
using Game.Tools;
using Unity.Entities;
using Unity.Jobs;
using Game.Prefabs;
using System.Runtime.CompilerServices;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Game.Buildings;
using Game.Notifications;

namespace FindStuff.Systems
{
    public class CheckPloppableRICOSystem : GameSystemBase
    {
        EndFrameBarrier _barrier;
        IconCommandSystem _iconCommandSystem;
        EntityQuery _ploppedBuildingsGroup;
        EntityQuery _buildingSettingsQuery;
        MakeSignatureTypeHandle _makeSignatureTypeHandle;

        protected override void OnCreate()
        {
            base.OnCreate();

            _barrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();
            _iconCommandSystem = World.GetOrCreateSystemManaged<IconCommandSystem>();
            _ploppedBuildingsGroup = GetEntityQuery(new EntityQueryDesc
            {
                All = [
                    ComponentType.ReadOnly<PloppableBuildingData>(),
                ],
                None = [
                    ComponentType.Exclude<Deleted>(),
                    ComponentType.Exclude<Temp>(),
                ],
            });
            _buildingSettingsQuery = GetEntityQuery(new ComponentType[] { ComponentType.ReadOnly<BuildingConfigurationData>() });
        }

        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);

            if (mode == GameMode.Game && !_ploppedBuildingsGroup.IsEmptyIgnoreFilter)
            {
                int amount = _ploppedBuildingsGroup.CalculateEntityCount();
                UnityEngine.Debug.Log($"FindStuff: Found {amount} entities to update...");
                _makeSignatureTypeHandle.AssignHandles(ref CheckedStateRef);
                MakeSignatureJob makeSignatureJob = new()
                {
                    Ecb = _barrier.CreateCommandBuffer().AsParallelWriter(),
                    Icb = _iconCommandSystem.CreateCommandBuffer(),
                    EntityTypeHandle = GetEntityTypeHandle(),
                    PrefabRefTypeHandle = _makeSignatureTypeHandle.PrefabRefTypeHandle,
                    PloppableBuildingLookup = _makeSignatureTypeHandle.PloppableBuildingLookup,
                    CondemnedLookup = _makeSignatureTypeHandle.CondemnedLookup,
                    BuildingConfigurationData = _buildingSettingsQuery.GetSingleton<BuildingConfigurationData>(),
                };
                JobHandle makeSignatureJobHandle = makeSignatureJob.ScheduleParallel(_ploppedBuildingsGroup, Dependency);
                _barrier.AddJobHandleForProducer(makeSignatureJobHandle);
                Dependency = makeSignatureJobHandle;
            }
        }

        protected override void OnUpdate()
        {
            // Do nothing here. Just needs to be running after game loading is complete. See above.
        }

        public struct MakeSignatureTypeHandle
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AssignHandles(ref SystemState state)
            {
                PrefabRefTypeHandle = state.GetComponentTypeHandle<PrefabRef>();
                PloppableBuildingLookup = state.GetComponentLookup<PloppableBuilding>();
                BuildingConditionLookup = state.GetComponentLookup<BuildingCondition>();
                CondemnedLookup = state.GetComponentLookup<Condemned>();
            }

            public ComponentTypeHandle<PrefabRef> PrefabRefTypeHandle;
            public ComponentLookup<PloppableBuilding> PloppableBuildingLookup;
            public ComponentLookup<BuildingCondition> BuildingConditionLookup;
            public ComponentLookup<Condemned> CondemnedLookup;
        }

        public struct MakeSignatureJob : IJobChunk
        {
            public EntityCommandBuffer.ParallelWriter Ecb;
            public IconCommandBuffer Icb;
            public EntityTypeHandle EntityTypeHandle;
            public ComponentTypeHandle<PrefabRef> PrefabRefTypeHandle;
            public ComponentLookup<PloppableBuilding> PloppableBuildingLookup;
            public ComponentLookup<Condemned> CondemnedLookup;
            public BuildingConfigurationData BuildingConfigurationData;

            public void Execute(in ArchetypeChunk chunk,
            int unfilteredChunkIndex,
                bool useEnabledMask,
                in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entities = chunk.GetNativeArray(EntityTypeHandle);
                NativeArray<PrefabRef> prefabs = chunk.GetNativeArray(ref PrefabRefTypeHandle);
                ChunkEntityEnumerator enumerator = new(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out int i))
                {
                    Entity entity = entities[i];
                    Entity prefab = prefabs[i].m_Prefab;

                    if (!PloppableBuildingLookup.HasComponent(prefab))
                    {
                        // Add ploppable building component
                        Ecb.AddComponent<PloppableBuilding>(i, prefab);

                        if (CondemnedLookup.HasComponent(entity))
                        {
                            Icb.Remove(entity, BuildingConfigurationData.m_CondemnedNotification, default, 0);
                            Ecb.RemoveComponent<Condemned>(i, entity);
                        }
                    }
                }

                prefabs.Dispose();
            }
        }
    }
}
