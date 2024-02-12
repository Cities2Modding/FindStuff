
using Colossal.Serialization.Entities;
using FindStuff.Prefabs;
using Game;
using Game.Buildings;
using Game.Common;
using Game.Prefabs;
using Game.Tools;
using System.Runtime.CompilerServices;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;

namespace FindStuff.Systems
{
    public class PloppableRICOSystem : GameSystemBase
    {
        private ModificationBarrier5 _barrier5;
        private EndFrameBarrier _endFrameBarrier;
        EntityQuery _freshlyPlacedBuildingsGroup;
        EntityQuery _ploppedBuildingsGroup;
        MakeSignatureTypeHandle _makeSignatureTypeHandle;
        MakePloppableTypeHandle _makePloppableTypeHandle;

        protected override void OnCreate()
        {
            base.OnCreate();

            _barrier5 = World.GetOrCreateSystemManaged<ModificationBarrier5>();
            _endFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();
            _freshlyPlacedBuildingsGroup = GetEntityQuery(new EntityQueryDesc
            {
                All =
                [
                    ComponentType.ReadOnly<Building>(),
                    ComponentType.ReadOnly<PrefabRef>(),
                    ComponentType.ReadOnly<BuildingCondition>(),
                    ComponentType.ReadOnly<Renter>(),
                ],
                Any = [
                    ComponentType.ReadOnly<CommercialProperty>(),
                    ComponentType.ReadOnly<IndustrialProperty>(),
                    ComponentType.ReadOnly<OfficeProperty>(),
                    ComponentType.ReadOnly<ResidentialProperty>(),
                ],
                None =
                [
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

            _barrier5.RequireForUpdate(_freshlyPlacedBuildingsGroup);
            _endFrameBarrier.RequireForUpdate(_ploppedBuildingsGroup);
        }

        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            if (mode.IsGame())
            {
                // Updates the PrefabRef component of the entities with PloppableBuildingData component
                // Only needs to be done once at startup
                if (!_ploppedBuildingsGroup.IsEmptyIgnoreFilter)
                {
                    _makeSignatureTypeHandle.AssignHandles(ref CheckedStateRef);
                    MakeSignatureJob makeSignatureJob = new()
                    {
                        Ecb = _endFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
                        EntityHandle = _makeSignatureTypeHandle.EntityTypeHandle,
                        PrefabRefLookup = _makeSignatureTypeHandle.PrefabRefLookup,
                        SignatureBuildingDataLookup = _makeSignatureTypeHandle.SignatureBuildingDataLookup,
                        PloppableBuildingLookup = _makeSignatureTypeHandle.PloppableBuildingLookup,
                        SpawnableBuildingDataLookup = _makeSignatureTypeHandle.SpawnableBuildingDataLookup,
                    };
                    Dependency = makeSignatureJob.Schedule(_ploppedBuildingsGroup, Dependency);
                    _endFrameBarrier.AddJobHandleForProducer(Dependency);
                }
            }
        }

        protected override void OnUpdate()
        {
            if (!_freshlyPlacedBuildingsGroup.IsEmptyIgnoreFilter)
            {
                _makePloppableTypeHandle.AssignHandles(ref CheckedStateRef);
                MakePloppableJob makePloppableJob = new()
                {
                    Ecb = _barrier5.CreateCommandBuffer().AsParallelWriter(),
                    EntityHandle = _makePloppableTypeHandle.EntityTypeHandle,
                    PrefabRefHandle = _makePloppableTypeHandle.PrefabRefTypeHandle,
                    PloppableBuildingLookup = _makePloppableTypeHandle.PloppableBuildingLookup,
                };
                Dependency = makePloppableJob.Schedule(_freshlyPlacedBuildingsGroup, Dependency);
                _barrier5.AddJobHandleForProducer(Dependency);
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
            }

            [ReadOnly]
            public EntityTypeHandle EntityTypeHandle;

            public ComponentTypeHandle<PrefabRef> PrefabRefTypeHandle;
            public ComponentLookup<PloppableBuilding> PloppableBuildingLookup;
        }

        public struct MakePloppableJob : IJobChunk
        {
            public EntityCommandBuffer.ParallelWriter Ecb;
            public EntityTypeHandle EntityHandle;
            public ComponentTypeHandle<PrefabRef> PrefabRefHandle;
            public ComponentLookup<PloppableBuilding> PloppableBuildingLookup;

            public void Execute(in ArchetypeChunk chunk,
            int unfilteredChunkIndex,
                bool useEnabledMask,
                in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entities = chunk.GetNativeArray(EntityHandle);
                NativeArray<PrefabRef> prefabs = chunk.GetNativeArray(ref PrefabRefHandle);
                ChunkEntityEnumerator enumerator = new(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out int i))
                {
                    Entity entity = entities[i];
                    PrefabRef prefab = prefabs[i];
                    if (PloppableBuildingLookup.HasComponent(prefab.m_Prefab))
                    {
                        Ecb.AddComponent<PloppableBuildingData>(i, entity);
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
                PrefabRefLookup = state.GetComponentLookup<PrefabRef>();
                SignatureBuildingDataLookup = state.GetComponentLookup<SignatureBuildingData>();
                PloppableBuildingLookup = state.GetComponentLookup<PloppableBuilding>();
                SpawnableBuildingDataLookup = state.GetComponentLookup<SpawnableBuildingData>();
            }

            [ReadOnly]
            public EntityTypeHandle EntityTypeHandle;

            public ComponentLookup<PrefabRef> PrefabRefLookup;
            public ComponentLookup<SignatureBuildingData> SignatureBuildingDataLookup;
            public ComponentLookup<PloppableBuilding> PloppableBuildingLookup;
            public ComponentLookup<SpawnableBuildingData> SpawnableBuildingDataLookup;
        }

        public struct MakeSignatureJob : IJobChunk
        {
            public EntityCommandBuffer.ParallelWriter Ecb;
            public EntityTypeHandle EntityHandle;
            public ComponentLookup<PrefabRef> PrefabRefLookup;
            public ComponentLookup<SignatureBuildingData> SignatureBuildingDataLookup;
            public ComponentLookup<PloppableBuilding> PloppableBuildingLookup;
            public ComponentLookup<SpawnableBuildingData> SpawnableBuildingDataLookup;

            public void Execute(in ArchetypeChunk chunk,
            int unfilteredChunkIndex,
                bool useEnabledMask,
                in v128 chunkEnabledMask)
            {
                NativeArray<Entity> entities = chunk.GetNativeArray(EntityHandle);
                ChunkEntityEnumerator enumerator = new(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out int i))
                {
                    Entity entity = entities[i];
                    Entity prefab = PrefabRefLookup[entity].m_Prefab;

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
    }
}
