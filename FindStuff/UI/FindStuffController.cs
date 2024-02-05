﻿using Game.Tools;
using Gooee.Plugins.Attributes;
using Gooee.Plugins;
using Unity.Entities;
using Game.Prefabs;
using System.Linq;
using System.Collections.Generic;
using Game.UI;
using System.Reflection;
using Colossal.Serialization.Entities;
using Game;
using Game.Companies;
using Colossal.Entities;
using Game.UI.InGame;
using UnityEngine.InputSystem;
using Colossal.IO.AssetDatabase;

namespace FindStuff.UI
{
    public class FindStuffController : Controller<FindStuffViewModel>
    {
        const string META_IS_DANGEROUS = "IsDangerous";
        const string META_IS_DANGEROUS_REASON = "IsDangerousReason";
        const string META_IS_SPAWNABLE = "IsSpawnable";

        private ToolSystem _toolSystem;
        private PrefabSystem _prefabSystem;
        private ImageSystem _imageSystem;
        private ToolbarUISystem _toolbarUISystem;
        private InputAction _enableAction;

        static FieldInfo _prefabsField = typeof( PrefabSystem ).GetField( "m_Prefabs", BindingFlags.Instance | BindingFlags.NonPublic );

        private Dictionary<string, PrefabBase> _prefabInstances = [];
        private EntityArchetype _entityArchetype;
        private EndFrameBarrier _endFrameBarrier;
        static MethodInfo _selectTool = typeof( ToolbarUISystem ).GetMethods( BindingFlags.Instance | BindingFlags.NonPublic )
            .FirstOrDefault( m => m.Name == "SelectAsset" && m.GetParameters( ).Length == 1 );

        private bool _isValidPrefab = false;
        private string _prefabType = "Unknown";
        private Dictionary<string, object> _meta = new Dictionary<string, object>();
        private List<string> _tags = new List<string>();

        public override FindStuffViewModel Configure( )
        {
            _toolSystem = World.GetOrCreateSystemManaged<ToolSystem>( );
            _prefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>( );
            _imageSystem = World.GetOrCreateSystemManaged<ImageSystem>( );
            _endFrameBarrier = World.GetOrCreateSystemManaged<EndFrameBarrier>( );
            _toolbarUISystem = World.GetOrCreateSystemManaged<ToolbarUISystem>( );

            _toolSystem.EventToolChanged += ( tool =>
            {
                if ( Model.IsVisible )
                {
                    Model.IsVisible = false;
                    TriggerUpdate( );
                }
            } );

            _entityArchetype = this.EntityManager.CreateArchetype( ComponentType.ReadWrite<Unlock>( ), ComponentType.ReadWrite<Game.Common.Event>( ) );

            var model = new FindStuffViewModel( );

            return model;
        }

        protected override void OnGameLoadingComplete( Purpose purpose, GameMode mode )
        {
            base.OnGameLoadingComplete( purpose, mode );

            if ( mode == GameMode.Game )
            {
                if ( _enableAction != null )
                {
                    _enableAction.Disable();
                    _enableAction.Dispose( );
                }

                _enableAction = new InputAction( "FindStuff_Toggle" );
                _enableAction.AddCompositeBinding( "ButtonWithOneModifier" )
                    .With( "Modifier", "<Keyboard>/CTRL" )
                    .With( "Button", "<Keyboard>/f" );
                _enableAction.performed += ( a ) => OnToggleVisible( );
                _enableAction.Enable( );

                var prefabs = ( List<PrefabBase> ) _prefabsField.GetValue( _prefabSystem );
                UnityEngine.Debug.Log("Getting prefabs");
                var prefabsList = new List<PrefabItem>();
                foreach ( var prefabBase in prefabs.Where(ProcessPrefab) )
                {
                    _tags.Clear();
                    _meta.Clear();

                    var entity = _prefabSystem.GetEntity( prefabBase );
                    var prefabIcon = "";

                    var thumbnail = _imageSystem.GetThumbnail( entity );
                    var typeIcon = GetTypeIcon(_prefabType);

                    if ( thumbnail == null || thumbnail == _imageSystem.placeholderIcon )
                        prefabIcon = typeIcon;
                    else
                        prefabIcon = thumbnail;

                    var prefabItem = new PrefabItem
                    {
                        Name = prefabBase.name,
                        Type = _prefabType,
                        Thumbnail = prefabIcon,
                        TypeIcon = typeIcon,
                        Meta = _meta,
                        Tags = _tags,
                    };
                    prefabsList.Add( prefabItem );

                    if ( _prefabInstances.ContainsKey( prefabBase.name ) )
                        _prefabInstances.Remove( prefabBase.name );

                    _prefabInstances.Add( prefabBase.name, prefabBase );
                }

                Model.Prefabs = prefabsList;
                TriggerUpdate( );
            }
        }

        protected override void OnDestroy( )
        {
            base.OnDestroy( );
            _enableAction?.Disable( );
            _enableAction?.Dispose( );
        }

        private bool ProcessPrefab(PrefabBase prefab)
        {
            var prefabEntity = _prefabSystem.GetEntity(prefab);
            bool isValid = false;
            _prefabType = "Unknown";

            if ( EntityManager.HasComponent<PlantData>( prefabEntity ) )
            {
                isValid = true;
                _prefabType = "Plant";
            }
            else if ( EntityManager.HasComponent<TreeData>( prefabEntity ) )
            {
                isValid = true;
                _prefabType = "Tree";
            }
            else if ( EntityManager.HasComponent<SurfaceData>( prefabEntity ) ) // Not working yet?
            {
                isValid = true;
                _prefabType = "Surface";
            }
            else if ( EntityManager.HasComponent<NetData>( prefabEntity ) )
            {
                // Flag invisible roads as dangerous. They can make your savegame break if not used properly.
                if (prefab.name.ToLower().Contains("invisible"))
                {
                    _meta.Add(META_IS_DANGEROUS, true);
                    _meta.Add(META_IS_DANGEROUS_REASON, "This asset could break your save game if not used properly.");
                }

                isValid = true;
                _prefabType = "Network";
            }
            else if (EntityManager.HasComponent<BuildingData>(prefabEntity) && EntityManager.HasComponent<ServiceObjectData>(prefabEntity))
            {
                isValid = true;
                _prefabType = "ServiceBuilding";
            }
            else if ( EntityManager.HasComponent<SignatureBuildingData>( prefabEntity ) )
            {
                isValid = true;
                _prefabType = "SignatureBuilding";
            }
            else if ( EntityManager.HasComponent<VehicleData>( prefabEntity ) )
            {
                if (EntityManager.HasComponent<TrainData>(prefabEntity))
                {
                    _meta.Add(META_IS_DANGEROUS, true);
                    _meta.Add(META_IS_DANGEROUS_REASON, "This asset can't be removed with bulldozer tool after placing.");
                }

                isValid = true;
                _prefabType = "Vehicle";
            }
            else if (EntityManager.TryGetComponent(prefabEntity, out SpawnableBuildingData spawnableBuildingData))
            {
                if (spawnableBuildingData.m_ZonePrefab != Entity.Null && EntityManager.TryGetComponent(spawnableBuildingData.m_ZonePrefab, out ZoneData zoneData))
                {
                    var areaType = zoneData.m_AreaType;
                    switch (areaType)
                    {
                        case Game.Zones.AreaType.Commercial:
                            _prefabType = "ZoneCommercial";
                            break;
                        case Game.Zones.AreaType.Residential:
                            _prefabType = "ZoneResidential";
                            break;
                        case Game.Zones.AreaType.Industrial:
                            ZonePrefab zonePrefab = _prefabSystem.GetPrefab<ZonePrefab>(spawnableBuildingData.m_ZonePrefab);
                            _prefabType = zonePrefab.m_Office ? "ZoneOffice" : "ZoneIndustrial";
                            break;
                    }

                    isValid = true;
                }
            }

            return isValid;
        }

        private string GetTypeIcon( string type )
        {
            switch ( type )
            {
                case "Plant":
                    return "Media/Game/Icons/Forest.svg";

                case "Tree":
                    return "Media/Game/Icons/Forest.svg";

                case "Network":
                    return "Media/Game/Icons/Roads.svg";

                case "ServiceBuilding":
                    return "Media/Game/Icons/Services.svg";

                case "SignatureBuilding":
                    return "Media/Game/Icons/ZoneSignature.svg";

                case "Zoneable":
                    return "Media/Game/Icons/Zones.svg";

                case "ZoneResidential":
                    return "Media/Game/Icons/ZoneResidential.svg";

                case "ZoneCommercial":
                    return "Media/Game/Icons/ZoneCommercial.svg";

                case "ZoneIndustrial":
                    return "Media/Game/Icons/ZoneIndustrial.svg";

                case "ZoneOffice":
                    return "Media/Game/Icons/ZoneOffice.svg";

                case "Vehicle":
                    return "Media/Game/Icons/Traffic.svg";

                case "Surface":
                    return "fa:solid-pencil";
            }

            return "";
        }

        private bool IsOffice(IndustrialProcessData industrialProcessData)
        {
            return industrialProcessData.m_Output.m_Resource switch
            {
                Game.Economy.Resource.Software or Game.Economy.Resource.Financial or Game.Economy.Resource.Media => true,
                _ => false,
            };
        }

        protected override void OnUpdate( )
        {
            base.OnUpdate( );
        }

        [OnTrigger]
        private void OnTestClick( )
        {
            //TriggerUpdate( );
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

        [OnTrigger]
        private void OnSelectPrefab( string name )
        {
            if ( string.IsNullOrEmpty( name ) )
                return;

            var prefabs = ( List<PrefabBase> ) _prefabsField.GetValue( _prefabSystem );
            var prefab = prefabs?.FirstOrDefault( p => p.name == name );

            if ( prefab != null )
            {
                var entity = _prefabSystem.GetEntity( prefab );

                if ( entity != Entity.Null )
                {
                    EntityCommandBuffer commandBuffer = _endFrameBarrier.CreateCommandBuffer( );

                    var unlockEntity = commandBuffer.CreateEntity( _entityArchetype );
                    commandBuffer.SetComponent<Unlock>( unlockEntity, new Unlock( entity ) );
                    _selectTool.Invoke( _toolbarUISystem, new object[] { entity } );
                }
            }
        }
    }
}
