using Colossal.Mathematics;
using FindStuff.Prefabs;
using Game;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Economy;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using System.Runtime.CompilerServices;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace FindStuff.Systems
{
    public class PloppableRICORentSystem : GameSystemBase
    {
        private PropertyRenterSystem _propertyRenterSystem;
        private CityStatisticsSystem _cityStatisticsSystem;
        private SimulationSystem _simulationSystem;

        private EntityQuery _ploppableBuildlingsGroup;
        private EntityQuery _economyParameterQuery;
        private EntityQuery _householdGroup;
        private EntityQuery _buildingSettingsQuery;

        private TypeHandle RentJobsTypeHandle;

        private NativeQueue<int> _paymentQueue;
        private NativeQueue<Entity> _levelupQueue;
        private NativeQueue<Entity> _leveldownQueue;

        private EndFrameBarrier _barrier;

        public bool _debugFastLeveling = false;
        public static readonly int kUpdatesPerDay = 16;

        public override int GetUpdateInterval(SystemUpdatePhase phase)
        {
            return 262144 / (kUpdatesPerDay * 16);
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            _propertyRenterSystem = World.GetOrCreateSystemManaged<PropertyRenterSystem>();
            _cityStatisticsSystem = World.GetOrCreateSystemManaged<CityStatisticsSystem>();
            _simulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();
            _barrier = World.GetOrCreateSystemManaged<EndFrameBarrier>();

            _ploppableBuildlingsGroup = GetEntityQuery(new EntityQueryDesc
            {
                All = [
                    ComponentType.ReadOnly<Building>(),
                    ComponentType.ReadOnly<Renter>(),
                    ComponentType.ReadOnly<Historical>(),
                    ComponentType.ReadOnly<UpdateFrame>(),
                ],
                Any = [ComponentType.ReadWrite<BuildingCondition>()],
                None = [
                    ComponentType.Exclude<Deleted>(),
                    ComponentType.Exclude<Temp>(),
                ],
            });

            _householdGroup = GetEntityQuery(new ComponentType[]
            {
                ComponentType.ReadOnly<Household>(),
                ComponentType.ReadOnly<PloppableBuildingData>(),
                ComponentType.ReadOnly<UpdateFrame>()
            });

            _paymentQueue = new NativeQueue<int>(Allocator.Persistent);
            _levelupQueue = new NativeQueue<Entity>(Allocator.Persistent);
            _leveldownQueue = new NativeQueue<Entity>(Allocator.Persistent);
            _economyParameterQuery = GetEntityQuery(new ComponentType[] { ComponentType.ReadOnly<EconomyParameterData>() });
            _buildingSettingsQuery = GetEntityQuery(new ComponentType[] { ComponentType.ReadOnly<BuildingConfigurationData>() });

            RequireForUpdate(_buildingSettingsQuery);
            RequireForUpdate(_ploppableBuildlingsGroup);
        }

        protected override void OnUpdate()
        {
            uint updateFrame = SimulationUtils.GetUpdateFrame(_simulationSystem.frameIndex, kUpdatesPerDay, 16);
            uint updateFrame2 = SimulationUtils.GetUpdateFrame(_simulationSystem.frameIndex, kUpdatesPerDay, 16);

            RentJobsTypeHandle.EntityType.Update(ref CheckedStateRef);
            RentJobsTypeHandle.RenterBufferType.Update(ref CheckedStateRef);
            RentJobsTypeHandle.StorageCompanyLookup.Update(ref CheckedStateRef);
            RentJobsTypeHandle.PrefabRefType.Update(ref CheckedStateRef);
            RentJobsTypeHandle.DestroyedLookup.Update(ref CheckedStateRef);
            RentJobsTypeHandle.AbandonedLookup.Update(ref CheckedStateRef);
            RentJobsTypeHandle.PropertyOnMarketLookup.Update(ref CheckedStateRef);
            RentJobsTypeHandle.BuildingPropertyDataLookup.Update(ref CheckedStateRef);
            RentJobsTypeHandle.ResourcesBufferLookup.Update(ref CheckedStateRef);
            RentJobsTypeHandle.PropertyRenterLookup.Update(ref CheckedStateRef);
            RentJobsTypeHandle.SpawnableBuildingDataLookup.Update(ref CheckedStateRef);
            PayRentJob payRentJob = new()
            {
                m_EntityType = RentJobsTypeHandle.EntityType,
                m_RenterType = RentJobsTypeHandle.RenterBufferType,
                m_UpdateFrameType = GetSharedComponentTypeHandle<UpdateFrame>(),
                m_Storages = RentJobsTypeHandle.StorageCompanyLookup,
                m_PrefabType = RentJobsTypeHandle.PrefabRefType,
                m_SpawnableBuildingData = RentJobsTypeHandle.SpawnableBuildingDataLookup,
                m_PropertyRenters = RentJobsTypeHandle.PropertyRenterLookup,
                m_Resources = RentJobsTypeHandle.ResourcesBufferLookup,
                m_BuildingProperties = RentJobsTypeHandle.BuildingPropertyDataLookup,
                m_PropertiesOnMarket = RentJobsTypeHandle.PropertyOnMarketLookup,
                m_Abandoned = RentJobsTypeHandle.AbandonedLookup,
                m_Destroyed = RentJobsTypeHandle.DestroyedLookup,
                m_RandomSeed = RandomSeed.Next(),
                m_LandlordQueue = _paymentQueue.AsParallelWriter(),
                m_UpdateFrameIndex = updateFrame,
                m_CommandBuffer = _barrier.CreateCommandBuffer().AsParallelWriter(),
            };
            JobHandle jobHandle = payRentJob.ScheduleParallel(_ploppableBuildlingsGroup, Dependency);
            _barrier.AddJobHandleForProducer(jobHandle);

            // ReturnRentJob
            RentJobsTypeHandle.CityStatisticsBufferLookup.Update(ref CheckedStateRef);
            RentJobsTypeHandle.CitizenLookup.Update(ref CheckedStateRef);
            RentJobsTypeHandle.ResourcesBufferLookup.Update(ref CheckedStateRef);
            RentJobsTypeHandle.HouseholdCitizenBufferType.Update(ref CheckedStateRef);
            RentJobsTypeHandle.EntityType.Update(ref CheckedStateRef);
            ReturnRentJob returnRentJob = default;
            returnRentJob.m_EntityType = RentJobsTypeHandle.EntityType;
            returnRentJob.m_UpdateFrameType = GetSharedComponentTypeHandle<UpdateFrame>();
            returnRentJob.m_HouseholdCitizenType = RentJobsTypeHandle.HouseholdCitizenBufferType;
            returnRentJob.m_Resources = RentJobsTypeHandle.ResourcesBufferLookup;
            returnRentJob.m_EconomyParameters = _economyParameterQuery.GetSingleton<EconomyParameterData>();
            returnRentJob.m_Citizens = RentJobsTypeHandle.CitizenLookup;
            returnRentJob.m_Statistics = RentJobsTypeHandle.CityStatisticsBufferLookup;
            returnRentJob.m_StatisticsLookup = _cityStatisticsSystem.GetLookup();
            returnRentJob.m_LandlordEntity = _propertyRenterSystem.Landlords;
            returnRentJob.m_UpdateFrameIndex = updateFrame2;
            returnRentJob.m_PaymentQueue = _paymentQueue.AsParallelWriter();
            jobHandle = returnRentJob.ScheduleParallel(_householdGroup, jobHandle);
            _barrier.AddJobHandleForProducer(jobHandle);

            // LandlordMoneyJob
            RentJobsTypeHandle.ResourcesBufferLookup.Update(ref CheckedStateRef);
            LandlordMoneyJob landlordMoneyJob = default;
            landlordMoneyJob.m_Resources = RentJobsTypeHandle.ResourcesBufferLookup;
            landlordMoneyJob.m_LandlordEntity = _propertyRenterSystem.Landlords;
            landlordMoneyJob.m_PaymentQueue = _paymentQueue;
            jobHandle = landlordMoneyJob.Schedule(jobHandle);

            Dependency = jobHandle;
        }

        [Preserve]
        protected override void OnDestroy()
        {
            _paymentQueue.Dispose();
            _levelupQueue.Dispose();
            _leveldownQueue.Dispose();
            base.OnDestroy();
        }

        protected override void OnCreateForCompiler()
        {
            base.OnCreateForCompiler();
            RentJobsTypeHandle.AssignHandles(ref CheckedStateRef);
        }

        private struct PayRentJob : IJobChunk
        {
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                if (chunk.GetSharedComponent(m_UpdateFrameType).m_Index != m_UpdateFrameIndex)
                {
                    return;
                }
                Unity.Mathematics.Random random = m_RandomSeed.GetRandom(1 + unfilteredChunkIndex);
                NativeArray<Entity> entities = chunk.GetNativeArray(m_EntityType);
                NativeArray<PrefabRef> prefabRefs = chunk.GetNativeArray(ref m_PrefabType);
                BufferAccessor<Renter> renterAccessor = chunk.GetBufferAccessor(ref m_RenterType);
                for (int i = 0; i < chunk.Count; i++)
                {
                    DynamicBuffer<Renter> renterBuffer = renterAccessor[i];
                    Entity prefab = prefabRefs[i].m_Prefab;
                    if (m_SpawnableBuildingData.HasComponent(prefab))
                    {
                        BuildingPropertyData buildingPropertyData = m_BuildingProperties[prefab];
                        int num2 = 0;
                        for (int j = 0; j < renterBuffer.Length; j++)
                        {
                            Entity renter = renterBuffer[j].m_Renter;
                            if (m_PropertyRenters.HasComponent(renter))
                            {
                                PropertyRenter propertyRenter = m_PropertyRenters[renter];
                                int num3;
                                if (m_Storages.HasComponent(renter))
                                {
                                    num3 = EconomyUtils.GetResources(Resource.Money, m_Resources[renter]);
                                }
                                else
                                {
                                    num3 = MathUtils.RoundToIntRandom(ref random, propertyRenter.m_Rent * 1f / kUpdatesPerDay);
                                    num2 += num3;
                                }
                                EconomyUtils.AddResources(Resource.Money, -num3, m_Resources[renter]);
                            }
                        }
                        m_LandlordQueue.Enqueue(num2);
                        bool notAbandonedOrDestroyed = !m_Abandoned.HasComponent(entities[i]) && !m_Destroyed.HasComponent(entities[i]);
                        for (int k = renterBuffer.Length - 1; k >= 0; k--)
                        {
                            Entity renter2 = renterBuffer[k].m_Renter;
                            if (!m_PropertyRenters.HasComponent(renter2))
                            {
                                renterBuffer.RemoveAt(k);
                            }
                        }
                        if (renterBuffer.Length < buildingPropertyData.CountProperties() && !m_PropertiesOnMarket.HasComponent(entities[i]) && notAbandonedOrDestroyed)
                        {
                            m_CommandBuffer.AddComponent(unfilteredChunkIndex, entities[i], default(PropertyToBeOnMarket));
                        }
                        int propertiesCount = buildingPropertyData.CountProperties();
                        while ((renterBuffer.Length > 0 && !notAbandonedOrDestroyed) || renterBuffer.Length > propertiesCount)
                        {
                            Entity renter3 = renterBuffer[renterBuffer.Length - 1].m_Renter;
                            if (m_PropertyRenters.HasComponent(renter3))
                            {
                                m_CommandBuffer.RemoveComponent<PropertyRenter>(unfilteredChunkIndex, renter3);
                            }
                            renterBuffer.RemoveAt(renterBuffer.Length - 1);
                        }
                    }
                }
            }

            void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                Execute(chunk, unfilteredChunkIndex, useEnabledMask, chunkEnabledMask);
            }

            [ReadOnly]
            public EntityTypeHandle m_EntityType;

            public BufferTypeHandle<Renter> m_RenterType;

            [ReadOnly]
            public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

            [ReadOnly]
            public ComponentTypeHandle<PrefabRef> m_PrefabType;

            [ReadOnly]
            public ComponentLookup<SpawnableBuildingData> m_SpawnableBuildingData;

            [ReadOnly]
            public ComponentLookup<PropertyRenter> m_PropertyRenters;

            [NativeDisableParallelForRestriction]
            public BufferLookup<Game.Economy.Resources> m_Resources;

            [ReadOnly]
            public ComponentLookup<BuildingPropertyData> m_BuildingProperties;

            [ReadOnly]
            public ComponentLookup<PropertyOnMarket> m_PropertiesOnMarket;

            [ReadOnly]
            public ComponentLookup<Abandoned> m_Abandoned;

            [ReadOnly]
            public ComponentLookup<Destroyed> m_Destroyed;

            [ReadOnly]
            public ComponentLookup<Game.Companies.StorageCompany> m_Storages;

            public RandomSeed m_RandomSeed;

            public NativeQueue<int>.ParallelWriter m_LandlordQueue;

            public uint m_UpdateFrameIndex;

            public EntityCommandBuffer.ParallelWriter m_CommandBuffer;
        }

        private struct LandlordMoneyJob : IJob
        {
            public void Execute()
            {
                DynamicBuffer<Game.Economy.Resources> dynamicBuffer = m_Resources[m_LandlordEntity];
                while (m_PaymentQueue.TryDequeue(out int num))
                {
                    EconomyUtils.AddResources(Resource.Money, num, dynamicBuffer);
                }
                if (EconomyUtils.GetResources(Resource.Money, dynamicBuffer) < 0)
                {
                    EconomyUtils.SetResources(Resource.Money, dynamicBuffer, 0);
                }
            }

            public BufferLookup<Game.Economy.Resources> m_Resources;

            public Entity m_LandlordEntity;

            public NativeQueue<int> m_PaymentQueue;
        }

        private struct ReturnRentJob : IJobChunk
        {
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                if (chunk.GetSharedComponent(m_UpdateFrameType).m_Index != m_UpdateFrameIndex)
                {
                    return;
                }
                BufferAccessor<HouseholdCitizen> bufferAccessor = chunk.GetBufferAccessor(ref m_HouseholdCitizenType);
                DynamicBuffer<Game.Economy.Resources> dynamicBuffer = m_Resources[m_LandlordEntity];
                int num = EconomyUtils.GetResources(Resource.Money, dynamicBuffer);
                if (num < 0)
                {
                    return;
                }
                num /= 32;
                int num2 = CityStatisticsSystem.GetStatisticValue(m_StatisticsLookup, m_Statistics, StatisticType.EducationCount, 0);
                int num3 = CityStatisticsSystem.GetStatisticValue(m_StatisticsLookup, m_Statistics, StatisticType.EducationCount, 1);
                int num4 = CityStatisticsSystem.GetStatisticValue(m_StatisticsLookup, m_Statistics, StatisticType.EducationCount, 2);
                int num5 = CityStatisticsSystem.GetStatisticValue(m_StatisticsLookup, m_Statistics, StatisticType.EducationCount, 3);
                int num6 = CityStatisticsSystem.GetStatisticValue(m_StatisticsLookup, m_Statistics, StatisticType.EducationCount, 4);
                int num7 = num2 * m_EconomyParameters.m_RentReturnUneducated + num3 * m_EconomyParameters.m_RentReturnPoorlyEducated + num4 * m_EconomyParameters.m_RentReturnEducated + num5 * m_EconomyParameters.m_RentReturnWellEducated + num6 * m_EconomyParameters.m_RentReturnHighlyEducated;
                if (num7 == 0)
                {
                    return;
                }
                num2 = num * m_EconomyParameters.m_RentReturnUneducated / num7;
                num3 = num * m_EconomyParameters.m_RentReturnPoorlyEducated / num7;
                num4 = num * m_EconomyParameters.m_RentReturnEducated / num7;
                num5 = num * m_EconomyParameters.m_RentReturnWellEducated / num7;
                num6 = num * m_EconomyParameters.m_RentReturnHighlyEducated / num7;
                int num8 = 0;
                NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
                for (int i = 0; i < nativeArray.Length; i++)
                {
                    num = 0;
                    DynamicBuffer<HouseholdCitizen> dynamicBuffer2 = bufferAccessor[i];
                    for (int j = 0; j < dynamicBuffer2.Length; j++)
                    {
                        Entity citizen = dynamicBuffer2[j].m_Citizen;
                        if (m_Citizens.HasComponent(citizen))
                        {
                            CitizenAge age = m_Citizens[citizen].GetAge();
                            if (age == CitizenAge.Adult || age == CitizenAge.Elderly)
                            {
                                switch (m_Citizens[citizen].GetEducationLevel())
                                {
                                    case 0:
                                        num += num2;
                                        num8 += num2;
                                        break;
                                    case 1:
                                        num += num3;
                                        num8 += num3;
                                        break;
                                    case 2:
                                        num += num4;
                                        num8 += num4;
                                        break;
                                    case 3:
                                        num += num5;
                                        num8 += num5;
                                        break;
                                    case 4:
                                        num += num6;
                                        num8 += num6;
                                        break;
                                }
                            }
                        }
                    }
                    EconomyUtils.AddResources(Resource.Money, num, m_Resources[nativeArray[i]]);
                }
                m_PaymentQueue.Enqueue(-2 * num8);
            }

            void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                Execute(chunk, unfilteredChunkIndex, useEnabledMask, chunkEnabledMask);
            }

            [ReadOnly]
            public EntityTypeHandle m_EntityType;

            [ReadOnly]
            public BufferTypeHandle<HouseholdCitizen> m_HouseholdCitizenType;

            [ReadOnly]
            public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

            [NativeDisableParallelForRestriction]
            public BufferLookup<Game.Economy.Resources> m_Resources;

            [ReadOnly]
            public EconomyParameterData m_EconomyParameters;

            [ReadOnly]
            public ComponentLookup<Citizen> m_Citizens;

            [ReadOnly]
            public BufferLookup<CityStatistic> m_Statistics;

            [ReadOnly]
            public NativeParallelHashMap<CityStatisticsSystem.StatisticsKey, Entity> m_StatisticsLookup;

            public uint m_UpdateFrameIndex;

            public Entity m_LandlordEntity;

            public NativeQueue<int>.ParallelWriter m_PaymentQueue;
        }

        private struct TypeHandle
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AssignHandles(ref SystemState state)
            {
                EntityType = state.GetEntityTypeHandle();
                PropertyRenterLookup = state.GetComponentLookup<PropertyRenter>(true);
                RenterBufferType = state.GetBufferTypeHandle<Renter>(false);
                PrefabRefType = state.GetComponentTypeHandle<PrefabRef>(true);
                SpawnableBuildingDataLookup = state.GetComponentLookup<SpawnableBuildingData>(true);
                ResourcesBufferLookup = state.GetBufferLookup<Game.Economy.Resources>(false);
                BuildingPropertyDataLookup = state.GetComponentLookup<BuildingPropertyData>(true);
                PropertyOnMarketLookup = state.GetComponentLookup<PropertyOnMarket>(true);
                AbandonedLookup = state.GetComponentLookup<Abandoned>(true);
                DestroyedLookup = state.GetComponentLookup<Destroyed>(true);
                StorageCompanyLookup = state.GetComponentLookup<Game.Companies.StorageCompany>(true);
                HouseholdCitizenBufferType = state.GetBufferTypeHandle<HouseholdCitizen>(true);
                CitizenLookup = state.GetComponentLookup<Citizen>(true);
                CityStatisticsBufferLookup = state.GetBufferLookup<CityStatistic>(true);
            }

            [ReadOnly]
            public EntityTypeHandle EntityType;

            [ReadOnly]
            public ComponentLookup<PropertyRenter> PropertyRenterLookup;

            public BufferTypeHandle<Renter> RenterBufferType;

            [ReadOnly]
            public ComponentTypeHandle<PrefabRef> PrefabRefType;

            [ReadOnly]
            public ComponentLookup<SpawnableBuildingData> SpawnableBuildingDataLookup;

            public BufferLookup<Game.Economy.Resources> ResourcesBufferLookup;

            [ReadOnly]
            public ComponentLookup<BuildingPropertyData> BuildingPropertyDataLookup;

            [ReadOnly]
            public ComponentLookup<PropertyOnMarket> PropertyOnMarketLookup;

            [ReadOnly]
            public ComponentLookup<Abandoned> AbandonedLookup;

            [ReadOnly]
            public ComponentLookup<Destroyed> DestroyedLookup;

            [ReadOnly]
            public ComponentLookup<Game.Companies.StorageCompany> StorageCompanyLookup;

            [ReadOnly]
            public BufferTypeHandle<HouseholdCitizen> HouseholdCitizenBufferType;

            [ReadOnly]
            public ComponentLookup<Citizen> CitizenLookup;

            [ReadOnly]
            public BufferLookup<CityStatistic> CityStatisticsBufferLookup;
        }
    }
}
