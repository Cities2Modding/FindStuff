using Colossal.Entities;
using Game.Buildings;
using Game.Common;
using Game.Creatures;
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
    public class PickerToolSystem : SystemBase
    {
        PrefabSystem m_PrefabSystem;

        public bool IsPicking
        {
            get;
            set;
        } = false;

        private OverlayRenderSystem.Buffer _overlay;

        [Preserve]
        protected override void OnCreate()
        {
            base.OnCreate();
            m_ToolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
            m_DefaultTool = World.GetOrCreateSystemManaged<DefaultToolSystem>();
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            m_ToolRaycastSystem = World.GetOrCreateSystemManaged<ToolRaycastSystem>();
            _overlay = World.GetExistingSystemManaged<OverlayRenderSystem>( ).GetBuffer( out _ );

            UnityEngine.Debug.Log("[PickerTool]: Created!");
        }

        [Preserve]
        protected override void OnUpdate() 
        {
            RaycastResult raycastResult;
            PrefabRef prefabRef;

            if (IsPicking && m_ToolSystem.activeTool == m_DefaultTool &&
                m_ToolRaycastSystem.GetRaycastResult(out raycastResult) &&
                (EntityManager.HasComponent<Building>(raycastResult.m_Owner) ||
                EntityManager.HasComponent<Vehicle>(raycastResult.m_Owner) ||
                EntityManager.HasComponent<Game.Objects.NetObject>(raycastResult.m_Owner) ||
                EntityManager.HasComponent<Game.Objects.Tree>(raycastResult.m_Owner) ||
                EntityManager.HasComponent<Plant>(raycastResult.m_Owner)) &&
                EntityManager.TryGetComponent(raycastResult.m_Owner, out prefabRef))
            {

                if ( EntityManager.TryGetComponent<Game.Objects.Transform>( raycastResult.m_Owner, out var transform ) )
                    _overlay.DrawCircle( UnityEngine.Color.yellow, transform.m_Position, 8f );

                Entity prefab = prefabRef.m_Prefab;
                if (Input.GetKeyDown(KeyCode.Mouse0))
                {
                    if (m_PrefabSystem.TryGetPrefab(prefab, out PrefabBase prefabBase))
                    {
                        m_ToolSystem.ActivatePrefabTool(prefabBase);
                    }
                    
                    IsPicking = false;
                }
            }
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
