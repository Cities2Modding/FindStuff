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
using System.Linq;

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
                UnityEngine.Debug.Log($"FindStuff: Found {amount} buildings to check...");
                _makeSignatureTypeHandle.AssignHandles(ref CheckedStateRef);
                CheckPloppedBuildingsJob checkPloppedBuildingsJob = new()
                {
                    Ecb = _barrier.CreateCommandBuffer().AsParallelWriter(),
                    Icb = _iconCommandSystem.CreateCommandBuffer(),
                    EntityTypeHandle = GetEntityTypeHandle(),
                    PloppableBuildingDataLookup = _makeSignatureTypeHandle.PloppableBuildingDataLookup,
                    HistoricalLookup = _makeSignatureTypeHandle.HistoricalLookup,
                    CondemnedLookup = _makeSignatureTypeHandle.CondemnedLookup,
                    BuildingConfigurationData = _buildingSettingsQuery.GetSingleton<BuildingConfigurationData>(),
                };
                JobHandle makePloppableJobHandle = checkPloppedBuildingsJob.ScheduleParallel(_ploppedBuildingsGroup, Dependency);
                _barrier.AddJobHandleForProducer(makePloppableJobHandle);
                Dependency = makePloppableJobHandle;
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
                PloppableBuildingDataLookup = state.GetComponentLookup<PloppableBuildingData>();
                HistoricalLookup = state.GetComponentLookup<Historical>();
                CondemnedLookup = state.GetComponentLookup<Condemned>();
            }

            public ComponentLookup<PloppableBuildingData> PloppableBuildingDataLookup;
            public ComponentLookup<Historical> HistoricalLookup;
            public ComponentLookup<Condemned> CondemnedLookup;
        }

        /**
         * This jobs checks all plopped buildings for the Historical component and adds it if it's necessary.
         * Also removes the Condemned component if it's present.
         */
        public struct CheckPloppedBuildingsJob : IJobChunk
        {
            public EntityCommandBuffer.ParallelWriter Ecb;
            public IconCommandBuffer Icb;
            public EntityTypeHandle EntityTypeHandle;
            public ComponentLookup<PloppableBuildingData> PloppableBuildingDataLookup;
            public ComponentLookup<Historical> HistoricalLookup;
            public ComponentLookup<Condemned> CondemnedLookup;
            public BuildingConfigurationData BuildingConfigurationData;

            public void Execute(in ArchetypeChunk chunk,
            int unfilteredChunkIndex,
                bool useEnabledMask,
                in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entities = chunk.GetNativeArray(EntityTypeHandle);
                ChunkEntityEnumerator enumerator = new(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out int i))
                {
                    Entity entity = entities[i];
                    PloppableBuildingData ploppableBuildingData = PloppableBuildingDataLookup[entity];

                    // Update to version 1 (adds historical feature)
                    if (ploppableBuildingData.GetType().GetFields().Any(f => f.Name == "version") && ploppableBuildingData.version == 0 && !HistoricalLookup.HasComponent(entity)) {
                        Ecb.AddComponent<Historical>(i, entity);
                        ploppableBuildingData.version = PloppableRICOSystem.kComponentVersion;
                        Ecb.SetComponent(i, entity, ploppableBuildingData);
                    }

                    if (CondemnedLookup.HasComponent(entity))
                    {
                        Icb.Remove(entity, BuildingConfigurationData.m_CondemnedNotification, default, 0);
                        Ecb.RemoveComponent<Condemned>(i, entity);
                    }
                }
            }
        }
    }
}
