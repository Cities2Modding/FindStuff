using Game.Settings;
using Gooee;

namespace FindStuff.UI
{
    internal class FindStuffSettings : GooeeSettings
    {
        [SettingsUISection( "Toggles" )]
        public bool HideMenuOnSelection
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
            HideMenuOnSelection = false;
        }
    }
}
