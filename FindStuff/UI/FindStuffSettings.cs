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
        }

        [Preserve]
        public static DropdownItem<string>[] GetOperationModes( )
        {
            var localisationManager = GameManager.instance.localizationManager;

            return
            [
                new DropdownItem<string>
                {
                    value = "MoveFindStuff",
                    displayName = localisationManager.GetLocalizedName( "FindStuff.FindStuffSettings.MoveFindStuff" )
                },
                new DropdownItem<string>
                {
                    value = "HideFindStuff",
                    displayName = localisationManager.GetLocalizedName( "FindStuff.FindStuffSettings.HideFindStuff" )
                },
                new DropdownItem<string>
                {
                    value = "HideAssetMenu",
                    displayName = localisationManager.GetLocalizedName( "FindStuff.FindStuffSettings.HideAssetMenu" )
                },
            ];
        }

    }
}
