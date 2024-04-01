using cohtml.Net;
using Game.SceneFlow;
using Game.Settings;
using Game.UI.Widgets;
using Gooee;

namespace FindStuff.UI
{
    internal class FindStuffSettings : GooeeSettings
    {
        [SettingsUISection( "Toggles" )]//SettingsUIDropdownAttribute
        public string OperationMode
        {
            get;
            set;
        }

        [SettingsUISection( "Toggles" )]
        public bool EnableShortcut
        {
            get;
            set;
        }

        [SettingsUISection( "Toggles" )]
        public bool ExpertMode
        {
            get;
            set;
        }

        [SettingsUISection( "Toggles" )]
        public string SearchSpeed
        {
            get;
            set;
        }

        [SettingsUISection( "Toggles" )]
        public bool AutomaticUnlocks
        {
            get;
            set;
        }

        [SettingsUIHidden]
        protected override string UIResource => "FindStuff.Resources.settings.xml";

        public FindStuffSettings( )
        {
        }

        public override void SetDefaults( )
        {
            OperationMode = "HideFindStuff";
            ExpertMode = false;
            EnableShortcut = false;
            SearchSpeed = "VeryHigh";
            AutomaticUnlocks = true;
        }

        [Preserve]
        public static DropdownItem<string>[] GetOperationModes( )
        {
            var localisationManager = GameManager.instance.localizationManager;

            return
            [
                new DropdownItem<string>
                {
                    value = ViewOperationMode.MoveFindStuff.ToString( ),
                    displayName = localisationManager.GetLocalizedName( "FindStuff.FindStuffSettings.MoveFindStuff" )
                },
                new DropdownItem<string>
                {
                    value = ViewOperationMode.HideFindStuff.ToString( ),
                    displayName = localisationManager.GetLocalizedName( "FindStuff.FindStuffSettings.HideFindStuff" )
                },
                new DropdownItem<string>
                {
                    value = ViewOperationMode.HideFindStuffSideMenu.ToString( ),
                    displayName = localisationManager.GetLocalizedName( "FindStuff.FindStuffSettings.HideFindStuffSideMenu" )
                },
                new DropdownItem<string>
                {
                    value = ViewOperationMode.HideAssetMenu.ToString( ),
                    displayName = localisationManager.GetLocalizedName( "FindStuff.FindStuffSettings.HideAssetMenu" )
                },
            ];
        }

        [Preserve]
        public static DropdownItem<string>[] GetSearchSpeeds( )
        {
            var localisationManager = GameManager.instance.localizationManager;

            return
            [
                new DropdownItem<string>
                {
                    value = "VeryLow",
                    displayName = localisationManager.GetLocalizedName( "FindStuff.FindStuffSettings.SearchSpeed.VeryLow" )
                },
                new DropdownItem<string>
                {
                    value = "Low",
                    displayName = localisationManager.GetLocalizedName( "FindStuff.FindStuffSettings.SearchSpeed.Low" )
                },
                new DropdownItem<string>
                {
                    value = "Medium",
                    displayName = localisationManager.GetLocalizedName( "FindStuff.FindStuffSettings.SearchSpeed.Medium" )
                },
                new DropdownItem<string>
                {
                    value = "High",
                    displayName = localisationManager.GetLocalizedName( "FindStuff.FindStuffSettings.SearchSpeed.High" )
                },
                new DropdownItem<string>
                {
                    value = "VeryHigh",
                    displayName = localisationManager.GetLocalizedName( "FindStuff.FindStuffSettings.SearchSpeed.VeryHigh" )
                },
            ];
        }
    }
}
