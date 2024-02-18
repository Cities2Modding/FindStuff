using Colossal.Entities;
using Colossal.Serialization.Entities;
using FindStuff.Prefabs;
using Game;
using Game.Buildings;
using Game.Common;
using Game.Notifications;
using Game.Objects;
using Game.Prefabs;
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
        ModificationBarrier2 _barrier;
        IconCommandSystem _iconCommandSystem;
        EntityQuery _freshlyPlacedBuildingsGroup;
        EntityQuery _buildingSettingsQuery;
        MakePloppableTypeHandle _makePloppableTypeHandle;

        protected override void OnCreate()
        {
            base.OnCreate();

            _barrier = World.GetOrCreateSystemManaged<ModificationBarrier2>();
            _iconCommandSystem = World.GetOrCreateSystemManaged<IconCommandSystem>();
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

            _buildingSettingsQuery = GetEntityQuery(new ComponentType[] { ComponentType.ReadOnly<BuildingConfigurationData>() });
            RequireForUpdate(_freshlyPlacedBuildingsGroup);
        }

        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
        }

        protected override void OnUpdate()
        {

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
                    CondemnedLookup = _makePloppableTypeHandle.CondemnedLookup,
                };
                
                JobHandle makePloppableJobHandle = makePloppableJob.ScheduleParallel(_freshlyPlacedBuildingsGroup, Dependency);
                _barrier.AddJobHandleForProducer(makePloppableJobHandle);
                Dependency = makePloppableJobHandle;
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
                CondemnedLookup = state.GetComponentLookup<Condemned>();
            }

            public EntityTypeHandle EntityTypeHandle;
            public ComponentTypeHandle<PrefabRef> PrefabRefTypeHandle;
            public ComponentLookup<PloppableBuilding> PloppableBuildingLookup;
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
                        Ecb.AddComponent<PloppableBuildingData>(i, entity);

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
            if (EntityManager.TryGetComponent(entity, out SpawnableBuildingData spawnableBuildingData)) {
                EntityCommandBuffer buffer = _barrier.CreateCommandBuffer();
                buffer.AddComponent(entity, new PloppableBuilding());

                // Add signature building data to the zone prefab to be ignored by the ZoneCheckSystem making them condemned
                buffer.AddComponent(entity, new SignatureBuildingData());

                // Set to level 5 to stop the buildings being part of certain simulation systems (signature buildings use the same technique)
                spawnableBuildingData.m_Level = 5;
                buffer.SetComponent(entity, spawnableBuildingData);
            }
        }

        public void MakePloppable(Entity entity, EntityCommandBuffer commandBuffer)
        {
            if (EntityManager.TryGetComponent(entity, out SpawnableBuildingData spawnableBuildingData)) {
                commandBuffer.AddComponent(entity, new PloppableBuilding());

                // Add signature building data to the zone prefab to be ignored by the ZoneCheckSystem making them condemned
                commandBuffer.AddComponent(entity, new SignatureBuildingData());

                // Set to level 5 to stop the buildings being part of certain simulation systems (signature buildings use the same technique)
                spawnableBuildingData.m_Level = 5;
                commandBuffer.SetComponent(entity, spawnableBuildingData);
            }
        }
    }
}
