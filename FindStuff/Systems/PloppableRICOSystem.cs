﻿
using Colossal.Entities;
using Colossal.Serialization.Entities;
using FindStuff.Prefabs;
using Game;
using Game.Buildings;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using System.Runtime.CompilerServices;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.Rendering;

namespace FindStuff.Systems
{
    public class PloppableRICOSystem : GameSystemBase
    {
        private EndFrameBarrier _barrier;
        EntityQuery _freshlyPlacedBuildingsGroup;
        EntityQuery _ploppedBuildingsGroup;
        MakeSignatureTypeHandle _makeSignatureTypeHandle;
        MakePloppableTypeHandle _makePloppableTypeHandle;

        protected override void OnCreate()
        {
            base.OnCreate();

            _barrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();
            _freshlyPlacedBuildingsGroup = GetEntityQuery(new EntityQueryDesc
            {
                All =
                [
                    ComponentType.ReadOnly<Building>(),
                    ComponentType.ReadOnly<PrefabRef>(),
                    ComponentType.ReadOnly<UpdateFrame>(),
                    ComponentType.ReadOnly<PropertyToBeOnMarket>(),
                ],
                None =
                [
                    ComponentType.Exclude<UnderConstruction>(),
                    ComponentType.Exclude<PloppableBuildingData>(),
                    ComponentType.Exclude<Deleted>(),
                    ComponentType.Exclude<Temp>(),
                ],
            });

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

            _barrier.RequireAnyForUpdate(_ploppedBuildingsGroup, _freshlyPlacedBuildingsGroup);
        }

        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);

            if (mode.IsGame() && !_ploppedBuildingsGroup.IsEmptyIgnoreFilter)
            {
                int amount = _ploppedBuildingsGroup.CalculateEntityCount();
                UnityEngine.Debug.Log($"FindStuff: Found {amount} entities to update...");
                _makeSignatureTypeHandle.AssignHandles(ref CheckedStateRef);
                MakeSignatureJob makeSignatureJob = new()
                {
                    Ecb = _barrier.CreateCommandBuffer().AsParallelWriter(),
                    PrefabRefTypeHandle = _makeSignatureTypeHandle.PrefabRefTypeHandle,
                    SignatureBuildingDataLookup = _makeSignatureTypeHandle.SignatureBuildingDataLookup,
                    PloppableBuildingLookup = _makeSignatureTypeHandle.PloppableBuildingLookup,
                    SpawnableBuildingDataLookup = _makeSignatureTypeHandle.SpawnableBuildingDataLookup,
                };
                Dependency = makeSignatureJob.Schedule(_ploppedBuildingsGroup, Dependency);
                _barrier.AddJobHandleForProducer(Dependency);
            }
        }

        protected override void OnUpdate()
        {
            if (!_freshlyPlacedBuildingsGroup.IsEmptyIgnoreFilter)
            {
                _makePloppableTypeHandle.AssignHandles(ref CheckedStateRef);
                MakePloppableJob makePloppableJob = new()
                {
                    Ecb = _barrier.CreateCommandBuffer().AsParallelWriter(),
                    EntityHandle = _makePloppableTypeHandle.EntityTypeHandle,
                    PrefabRefTypeHandle = _makePloppableTypeHandle.PrefabRefTypeHandle,
                    PloppableBuildingLookup = _makePloppableTypeHandle.PloppableBuildingLookup,
                    CondemnedLookup = _makePloppableTypeHandle.CondemnedLookup,
                };
                Dependency = makePloppableJob.Schedule(_freshlyPlacedBuildingsGroup, Dependency);
                _barrier.AddJobHandleForProducer(Dependency);
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
                    if (PloppableBuildingLookup.HasComponent(prefab))
                    {
                        Ecb.AddComponent<PloppableBuildingData>(i, entity);

                        if (CondemnedLookup.HasComponent(entity))
                        {
                            Ecb.RemoveComponent<Condemned>(i, entity);
                        }
                    }
                }
            }
        }

        public struct MakeSignatureTypeHandle
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AssignHandles(ref SystemState state)
            {
                EntityTypeHandle = state.GetEntityTypeHandle();
                PrefabRefTypeHandle = state.GetComponentTypeHandle<PrefabRef>();
                SignatureBuildingDataLookup = state.GetComponentLookup<SignatureBuildingData>();
                PloppableBuildingLookup = state.GetComponentLookup<PloppableBuilding>();
                SpawnableBuildingDataLookup = state.GetComponentLookup<SpawnableBuildingData>();
            }

            public EntityTypeHandle EntityTypeHandle;
            public ComponentTypeHandle<PrefabRef> PrefabRefTypeHandle;
            public ComponentLookup<SignatureBuildingData> SignatureBuildingDataLookup;
            public ComponentLookup<PloppableBuilding> PloppableBuildingLookup;
            public ComponentLookup<SpawnableBuildingData> SpawnableBuildingDataLookup;
        }

        public struct MakeSignatureJob : IJobChunk
        {
            public EntityCommandBuffer.ParallelWriter Ecb;
            public EntityTypeHandle EntityTypeHandle;
            public ComponentTypeHandle<PrefabRef> PrefabRefTypeHandle;
            public ComponentLookup<SignatureBuildingData> SignatureBuildingDataLookup;
            public ComponentLookup<PloppableBuilding> PloppableBuildingLookup;
            public ComponentLookup<SpawnableBuildingData> SpawnableBuildingDataLookup;

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
                    if (!SignatureBuildingDataLookup.HasComponent(prefab) && !PloppableBuildingLookup.HasComponent(prefab))
                    {
                        // Add the signature building data and ploppable building components
                        Ecb.AddComponent<SignatureBuildingData>(i, prefab);
                        Ecb.AddComponent<PloppableBuilding>(i, prefab);

                        // Set the level to 5
                        if (SpawnableBuildingDataLookup.TryGetComponent(prefab, out SpawnableBuildingData spawnableBuildingData))
                        {
                            if (spawnableBuildingData.m_Level < 5)
                            {
                                spawnableBuildingData.m_Level = 5;
                                Ecb.SetComponent(i, prefab, spawnableBuildingData);
                            }
                        }
                    }
                }
            }
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
            if (EntityManager.TryGetComponent(entity, out SpawnableBuildingData spawnableBuildingData) {
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