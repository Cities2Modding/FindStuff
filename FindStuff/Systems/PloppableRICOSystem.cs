using Colossal.Entities;
using Colossal.Serialization.Entities;
using FindStuff.Prefabs;
using FindStuff.UI;
using Game;
using Game.Buildings;
using Game.Common;
using Game.Notifications;
using Game.Objects;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using System.Runtime.CompilerServices;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace FindStuff.Systems
{
    public class PloppableRICOSystem : GameSystemBase
    {
        ModificationBarrier5 _barrier;
        IconCommandSystem _iconCommandSystem;
        SimulationSystem _simulationSystem;
        FindStuffController _controller;
        EntityQuery _freshlyPlacedBuildingsGroup;
        EntityQuery _buildingSettingsQuery;
        EntityQuery _ploppableBuildlingsGroup;
        MakePloppableTypeHandle _makePloppableTypeHandle;
        StopLevelingUpDownTypeHandle _stopLevelingUpDownTypeHandle;

        public static readonly int kComponentVersion = 1;

        public static readonly int kUpdatesPerDay = 16;

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            return 262144 / (PropertyRenterSystem.kUpdatesPerDay * 16);
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            _barrier = World.GetOrCreateSystemManaged<ModificationBarrier5>();
            _iconCommandSystem = World.GetOrCreateSystemManaged<IconCommandSystem>();
            _simulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();
            _controller = World.GetOrCreateSystemManaged<FindStuffController>();

            _freshlyPlacedBuildingsGroup = GetEntityQuery(new EntityQueryDesc
            {
                All =
                [
                    ComponentType.ReadOnly<Building>(),
                    ComponentType.ReadOnly<PrefabRef>(),
                    ComponentType.ReadOnly<PropertyToBeOnMarket>(),
                    ComponentType.ReadOnly<BuildingCondition>(),
                ],
                Any = [
                    ComponentType.ReadOnly<ResidentialProperty>(),
                    ComponentType.ReadOnly<CommercialProperty>(),
                    ComponentType.ReadOnly<IndustrialProperty>(),
                    ComponentType.ReadOnly<OfficeProperty>(),
                ],
                None =
                [
                    ComponentType.Exclude<PropertyOnMarket>(),
                    ComponentType.Exclude<UnderConstruction>(),
                    ComponentType.Exclude<PloppableBuildingData>(),
                    ComponentType.Exclude<Deleted>(),
                    ComponentType.Exclude<Temp>(),
                ],
            });

            _ploppableBuildlingsGroup = GetEntityQuery(new EntityQueryDesc
            {
                All =
                [
                    ComponentType.ReadOnly<PloppableBuildingData>(),
                    ComponentType.ReadOnly<UpdateFrame>(),
                ],
                None =
                [
                    ComponentType.Exclude<Deleted>(),
                    ComponentType.Exclude<Temp>(),
                ],
            });

            _buildingSettingsQuery = GetEntityQuery(new ComponentType[] { ComponentType.ReadOnly<BuildingConfigurationData>() });
            RequireAnyForUpdate(_freshlyPlacedBuildingsGroup, _ploppableBuildlingsGroup);
        }

        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
        }

        protected override void OnUpdate()
        {
            uint updateFrame = SimulationUtils.GetUpdateFrame(_simulationSystem.frameIndex, kUpdatesPerDay, 16);

            if (!_freshlyPlacedBuildingsGroup.IsEmptyIgnoreFilter && !_buildingSettingsQuery.IsEmptyIgnoreFilter)
            {
                _makePloppableTypeHandle.AssignHandles(ref CheckedStateRef);
                MakePloppableJob makePloppableJob = new()
                {
                    Ecb = _barrier.CreateCommandBuffer().AsParallelWriter(),
                    Icb = _iconCommandSystem.CreateCommandBuffer(),
                    BuildingConfigurationData = _buildingSettingsQuery.GetSingleton<BuildingConfigurationData>(),
                    EntityHandle = _makePloppableTypeHandle.EntityTypeHandle,
                    PrefabRefTypeHandle = _makePloppableTypeHandle.PrefabRefTypeHandle,
                    PloppableBuildingLookup = _makePloppableTypeHandle.PloppableBuildingLookup,
                    AllowLeveling = !_controller.IsHistorical,
                    CondemnedLookup = _makePloppableTypeHandle.CondemnedLookup,
                };
                
                JobHandle makePloppableJobHandle = makePloppableJob.ScheduleParallel(_freshlyPlacedBuildingsGroup, Dependency);
                _barrier.AddJobHandleForProducer(makePloppableJobHandle);
                Dependency = makePloppableJobHandle;
            }

            if (!_ploppableBuildlingsGroup.IsEmptyIgnoreFilter)
            {
                _stopLevelingUpDownTypeHandle.AssignHandles(ref CheckedStateRef);
                StopLevelingUpDownJob stopLevelingUpDownJob = new()
                {
                    Ecb = _barrier.CreateCommandBuffer().AsParallelWriter(),
                    EntityHandle = _stopLevelingUpDownTypeHandle.EntityTypeHandle,
                    PloppableBuildingDataTypeHandle = _stopLevelingUpDownTypeHandle.PloppableBuildingDataTypeHandle,
                    UpdateFrameType = GetSharedComponentTypeHandle<UpdateFrame>(),
                    UpdateFrameIndex = updateFrame,
                    BuildingConditionLookup = _stopLevelingUpDownTypeHandle.BuildingConditionLookup,
                };

                JobHandle stopLevelingUpDownHandle = stopLevelingUpDownJob.ScheduleParallel(_ploppableBuildlingsGroup, Dependency);
                _barrier.AddJobHandleForProducer(stopLevelingUpDownHandle);
                Dependency = stopLevelingUpDownHandle;
            }
        }

        public struct MakePloppableTypeHandle
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AssignHandles(ref SystemState state)
            {
                EntityTypeHandle = state.GetEntityTypeHandle();
                PrefabRefTypeHandle = state.GetComponentTypeHandle<PrefabRef>();
                PloppableBuildingLookup = state.GetComponentLookup<PloppableBuilding>();
                BuildingConditionLookup = state.GetComponentLookup<BuildingCondition>();
                CondemnedLookup = state.GetComponentLookup<Condemned>();
            }

            public EntityTypeHandle EntityTypeHandle;
            public ComponentTypeHandle<PrefabRef> PrefabRefTypeHandle;
            public ComponentLookup<PloppableBuilding> PloppableBuildingLookup;
            public ComponentLookup<BuildingCondition> BuildingConditionLookup;
            public ComponentLookup<Condemned> CondemnedLookup;
        }

        public struct MakePloppableJob : IJobChunk
        {
            public EntityCommandBuffer.ParallelWriter Ecb;
            public IconCommandBuffer Icb;
            public BuildingConfigurationData BuildingConfigurationData;
            public EntityTypeHandle EntityHandle;
            public ComponentTypeHandle<PrefabRef> PrefabRefTypeHandle;
            public ComponentLookup<PloppableBuilding> PloppableBuildingLookup;
            public ComponentLookup<Condemned> CondemnedLookup;
            public bool AllowLeveling;

            public void Execute(in ArchetypeChunk chunk,
            int unfilteredChunkIndex,
                bool useEnabledMask,
                in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entities = chunk.GetNativeArray(EntityHandle);
                NativeArray<PrefabRef> prefabs = chunk.GetNativeArray(ref PrefabRefTypeHandle);
                ChunkEntityEnumerator enumerator = new(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out int i))
                {
                    Entity entity = entities[i];
                    Entity prefab = prefabs[i].m_Prefab;
                    if (PloppableBuildingLookup.HasComponent(prefab) && entity != Entity.Null)
                    {
                        PloppableBuildingData ploppableBuildingData = new()
                        {
                            version = kComponentVersion,
                            allowLeveling = AllowLeveling,
                        };
                        Ecb.AddComponent(i, entity, ploppableBuildingData);

                        if (CondemnedLookup.HasComponent(entity))
                        {
                            Icb.Remove(entity, BuildingConfigurationData.m_CondemnedNotification, default, 0);
                            Ecb.RemoveComponent<Condemned>(i, entity);
                        }
                    }
                }

                prefabs.Dispose();
                entities.Dispose();
            }
        }

        public struct StopLevelingUpDownTypeHandle
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AssignHandles(ref SystemState state)
            {
                EntityTypeHandle = state.GetEntityTypeHandle();
                PloppableBuildingDataTypeHandle = state.GetComponentTypeHandle<PloppableBuildingData>();
                BuildingConditionLookup = state.GetComponentLookup<BuildingCondition>();
            }

            public EntityTypeHandle EntityTypeHandle;
            public ComponentTypeHandle<PloppableBuildingData> PloppableBuildingDataTypeHandle;
            public ComponentLookup<BuildingCondition> BuildingConditionLookup;
        }

        public struct StopLevelingUpDownJob : IJobChunk
        {
            public EntityCommandBuffer.ParallelWriter Ecb;
            public EntityTypeHandle EntityHandle;
            public ComponentTypeHandle<PloppableBuildingData> PloppableBuildingDataTypeHandle;
            public ComponentLookup<BuildingCondition> BuildingConditionLookup;

            [ReadOnly]
            public SharedComponentTypeHandle<UpdateFrame> UpdateFrameType;

            public uint UpdateFrameIndex;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                if (chunk.GetSharedComponent(UpdateFrameType).m_Index != UpdateFrameIndex)
                {
                    return;
                }

                NativeArray<Entity> entities = chunk.GetNativeArray(EntityHandle);
                NativeArray<PloppableBuildingData> ploppableBuildings = chunk.GetNativeArray(ref PloppableBuildingDataTypeHandle);
                ChunkEntityEnumerator enumerator = new(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out int i))
                {
                    Entity entity = entities[i];
                    PloppableBuildingData ploppableBuildingData = ploppableBuildings[i];
                    if (ploppableBuildingData.allowLeveling && BuildingConditionLookup.TryGetComponent(entity, out BuildingCondition buildingCondition))
                    {
                        // Reset the building condition to 100 so buildings do not level up or down (historical)
                        buildingCondition.m_Condition = 100;
                        Ecb.SetComponent(i, entity, buildingCondition);
                    }
                }

                entities.Dispose();
            }
        }

        public bool IsPloppable(PrefabBase prefabBase, Entity prefabEntity, Entity originalEntity)
        {
            if (originalEntity == Entity.Null || prefabEntity == Entity.Null)
                return false;

            if (EntityManager.HasComponent<Signature>(originalEntity))
                return false;

            // Check if the prefab is a building, has a zone prefab and is not based on a signature building
            if (prefabBase is not BuildingPrefab)
                return false;

            return EntityManager.TryGetComponent(prefabEntity, out SpawnableBuildingData spawnable) && spawnable.m_ZonePrefab != Entity.Null;
        }

        public void MakePloppable(Entity entity)
        {
            EntityCommandBuffer buffer = _barrier.CreateCommandBuffer();
            buffer.AddComponent(entity, new PloppableBuilding());
        }

        public void MakePloppable(Entity entity, EntityCommandBuffer commandBuffer)
        {
            commandBuffer.AddComponent(entity, new PloppableBuilding());
        }
    }
}
