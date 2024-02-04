using Game.Tools;
using Gooee.Plugins.Attributes;
using Gooee.Plugins;
using System;
using Unity.Entities;
using Game.Prefabs;
using System.Linq;
using System.Collections.Generic;
using Game.UI;
using System.Reflection;
using Newtonsoft.Json;
using Colossal.Entities;

namespace FindStuff.UI
{
    public class FindStuffController : Controller<FindStuffViewModel>
    {
        private ToolSystem _toolSystem;
        private PrefabSystem _prefabSystem;
        private ImageSystem _imageSystem;

        static FieldInfo _prefabsField = typeof( PrefabSystem ).GetField( "m_Prefabs", BindingFlags.Instance | BindingFlags.NonPublic );

        private Dictionary<string, PrefabBase> _prefabInstances = [];

        public override FindStuffViewModel Configure( )
        {
            _toolSystem = World.GetOrCreateSystemManaged<ToolSystem>( );
            _prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>( );
            _imageSystem = World.GetOrCreateSystemManaged<ImageSystem>( );

            _toolSystem.EventToolChanged += ( tool =>
            {
                if ( Model.IsVisible )
                {
                    Model.IsVisible = false;
                    TriggerUpdate( );
                }
            } );

            var model = new FindStuffViewModel( );

            var prefabs = ( List<PrefabBase> ) _prefabsField.GetValue( _prefabSystem );
            UnityEngine.Debug.Log( "Getting prefabs" );
            var prefabsList = new List<PrefabItem>( );

            Func<PrefabBase, bool> hasAType = ( p ) =>
            {
                return EntityManager.HasComponent<TreeData>( _prefabSystem.GetEntity( p ) ) ||
                    EntityManager.HasComponent<SpawnableBuildingData>( _prefabSystem.GetEntity( p ) );
            };

            foreach ( var prefabBase in prefabs.Where( p => hasAType( p ) ) )
            {
                var entity = _prefabSystem.GetEntity( prefabBase );

                //var objectPrefab = _prefabSystem.GetPrefab<ObjectPrefab>( prefabEntity );

                //if ( objectPrefab == null )
                //    continue;

                var isSpawnableBuilding = EntityManager.HasComponent<SpawnableBuildingData>( entity );
                var prefabIcon = "";

                var thumbnail = _imageSystem.GetThumbnail( entity );
                var typeIcon = "";

                if ( EntityManager.TryGetComponent<SpawnableBuildingData>( entity, out var component ) )
                {
                    string iconOrGroupIcon = _imageSystem.GetIconOrGroupIcon( component.m_ZonePrefab );
                    if ( iconOrGroupIcon != null )
                    {
                        typeIcon = iconOrGroupIcon;
                    }
                }

                string iconOrGroupIcon2 = _imageSystem.GetIconOrGroupIcon( entity );
                if ( iconOrGroupIcon2 != null )
                {
                    typeIcon = iconOrGroupIcon2;
                }

                if ( thumbnail == null || thumbnail == "Media/Placeholder.svg" )
                {
                    prefabIcon = typeIcon;
                }
                else
                {
                    prefabIcon = thumbnail;
                }
     
                var icon = prefabIcon;
                var prefabItem = new PrefabItem { Name = prefabBase.name, Thumbnail = icon, TypeIcon = typeIcon };
                prefabsList.Add( prefabItem );
                //UnityEngine.Debug.Log( "Got? prefab: " + JsonConvert.SerializeObject( prefabItem ) );
                // UnityEngine.Debug.Log( "Got prefab: " + prefabBase.name  + " icon: "+ icon );
                _prefabInstances.Add( prefabBase.name, prefabBase );
            }

            model.Prefabs = prefabsList;

            return model;
        }

        protected override void OnUpdate( )
        {
            base.OnUpdate( );
        }

        [OnTrigger]
        private void OnTestClick( )
        {
            Model.Message = "An amended message! " + DateTime.Now;
            TriggerUpdate( );
        }

        [OnTrigger]
        private void OnToggleVisible( )
        {
            Model.IsVisible = !Model.IsVisible;
            //_toolSystem.activeTool = World.GetOrCreateSystemManaged<ManualDuckToolSystem>( );

            //if (_toolSystem.activeTool != null && _toolSystem.activeTool is ManualDuckToolSystem duckTool )
            //{
            //    duckTool.SetActive( Model.IsVisible );
            //}
            TriggerUpdate( );
        }
    }
}
