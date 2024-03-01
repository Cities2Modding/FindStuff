using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using FindStuff.Prefabs;
using Game.Common;
using Game.Notifications;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Game.Zones;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Buildings
{
    public class CustomZoneCheckSystem : GameSystemBase
    {
        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();
            m_ZoneUpdateCollectSystem = World.GetOrCreateSystemManaged<Zones.UpdateCollectSystem>();
            m_ZoneSearchSystem = World.GetOrCreateSystemManaged<Zones.SearchSystem>();
            m_ModificationEndBarrier = World.GetOrCreateSystemManaged<ModificationEndBarrier>();
            m_ObjectSearchSystem = World.GetOrCreateSystemManaged<Objects.SearchSystem>();
            m_ToolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
            m_IconCommandSystem = World.GetOrCreateSystemManaged<IconCommandSystem>();
            m_BuildingSettingsQuery = GetEntityQuery(new ComponentType[] { ComponentType.ReadOnly<BuildingConfigurationData>() });
        }

        [Preserve]
        protected override void OnUpdate()
        {
            if (!m_ZoneUpdateCollectSystem.isUpdated)
            {
                return;
            }
            if (m_BuildingSettingsQuery.IsEmptyIgnoreFilter)
            {
                return;
            }
            NativeQueue<Entity> nativeQueue = new(Allocator.TempJob);
            NativeList<Entity> nativeList = new(Allocator.TempJob);
            NativeList<Bounds2> updatedBounds = m_ZoneUpdateCollectSystem.GetUpdatedBounds(true, out JobHandle jobHandle);
            __TypeHandle.__Game_Prefabs_SignatureBuildingData_RO_ComponentLookup.Update(ref CheckedStateRef);
            __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup.Update(ref CheckedStateRef);
            __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup.Update(ref CheckedStateRef);
            __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup.Update(ref CheckedStateRef);
            FindSpawnableBuildingsJob findSpawnableBuildingsJob = default;
            findSpawnableBuildingsJob.m_Bounds = updatedBounds.AsDeferredJobArray();
            JobHandle jobHandle2;
            findSpawnableBuildingsJob.m_SearchTree = m_ObjectSearchSystem.GetStaticSearchTree(true, out jobHandle2);
            findSpawnableBuildingsJob.m_BuildingData = __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup;
            findSpawnableBuildingsJob.m_PrefabRefData = __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup;
            findSpawnableBuildingsJob.m_PrefabSpawnableBuildingData = __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;
            findSpawnableBuildingsJob.m_PrefabSignatureBuildingData = __TypeHandle.__Game_Prefabs_SignatureBuildingData_RO_ComponentLookup;
            findSpawnableBuildingsJob.m_PloppableBuildingData = __TypeHandle.__Game_Prefabs_PloppableBuildingData_RO_ComponentLookup;
            findSpawnableBuildingsJob.m_ResultQueue = nativeQueue.AsParallelWriter();
            FindSpawnableBuildingsJob findSpawnableBuildingsJob2 = findSpawnableBuildingsJob;
            CollectEntitiesJob collectEntitiesJob = default;
            collectEntitiesJob.m_Queue = nativeQueue;
            collectEntitiesJob.m_List = nativeList;
            CollectEntitiesJob collectEntitiesJob2 = collectEntitiesJob;
            __TypeHandle.__Game_Zones_Cell_RO_BufferLookup.Update(ref CheckedStateRef);
            __TypeHandle.__Game_Prefabs_ZoneData_RO_ComponentLookup.Update(ref CheckedStateRef);
            __TypeHandle.__Game_Prefabs_PlaceholderBuildingData_RO_ComponentLookup.Update(ref CheckedStateRef);
            __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup.Update(ref CheckedStateRef);
            __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup.Update(ref CheckedStateRef);
            __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup.Update(ref CheckedStateRef);
            __TypeHandle.__Game_Objects_Attached_RO_ComponentLookup.Update(ref CheckedStateRef);
            __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup.Update(ref CheckedStateRef);
            __TypeHandle.__Game_Buildings_Abandoned_RO_ComponentLookup.Update(ref CheckedStateRef);
            __TypeHandle.__Game_Common_Destroyed_RO_ComponentLookup.Update(ref CheckedStateRef);
            __TypeHandle.__Game_Zones_ValidArea_RO_ComponentLookup.Update(ref CheckedStateRef);
            __TypeHandle.__Game_Zones_Block_RO_ComponentLookup.Update(ref CheckedStateRef);
            __TypeHandle.__Game_Buildings_Condemned_RO_ComponentLookup.Update(ref CheckedStateRef);
            CheckBuildingZonesJob checkBuildingZonesJob = default;
            checkBuildingZonesJob.m_CondemnedData = __TypeHandle.__Game_Buildings_Condemned_RO_ComponentLookup;
            checkBuildingZonesJob.m_BlockData = __TypeHandle.__Game_Zones_Block_RO_ComponentLookup;
            checkBuildingZonesJob.m_ValidAreaData = __TypeHandle.__Game_Zones_ValidArea_RO_ComponentLookup;
            checkBuildingZonesJob.m_DestroyedData = __TypeHandle.__Game_Common_Destroyed_RO_ComponentLookup;
            checkBuildingZonesJob.m_AbandonedData = __TypeHandle.__Game_Buildings_Abandoned_RO_ComponentLookup;
            checkBuildingZonesJob.m_TransformData = __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup;
            checkBuildingZonesJob.m_AttachedData = __TypeHandle.__Game_Objects_Attached_RO_ComponentLookup;
            checkBuildingZonesJob.m_PrefabRefData = __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup;
            checkBuildingZonesJob.m_PrefabBuildingData = __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup;
            checkBuildingZonesJob.m_PrefabSpawnableBuildingData = __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;
            checkBuildingZonesJob.m_PrefabPlaceholderBuildingData = __TypeHandle.__Game_Prefabs_PlaceholderBuildingData_RO_ComponentLookup;
            checkBuildingZonesJob.m_PrefabZoneData = __TypeHandle.__Game_Prefabs_ZoneData_RO_ComponentLookup;
            checkBuildingZonesJob.m_Cells = __TypeHandle.__Game_Zones_Cell_RO_BufferLookup;
            checkBuildingZonesJob.m_BuildingConfigurationData = m_BuildingSettingsQuery.GetSingleton<BuildingConfigurationData>();
            checkBuildingZonesJob.m_Buildings = nativeList.AsDeferredJobArray();
            checkBuildingZonesJob.m_SearchTree = m_ZoneSearchSystem.GetSearchTree(true, out JobHandle jobHandle3);
            checkBuildingZonesJob.m_EditorMode = m_ToolSystem.actionMode.IsEditor();
            checkBuildingZonesJob.m_IconCommandBuffer = m_IconCommandSystem.CreateCommandBuffer();
            checkBuildingZonesJob.m_CommandBuffer = m_ModificationEndBarrier.CreateCommandBuffer().AsParallelWriter();
            CheckBuildingZonesJob checkBuildingZonesJob2 = checkBuildingZonesJob;
            JobHandle jobHandle4 = findSpawnableBuildingsJob2.Schedule(updatedBounds, 1, JobHandle.CombineDependencies(Dependency, jobHandle, jobHandle2));
            JobHandle jobHandle5 = collectEntitiesJob2.Schedule(jobHandle4);
            JobHandle jobHandle6 = checkBuildingZonesJob2.Schedule(nativeList, 1, JobHandle.CombineDependencies(jobHandle5, jobHandle3));
            nativeQueue.Dispose(jobHandle5);
            nativeList.Dispose(jobHandle6);
            m_ZoneUpdateCollectSystem.AddBoundsReader(jobHandle4);
            m_ObjectSearchSystem.AddStaticSearchTreeReader(jobHandle4);
            m_ZoneSearchSystem.AddSearchTreeReader(jobHandle6);
            m_IconCommandSystem.AddCommandBufferWriter(jobHandle6);
            m_ModificationEndBarrier.AddJobHandleForProducer(jobHandle6);
            Dependency = jobHandle6;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void __AssignQueries(ref SystemState state)
        {
        }

        protected override void OnCreateForCompiler()
        {
            base.OnCreateForCompiler();
            __AssignQueries(ref CheckedStateRef);
            __TypeHandle.__AssignHandles(ref CheckedStateRef);
        }

        [Preserve]
        public CustomZoneCheckSystem()
        {
        }

        private Zones.UpdateCollectSystem m_ZoneUpdateCollectSystem;

        private Zones.SearchSystem m_ZoneSearchSystem;

        private ModificationEndBarrier m_ModificationEndBarrier;

        private Objects.SearchSystem m_ObjectSearchSystem;

        private ToolSystem m_ToolSystem;

        private IconCommandSystem m_IconCommandSystem;

        private EntityQuery m_BuildingSettingsQuery;

        private TypeHandle __TypeHandle;

        [BurstCompile]
        private struct FindSpawnableBuildingsJob : IJobParallelForDefer
        {
            public void Execute(int index)
            {
                Iterator iterator = default;
                iterator.m_Bounds = m_Bounds[index];
                iterator.m_ResultQueue = m_ResultQueue;
                iterator.m_BuildingData = m_BuildingData;
                iterator.m_PrefabRefData = m_PrefabRefData;
                iterator.m_PrefabSpawnableBuildingData = m_PrefabSpawnableBuildingData;
                iterator.m_PrefabSignatureBuildingData = m_PrefabSignatureBuildingData;
                iterator.m_PloppableBuildingData = m_PloppableBuildingData;
                Iterator iterator2 = iterator;
                m_SearchTree.Iterate(ref iterator2, 0);
            }

            [ReadOnly]
            public NativeArray<Bounds2> m_Bounds;

            [ReadOnly]
            public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_SearchTree;

            [ReadOnly]
            public ComponentLookup<Building> m_BuildingData;

            [ReadOnly]
            public ComponentLookup<PrefabRef> m_PrefabRefData;

            [ReadOnly]
            public ComponentLookup<SpawnableBuildingData> m_PrefabSpawnableBuildingData;

            [ReadOnly]
            public ComponentLookup<SignatureBuildingData> m_PrefabSignatureBuildingData;

            [ReadOnly]
            public ComponentLookup<PloppableBuildingData> m_PloppableBuildingData;

            public NativeQueue<Entity>.ParallelWriter m_ResultQueue;

            private struct Iterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
            {
                public bool Intersect(QuadTreeBoundsXZ bounds)
                {
                    return MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds);
                }

                public void Iterate(QuadTreeBoundsXZ bounds, Entity objectEntity)
                {
                    if (!MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds))
                    {
                        return;
                    }
                    if (!m_BuildingData.HasComponent(objectEntity))
                    {
                        return;
                    }
                    PrefabRef prefabRef = m_PrefabRefData[objectEntity];
                    // Add check for ploppable building data on the entity (objectEntity)
                    if (m_PrefabSpawnableBuildingData.HasComponent(prefabRef.m_Prefab) && !m_PrefabSignatureBuildingData.HasComponent(prefabRef.m_Prefab) && !m_PloppableBuildingData.HasComponent(objectEntity))
                    {
                        m_ResultQueue.Enqueue(objectEntity);
                    }
                }

                public Bounds2 m_Bounds;

                public NativeQueue<Entity>.ParallelWriter m_ResultQueue;

                public ComponentLookup<Building> m_BuildingData;

                public ComponentLookup<PrefabRef> m_PrefabRefData;

                public ComponentLookup<SpawnableBuildingData> m_PrefabSpawnableBuildingData;

                public ComponentLookup<SignatureBuildingData> m_PrefabSignatureBuildingData;

                public ComponentLookup<PloppableBuildingData> m_PloppableBuildingData;
            }
        }

        [BurstCompile]
        private struct CollectEntitiesJob : IJob
        {
            public void Execute()
            {
                int count = m_Queue.Count;
                if (count == 0)
                {
                    return;
                }
                m_List.ResizeUninitialized(count);
                for (int i = 0; i < count; i++)
                {
                    m_List[i] = m_Queue.Dequeue();
                }
                m_List.Sort(default(EntityComparer));
                Entity entity = Entity.Null;
                int j = 0;
                int num = 0;
                while (j < m_List.Length)
                {
                    Entity entity2 = m_List[j++];
                    if (entity2 != entity)
                    {
                        m_List[num++] = entity2;
                        entity = entity2;
                    }
                }
                if (num < m_List.Length)
                {
                    m_List.RemoveRangeSwapBack(num, m_List.Length - num);
                }
            }

            public NativeQueue<Entity> m_Queue;

            public NativeList<Entity> m_List;

            private struct EntityComparer : IComparer<Entity>
            {
                public int Compare(Entity x, Entity y)
                {
                    return x.Index - y.Index;
                }
            }
        }

        [BurstCompile]
        private struct CheckBuildingZonesJob : IJobParallelForDefer
        {
            public void Execute(int index)
            {
                Entity entity = m_Buildings[index];
                PrefabRef prefabRef = m_PrefabRefData[entity];
                BuildingData buildingData = m_PrefabBuildingData[prefabRef.m_Prefab];
                SpawnableBuildingData spawnableBuildingData = m_PrefabSpawnableBuildingData[prefabRef.m_Prefab];
                bool flag = m_EditorMode;
                if (!flag)
                {
                    flag = ValidateAttachedParent(entity, spawnableBuildingData);
                }
                if (!flag)
                {
                    flag = ValidateZoneBlocks(entity, buildingData, spawnableBuildingData);
                }
                if (flag)
                {
                    if (m_CondemnedData.HasComponent(entity))
                    {
                        m_CommandBuffer.RemoveComponent<Condemned>(index, entity);
                        m_IconCommandBuffer.Remove(entity, m_BuildingConfigurationData.m_CondemnedNotification, default, 0);
                        return;
                    }
                }
                else if (!m_CondemnedData.HasComponent(entity))
                {
                    m_CommandBuffer.AddComponent(index, entity, default(Condemned));
                    if (!m_DestroyedData.HasComponent(entity) && !m_AbandonedData.HasComponent(entity))
                    {
                        m_IconCommandBuffer.Add(entity, m_BuildingConfigurationData.m_CondemnedNotification, IconPriority.FatalProblem, IconClusterLayer.Default, (IconFlags)0, default(Entity), false, false, false, 0f);
                    }
                }
            }

            private bool ValidateAttachedParent(Entity building, SpawnableBuildingData prefabSpawnableBuildingData)
            {
                if (m_AttachedData.HasComponent(building))
                {
                    Attached attached = m_AttachedData[building];
                    if (m_PrefabRefData.HasComponent(attached.m_Parent))
                    {
                        PrefabRef prefabRef = m_PrefabRefData[attached.m_Parent];
                        if (m_PrefabPlaceholderBuildingData.HasComponent(prefabRef.m_Prefab) && m_PrefabPlaceholderBuildingData[prefabRef.m_Prefab].m_ZonePrefab == prefabSpawnableBuildingData.m_ZonePrefab)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }

            private bool ValidateZoneBlocks(Entity building, BuildingData prefabBuildingData, SpawnableBuildingData prefabSpawnableBuildingData)
            {
                Transform transform = m_TransformData[building];
                ZoneData zoneData = default;
                if (m_PrefabZoneData.HasComponent(prefabSpawnableBuildingData.m_ZonePrefab))
                {
                    zoneData = m_PrefabZoneData[prefabSpawnableBuildingData.m_ZonePrefab];
                }
                float2 xz = math.rotate(transform.m_Rotation, new float3(8f, 0f, 0f)).xz;
                float2 xz2 = math.rotate(transform.m_Rotation, new float3(0f, 0f, 8f)).xz;
                float2 @float = xz * (prefabBuildingData.m_LotSize.x * 0.5f - 0.5f);
                float2 float2 = xz2 * (prefabBuildingData.m_LotSize.y * 0.5f - 0.5f);
                float2 float3 = math.abs(float2) + math.abs(@float);
                NativeArray<bool> nativeArray = new(prefabBuildingData.m_LotSize.x * prefabBuildingData.m_LotSize.y, Allocator.Temp, NativeArrayOptions.ClearMemory);
                Iterator iterator = default;
                iterator.m_Bounds = new Bounds2(transform.m_Position.xz - float3, transform.m_Position.xz + float3);
                iterator.m_LotSize = prefabBuildingData.m_LotSize;
                iterator.m_StartPosition = transform.m_Position.xz + float2 + @float;
                iterator.m_Right = xz;
                iterator.m_Forward = xz2;
                iterator.m_ZoneType = zoneData.m_ZoneType;
                iterator.m_Validated = nativeArray;
                iterator.m_BlockData = m_BlockData;
                iterator.m_ValidAreaData = m_ValidAreaData;
                iterator.m_Cells = m_Cells;
                Iterator iterator2 = iterator;
                m_SearchTree.Iterate(ref iterator2, 0);
                bool flag = (iterator2.m_Directions & CellFlags.Roadside) > CellFlags.None;
                for (int i = 0; i < nativeArray.Length; i++)
                {
                    flag &= nativeArray[i];
                }
                nativeArray.Dispose();
                return flag;
            }

            [ReadOnly]
            public ComponentLookup<Condemned> m_CondemnedData;

            [ReadOnly]
            public ComponentLookup<Block> m_BlockData;

            [ReadOnly]
            public ComponentLookup<ValidArea> m_ValidAreaData;

            [ReadOnly]
            public ComponentLookup<Destroyed> m_DestroyedData;

            [ReadOnly]
            public ComponentLookup<Abandoned> m_AbandonedData;

            [ReadOnly]
            public ComponentLookup<Transform> m_TransformData;

            [ReadOnly]
            public ComponentLookup<Attached> m_AttachedData;

            [ReadOnly]
            public ComponentLookup<PrefabRef> m_PrefabRefData;

            [ReadOnly]
            public ComponentLookup<BuildingData> m_PrefabBuildingData;

            [ReadOnly]
            public ComponentLookup<SpawnableBuildingData> m_PrefabSpawnableBuildingData;

            [ReadOnly]
            public ComponentLookup<PlaceholderBuildingData> m_PrefabPlaceholderBuildingData;

            [ReadOnly]
            public ComponentLookup<ZoneData> m_PrefabZoneData;

            [ReadOnly]
            public BufferLookup<Cell> m_Cells;

            [ReadOnly]
            public BuildingConfigurationData m_BuildingConfigurationData;

            [ReadOnly]
            public NativeArray<Entity> m_Buildings;

            [ReadOnly]
            public NativeQuadTree<Entity, Bounds2> m_SearchTree;

            [ReadOnly]
            public bool m_EditorMode;

            public IconCommandBuffer m_IconCommandBuffer;

            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

            private struct Iterator : INativeQuadTreeIterator<Entity, Bounds2>, IUnsafeQuadTreeIterator<Entity, Bounds2>
            {
                public bool Intersect(Bounds2 bounds)
                {
                    return MathUtils.Intersect(bounds, m_Bounds);
                }

                public void Iterate(Bounds2 bounds, Entity blockEntity)
                {
                    if (!MathUtils.Intersect(bounds, m_Bounds))
                    {
                        return;
                    }
                    ValidArea validArea = m_ValidAreaData[blockEntity];
                    if (validArea.m_Area.y <= validArea.m_Area.x)
                    {
                        return;
                    }
                    Block block = new()
                    {
                        m_Direction = m_Forward
                    };
                    Block block2 = m_BlockData[blockEntity];
                    DynamicBuffer<Cell> dynamicBuffer = m_Cells[blockEntity];
                    float2 @float = m_StartPosition;
                    int2 @int;
                    @int.y = 0;
                    while (@int.y < m_LotSize.y)
                    {
                        float2 float2 = @float;
                        @int.x = 0;
                        while (@int.x < m_LotSize.x)
                        {
                            int2 cellIndex = ZoneUtils.GetCellIndex(block2, float2);
                            if (math.all((cellIndex >= validArea.m_Area.xz) & (cellIndex < validArea.m_Area.yw)))
                            {
                                int num = cellIndex.y * block2.m_Size.x + cellIndex.x;
                                Cell cell = dynamicBuffer[num];
                                if ((cell.m_State & CellFlags.Visible) != CellFlags.None && cell.m_Zone.Equals(m_ZoneType))
                                {
                                    m_Validated[@int.y * m_LotSize.x + @int.x] = true;
                                    if ((cell.m_State & (CellFlags.Roadside | CellFlags.RoadLeft | CellFlags.RoadRight | CellFlags.RoadBack)) != CellFlags.None)
                                    {
                                        CellFlags roadDirection = ZoneUtils.GetRoadDirection(block, block2, cell.m_State);
                                        int4 int2 = new(512, 4, 1024, 2048);
                                        int2 = math.select(0, int2, new bool4(@int == 0, @int == m_LotSize - 1));
                                        m_Directions |= roadDirection & (CellFlags)math.csum(int2);
                                    }
                                }
                            }
                            float2 -= m_Right;
                            @int.x++;
                        }
                        @float -= m_Forward;
                        @int.y++;
                    }
                }

                public Bounds2 m_Bounds;

                public int2 m_LotSize;

                public float2 m_StartPosition;

                public float2 m_Right;

                public float2 m_Forward;

                public ZoneType m_ZoneType;

                public CellFlags m_Directions;

                public NativeArray<bool> m_Validated;

                public ComponentLookup<Block> m_BlockData;

                public ComponentLookup<ValidArea> m_ValidAreaData;

                public BufferLookup<Cell> m_Cells;
            }
        }

        private struct TypeHandle
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void __AssignHandles(ref SystemState state)
            {
                __Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(true);
                __Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(true);
                __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup = state.GetComponentLookup<SpawnableBuildingData>(true);
                __Game_Prefabs_SignatureBuildingData_RO_ComponentLookup = state.GetComponentLookup<SignatureBuildingData>(true);
                __Game_Buildings_Condemned_RO_ComponentLookup = state.GetComponentLookup<Condemned>(true);
                __Game_Zones_Block_RO_ComponentLookup = state.GetComponentLookup<Block>(true);
                __Game_Zones_ValidArea_RO_ComponentLookup = state.GetComponentLookup<ValidArea>(true);
                __Game_Common_Destroyed_RO_ComponentLookup = state.GetComponentLookup<Destroyed>(true);
                __Game_Buildings_Abandoned_RO_ComponentLookup = state.GetComponentLookup<Abandoned>(true);
                __Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(true);
                __Game_Objects_Attached_RO_ComponentLookup = state.GetComponentLookup<Attached>(true);
                __Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(true);
                __Game_Prefabs_PlaceholderBuildingData_RO_ComponentLookup = state.GetComponentLookup<PlaceholderBuildingData>(true);
                __Game_Prefabs_PloppableBuildingData_RO_ComponentLookup = state.GetComponentLookup<PloppableBuildingData>(true);
                __Game_Prefabs_ZoneData_RO_ComponentLookup = state.GetComponentLookup<ZoneData>(true);
                __Game_Zones_Cell_RO_BufferLookup = state.GetBufferLookup<Cell>(true);
            }

            [ReadOnly]
            public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<SignatureBuildingData> __Game_Prefabs_SignatureBuildingData_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<Condemned> __Game_Buildings_Condemned_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<Block> __Game_Zones_Block_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<ValidArea> __Game_Zones_ValidArea_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<Destroyed> __Game_Common_Destroyed_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<Abandoned> __Game_Buildings_Abandoned_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<Attached> __Game_Objects_Attached_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<PlaceholderBuildingData> __Game_Prefabs_PlaceholderBuildingData_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<PloppableBuildingData> __Game_Prefabs_PloppableBuildingData_RO_ComponentLookup;

            [ReadOnly]
            public ComponentLookup<ZoneData> __Game_Prefabs_ZoneData_RO_ComponentLookup;

            [ReadOnly]
            public BufferLookup<Cell> __Game_Zones_Cell_RO_BufferLookup;
        }
    }
}
