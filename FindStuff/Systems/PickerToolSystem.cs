using Colossal.Entities;
using FindStuff.UI;
using Game;
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
    public class PickerToolSystem : ToolBaseSystem
    {
        private PrefabSystem _prefabSystem;
        private FindStuffController _controller;
        private OverlayRenderSystem.Buffer _overlay;
        private Entity _lastEntity = Entity.Null;

        public override string toolID => "PickStuff";

        [Preserve]
        public PickerToolSystem( )
        {
        }

        [Preserve]
        protected override void OnCreate()
        {
            _prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>( );
            base.OnCreate();
        }

        public override void InitializeRaycast( )
        {
            base.InitializeRaycast( );
            m_ToolRaycastSystem.netLayerMask = Layer.All;
            m_ToolRaycastSystem.typeMask = TypeMask.StaticObjects | TypeMask.Net | TypeMask.MovingObjects;
            m_ToolRaycastSystem.raycastFlags |= RaycastFlags.Placeholders | RaycastFlags.SubElements | RaycastFlags.Decals;
            m_ToolRaycastSystem.collisionMask = CollisionMask.Overground | CollisionMask.OnGround;
            m_ToolRaycastSystem.raycastFlags &= ~RaycastFlags.FreeCameraDisable;
        }

        private void DrawNetCurve( NetCompositionData compositionData,  EdgeGeometry edgeGeometry )
        {
            var yellow = new UnityEngine.Color( 1f, 1f, 0f, 0.5f );

            var halfWidth = compositionData.m_Width / 2f;

            var a1 = NetUtils.OffsetCurveLeftSmooth( edgeGeometry.m_Start.m_Left, -halfWidth );
            _overlay.DrawCurve( yellow, a1, compositionData.m_Width );

            var a2 = NetUtils.OffsetCurveLeftSmooth( edgeGeometry.m_End.m_Left, -halfWidth );
            _overlay.DrawCurve( yellow, a2, compositionData.m_Width );

            //a1 = NetUtils.OffsetCurveLeftSmooth( edgeGeometry.m_Start.m_Left, eighthWidth );
            //_overlay.DrawCurve( white, a1, 0f );

            //a2 = NetUtils.OffsetCurveLeftSmooth( edgeGeometry.m_End.m_Left, eighthWidth );
            //_overlay.DrawCurve( white, a2, 0f );

            //var b1 = NetUtils.OffsetCurveLeftSmooth( edgeGeometry.m_Start.m_Right, -eighthWidth );
            //_overlay.DrawCurve( white, b1, 0f );

            //var b2 = NetUtils.OffsetCurveLeftSmooth( edgeGeometry.m_End.m_Right, -eighthWidth );
            //_overlay.DrawCurve( white, b2, 0f );
        }

        [Preserve]
        protected override JobHandle OnUpdate( JobHandle inputDeps )
        {
            if ( _prefabSystem != null )
            {
                var overlaySystem = World.GetExistingSystemManaged<OverlayRenderSystem>( );

                if ( overlaySystem != null )
                {
                    _controller = World.GetOrCreateSystemManaged<FindStuffController>( );
                    _overlay = overlaySystem.GetBuffer( out _ );
                }
            }

            //if ( _prefabSystem == null )
            //    return base.OnUpdate( inputDeps );

            if ( _controller.IsPicking && _controller != null && GetRaycastResult( out Entity entity, out Game.Common.RaycastHit hitInfo ) )
            {
                var yellow = new UnityEngine.Color( 1f, 1f, 0f, 0.5f );

                if ( EntityManager.TryGetComponent<Game.Objects.Transform>( entity, out var transform ) )
                    _overlay.DrawCircle( yellow, transform.m_Position, 8f );

                if ( EntityManager.TryGetComponent( entity, out Curve curve ) &&
                    EntityManager.TryGetComponent<Composition>( entity, out var composition ) &&
                    EntityManager.TryGetComponent<NetCompositionData>( composition.m_Edge, out var edgeCompositionData ) &&
                    EntityManager.TryGetComponent<EdgeGeometry>( entity, out var edgeGeometry ) )
                {
                    DrawNetCurve( edgeCompositionData, edgeGeometry );
                }

                if ( EntityManager.TryGetComponent( entity, out PrefabRef prefabRef ) )
                {
                    Entity prefab = prefabRef.m_Prefab;
                    if ( Input.GetKeyDown( KeyCode.Mouse0 ) )
                    {
                        UnityEngine.Debug.Log( "GOT PREFAB" );
                        m_ToolSystem.activeTool = m_DefaultToolSystem;

                        _controller.UpdatePicker( false );

                        if ( _prefabSystem.TryGetPrefab( prefab, out PrefabBase prefabBase ) )
                        {
                            m_ToolSystem.ActivatePrefabTool( prefabBase );
                        }
                    }
                }

                if ( _lastEntity != entity && _lastEntity != Entity.Null && EntityManager.HasComponent<Highlighted>( _lastEntity ) )
                {
                    EntityManager.RemoveComponent<Highlighted>( _lastEntity );
                    EntityManager.AddComponent<Updated>( _lastEntity );
                }

                if ( !EntityManager.HasComponent<Highlighted>( entity ) )
                {
                    EntityManager.AddComponent<Highlighted>( entity );
                    EntityManager.AddComponent<Updated>( entity );
                }

                _lastEntity = entity;
            }
            else if ( _lastEntity != Entity.Null && EntityManager.HasComponent<Highlighted>( _lastEntity ) )
            {
                EntityManager.RemoveComponent<Highlighted>( _lastEntity );
                EntityManager.AddComponent<Updated>( _lastEntity );
            }

            return base.OnUpdate( inputDeps );
        }

        //protected override void OnUpdate() 
        //{
        //    RaycastResult raycastResult;
        //    PrefabRef prefabRef;

        //    //m_ToolRaycastSystem.netLayerMask |= Layer.Road;
        //    //m_ToolRaycastSystem.typeMask |= TypeMask.Net;

        //    m_ToolRaycastSystem.netLayerMask = Layer.All;
        //    m_ToolRaycastSystem.typeMask = TypeMask.StaticObjects | TypeMask.Net | TypeMask.MovingObjects;
        //    m_ToolRaycastSystem.raycastFlags |= RaycastFlags.Placeholders | RaycastFlags.SubElements | RaycastFlags.Decals;
        //    m_ToolRaycastSystem.collisionMask = CollisionMask.Overground | CollisionMask.OnGround;

        //    //if ( GetNetRaycastResult( out var entity, out var hitInfo ) &&
        //    //    EntityManager.TryGetComponent( entity, out Curve curve ) )
        //    //     _overlay.DrawCurve( UnityEngine.Color.yellow, curve.m_Bezier, 1f );
        //    if ( m_ToolRaycastSystem.GetRaycastResult( out raycastResult ) &&
        //        EntityManager.TryGetComponent( raycastResult.m_Owner, out Curve curve ) &&
        //        EntityManager.TryGetComponent<Composition>( raycastResult.m_Owner, out var composition ) &&
        //        EntityManager.TryGetComponent<NetCompositionData>( composition.m_Edge, out var edgeCompositionData ) )
        //    {

        //        //EntityManager.TryGetComponent( raycastResult.m_Owner, out NetCompositionData compositionData ) )
        //        _overlay.DrawCurve( UnityEngine.Color.yellow, curve.m_Bezier, edgeCompositionData.m_Width/*compositionData.m_Width*/ );
        //    }

    //        if ((_controller.IsPicking || ShortcutIsEnabled( )) && m_ToolSystem.activeTool == m_DefaultTool &&
    //            m_ToolRaycastSystem.GetRaycastResult(out raycastResult) && HasComponents( raycastResult) && 
    //            EntityManager.TryGetComponent(raycastResult.m_Owner, out prefabRef))
    //        {
    //            if (EntityManager.TryGetComponent<Game.Objects.Transform>(raycastResult.m_Owner, out var transform ) )
    //                _overlay.DrawCircle(UnityEngine.Color.yellow, transform.m_Position, 8f );

    //            Entity prefab = prefabRef.m_Prefab;
    //            if (Input.GetKeyDown(KeyCode.Mouse0))
    //            {
    //                if (_prefabSystem.TryGetPrefab(prefab, out PrefabBase prefabBase))
    //                {
    //                    m_ToolSystem.ActivatePrefabTool(prefabBase);
    //                }

    //_controller.UpdatePicker( false );
    //            }
    //        }
        //}

        bool HasComponents(RaycastResult raycastResult)
        {
            return EntityManager.HasComponent<Building>(raycastResult.m_Owner) ||
                EntityManager.HasComponent<Vehicle>(raycastResult.m_Owner) ||
                EntityManager.HasComponent<Game.Objects.Tree>(raycastResult.m_Owner) ||
                EntityManager.HasComponent<Node>(raycastResult.m_Owner) ||
                EntityManager.HasComponent<Edge>(raycastResult.m_Owner) ||
                EntityManager.HasComponent<Plant>(raycastResult.m_Owner);
        }

        // Unused
        public override PrefabBase GetPrefab( )
        {
            return default;
        }

        public override System.Boolean TrySetPrefab( PrefabBase prefab )
        {
            return true;
        }
    }
}
