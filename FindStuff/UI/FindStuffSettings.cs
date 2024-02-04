using Game.Settings;
using Gooee;

namespace FindStuff.UI
{
    internal class FindStuffSettings : GooeeSettings
    {
        [SettingsUISection( "ChangeLog" )]
        public bool ChangeLog
        {
            get;
            set;
        }

        [SettingsUIHidden]
        protected override string UIResource => null;

        public FindStuffSettings( )
        {
        }

        public override void SetDefaults( )
        {
            ChangeLog = true;
        }
    }
}
