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

namespace FindStuff.Systems
{
    public class CheckPloppableRICOSystem : GameSystemBase
    {
        EndFrameBarrier _barrier;
        EntityQuery _ploppedBuildingsGroup;
        MakeSignatureTypeHandle _makeSignatureTypeHandle;

        protected override void OnCreate()
        {
            base.OnCreate();

            _barrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();
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
                    PrefabRefTypeHandle = _makeSignatureTypeHandle.PrefabRefTypeHandle,
                    SignatureBuildingDataLookup = _makeSignatureTypeHandle.SignatureBuildingDataLookup,
                    PloppableBuildingLookup = _makeSignatureTypeHandle.PloppableBuildingLookup,
                    SpawnableBuildingDataLookup = _makeSignatureTypeHandle.SpawnableBuildingDataLookup,
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
                SignatureBuildingDataLookup = state.GetComponentLookup<SignatureBuildingData>();
                PloppableBuildingLookup = state.GetComponentLookup<PloppableBuilding>();
                SpawnableBuildingDataLookup = state.GetComponentLookup<SpawnableBuildingData>();
            }

            public ComponentTypeHandle<PrefabRef> PrefabRefTypeHandle;
            public ComponentLookup<SignatureBuildingData> SignatureBuildingDataLookup;
            public ComponentLookup<PloppableBuilding> PloppableBuildingLookup;
            public ComponentLookup<SpawnableBuildingData> SpawnableBuildingDataLookup;
        }

        public struct MakeSignatureJob : IJobChunk
        {
            public EntityCommandBuffer.ParallelWriter Ecb;
            public ComponentTypeHandle<PrefabRef> PrefabRefTypeHandle;
            public ComponentLookup<SignatureBuildingData> SignatureBuildingDataLookup;
            public ComponentLookup<PloppableBuilding> PloppableBuildingLookup;
            public ComponentLookup<SpawnableBuildingData> SpawnableBuildingDataLookup;

            public void Execute(in ArchetypeChunk chunk,
            int unfilteredChunkIndex,
                bool useEnabledMask,
                in v128 chunkEnabledMask)
            {
                NativeArray<PrefabRef> prefabs = chunk.GetNativeArray(ref PrefabRefTypeHandle);
                ChunkEntityEnumerator enumerator = new(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out int i))
                {
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

                prefabs.Dispose();
            }
        }
    }
}
