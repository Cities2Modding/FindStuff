using Colossal.Entities;
using Colossal.Mathematics;
using FindStuff.UI;
using Game.Buildings;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Rendering;
using Game.Tools;
using Game.Vehicles;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Scripting;

namespace FindStuff.Systems
{
    public partial class PickerToolSystem : ToolBaseSystem
    {
        private PrefabSystem _prefabSystem;
        private PloppableRICOSystem _ploppableRICOSystem;
        private FindStuffController _controller;
        private OverlayRenderSystem.Buffer _overlay;
        private ToolOutputBarrier _outputBarrier;
        private Entity _lastEntity = Entity.Null;

        public override string toolID => "PickStuff";

        static readonly UnityEngine.Color yellow = new UnityEngine.Color( 1f, 1f, 0f, 0.2f );

        [Preserve]
        public PickerToolSystem( )
        {
        }

        [Preserve]
        protected override void OnCreate()
        {
            _prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>( );
            _ploppableRICOSystem = World.GetOrCreateSystemManaged<PloppableRICOSystem>( );
            _outputBarrier = World.GetOrCreateSystemManaged<ToolOutputBarrier>( );
            base.OnCreate();
        }

        public override void InitializeRaycast( )
        {
            base.InitializeRaycast( );
            m_ToolRaycastSystem.netLayerMask = Layer.All;
            m_ToolRaycastSystem.typeMask = TypeMask.StaticObjects | TypeMask.Net | TypeMask.MovingObjects;
            m_ToolRaycastSystem.raycastFlags |= RaycastFlags.Placeholders | RaycastFlags.SubElements | RaycastFlags.Decals | RaycastFlags.Markers;
            m_ToolRaycastSystem.collisionMask = CollisionMask.Overground | CollisionMask.OnGround;
            m_ToolRaycastSystem.raycastFlags &= ~RaycastFlags.FreeCameraDisable;
        }

        private void DrawNetCurve( NetCompositionData compositionData,  EdgeGeometry edgeGeometry )
        {

            var halfWidth = compositionData.m_Width / 2f;

            var a1 = NetUtils.OffsetCurveLeftSmooth( edgeGeometry.m_Start.m_Left, -halfWidth );
            _overlay.DrawCurve( yellow, a1, compositionData.m_Width );

            var a2 = NetUtils.OffsetCurveLeftSmooth( edgeGeometry.m_End.m_Left, -halfWidth );
            _overlay.DrawCurve( yellow, a2, compositionData.m_Width );
        }

        [Preserve]
        protected override JobHandle OnUpdate( JobHandle inputDeps )
        {
            RemoveLastHighlighted( );

            if ( _prefabSystem != null )
            {
                var overlaySystem = World.GetExistingSystemManaged<OverlayRenderSystem>( );
                if ( overlaySystem != null )
                {
                    _controller = World.GetOrCreateSystemManaged<FindStuffController>( );
                    _overlay = overlaySystem.GetBuffer( out _ );
                }
            }
           
            if ( _controller.IsPicking && _controller != null &&
                GetRaycastResult( out Entity entity, out Game.Common.RaycastHit _ ) )
            {
                if ( EntityManager.TryGetComponent( entity, out PrefabRef prefabRef ) &&
                     _prefabSystem.TryGetPrefab( prefabRef.m_Prefab, out PrefabBase prefabBase ) &&
                    ( _controller.IsValidPrefab( prefabBase, entity ) ||
                    HasComponents( entity ) ) )
                {
                    if ( EntityManager.TryGetComponent<Game.Objects.Transform>( entity, out var transform ) )
                    {
                        var pos = new Unity.Mathematics.float3( );
                        var bounds = new Bounds3( );

                        if ( _prefabSystem.TryGetComponentData<ObjectGeometryData>( prefabBase, out var geometryData ) )
                        {
                            pos.y -= geometryData.m_Pivot.y;
                            bounds = geometryData.m_Bounds;
                        }

                        if ( _prefabSystem.TryGetComponentData<PlaceableObjectData>( prefabBase, out var objectData ) )
                        {
                            pos.y -= objectData.m_PlacementOffset.y;
                            if ( ( objectData.m_Flags & Game.Objects.PlacementFlags.Hanging ) != Game.Objects.PlacementFlags.None )
                                pos.y += bounds.max.y;
                        }

                        var sizeXY = bounds.xy.max - bounds.xy.min;
                        var size = ( sizeXY.x + sizeXY.y ) / 2f;
                        _overlay.DrawCircle( yellow, transform.m_Position, size > 0 ? size : 8f );
                    }

                    if ( EntityManager.TryGetComponent( entity, out Curve curve ) &&
                        EntityManager.TryGetComponent<Composition>( entity, out var composition ) &&
                        EntityManager.TryGetComponent<NetCompositionData>( composition.m_Edge, out var edgeCompositionData ) &&
                        EntityManager.TryGetComponent<EdgeGeometry>( entity, out var edgeGeometry ) )
                    {
                        DrawNetCurve( edgeCompositionData, edgeGeometry );
                    }

                    if ( Input.GetKeyDown( KeyCode.Mouse0 ) )
                    {
                        m_ToolSystem.activeTool = m_DefaultToolSystem;

                        _controller.UpdatePrefabFromPicker( prefabBase.name );
                        _controller.UpdatePicker( false );
                        
                        m_ToolSystem.ActivatePrefabTool( prefabBase );

                        if (_ploppableRICOSystem.IsPloppable(prefabBase, prefabRef.m_Prefab, entity))
                        {
                            Entity prefabEntity = _prefabSystem.GetEntity(prefabBase);
                            _ploppableRICOSystem.MakePloppable(prefabEntity, _outputBarrier.CreateCommandBuffer());
                        }

                        Unhighlight( entity );

                        return base.OnUpdate( inputDeps );
                    }

                    Highlight( entity );
                }


                _lastEntity = entity;
            }

            return base.OnUpdate( inputDeps );
        }

        private void Highlight( Entity entity )
        {
            if ( EntityManager.HasComponent<Highlighted>( entity ) )
                return;

            EntityManager.AddComponent<Highlighted>( entity );
            EntityManager.AddComponent<BatchesUpdated>( entity );
        }

        private void Unhighlight( Entity entity )
        {
            if ( !EntityManager.HasComponent<Highlighted>( entity ) )
                return;

            EntityManager.RemoveComponent<Highlighted>( entity );
            EntityManager.AddComponent<BatchesUpdated>( entity );
        }

        public void RemoveLastHighlighted( )
        {
            if ( _lastEntity != Entity.Null && EntityManager.HasComponent<Highlighted>( _lastEntity ) )
            {
                EntityManager.RemoveComponent<Highlighted>( _lastEntity );
                EntityManager.AddComponent<BatchesUpdated>( _lastEntity );
            }
        }

        private bool HasComponents( Entity entity )
        {
            if ( EntityManager.HasComponent<Deleted>( entity ) || EntityManager.HasComponent<Temp>( entity ) )
                return false;

            return EntityManager.HasComponent<Building>( entity ) ||
                EntityManager.HasComponent<Vehicle>( entity ) ||
                EntityManager.HasComponent<Game.Objects.Tree>( entity ) ||
                ( EntityManager.HasComponent<Game.Objects.Object>( entity ) &&
                EntityManager.HasComponent<SpawnableObjectData>( entity ) ) ||
                EntityManager.HasComponent<Node>( entity ) ||
                EntityManager.HasComponent<Edge>( entity ) ||
                EntityManager.HasComponent<Plant>( entity ) ||
                EntityManager.HasComponent<Curve>( entity );
        }

        // Unused
        public override PrefabBase GetPrefab( )
        {
            return default;
        }

        public override System.Boolean TrySetPrefab( PrefabBase prefab )
        {
            return false;
        }
    }
}
