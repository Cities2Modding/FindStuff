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
using UnityEngine;
using UnityEngine.Scripting;

namespace FindStuff.Systems
{
    public class PickerToolSystem : GameSystemBase
    {
        private PrefabSystem _prefabSystem;
        private FindStuffController _controller;
        private OverlayRenderSystem.Buffer _overlay;

        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();
            m_ToolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
            m_DefaultTool = World.GetOrCreateSystemManaged<DefaultToolSystem>();
            m_ToolRaycastSystem = World.GetOrCreateSystemManaged<ToolRaycastSystem>();

            _prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>( );
            _controller = World.GetOrCreateSystemManaged<FindStuffController>( );
            _overlay = World.GetExistingSystemManaged<OverlayRenderSystem>( ).GetBuffer( out _ );
        }

        [Preserve]
        protected override void OnUpdate() 
        {
            RaycastResult raycastResult;
            PrefabRef prefabRef;

            m_ToolRaycastSystem.netLayerMask |= Layer.Road;
            m_ToolRaycastSystem.typeMask |= TypeMask.Net;

            if ((_controller.IsPicking || ShortcutIsEnabled()) && m_ToolSystem.activeTool == m_DefaultTool &&
                m_ToolRaycastSystem.GetRaycastResult(out raycastResult) && HasComponents(raycastResult) && 
                EntityManager.TryGetComponent(raycastResult.m_Owner, out prefabRef))
            {
                if ( EntityManager.TryGetComponent<Game.Objects.Transform>( raycastResult.m_Owner, out var transform ) )
                    _overlay.DrawCircle( UnityEngine.Color.yellow, transform.m_Position, 8f );

                if ( EntityManager.TryGetComponent(raycastResult.m_Owner, out Curve curve))
                    _overlay.DrawCurve( UnityEngine.Color.yellow, curve.m_Bezier, 1f );

                Entity prefab = prefabRef.m_Prefab;
                if (Input.GetKeyDown(KeyCode.Mouse0))
                {
                    if ( _prefabSystem.TryGetPrefab(prefab, out PrefabBase prefabBase))
                    {
                        m_ToolSystem.ActivatePrefabTool(prefabBase);
                    }

                    _controller.UpdatePicker( false );
                }
            }
        }

        bool HasComponents(RaycastResult raycastResult)
        {
            return EntityManager.HasComponent<Building>(raycastResult.m_Owner) ||
                EntityManager.HasComponent<Vehicle>(raycastResult.m_Owner) ||
                EntityManager.HasComponent<Game.Objects.Tree>(raycastResult.m_Owner) ||
                EntityManager.HasComponent<Node>(raycastResult.m_Owner) ||
                EntityManager.HasComponent<Edge>(raycastResult.m_Owner) ||
                EntityManager.HasComponent<Plant>(raycastResult.m_Owner);
        }

        private bool ShortcutIsEnabled()
        {
            return _controller.EnableShortcut && Input.GetKey(KeyCode.LeftControl);
        }

        [Preserve]
        public PickerToolSystem()
        {
        }

        private ToolSystem m_ToolSystem;

        private DefaultToolSystem m_DefaultTool;

        private ToolRaycastSystem m_ToolRaycastSystem;
    }
}
