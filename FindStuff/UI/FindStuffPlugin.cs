using Gooee;
using Gooee.Plugins;
using Gooee.Plugins.Attributes;

namespace FindStuff.UI
{
    [ControllerTypes( typeof( FindStuffController ) )]
    [GooeeSettingsMenu( typeof( FindStuffSettings ) )]
    [PluginToolbar( typeof( FindStuffController ), "OnToggleVisible", icon: "Media/Game/Icons/Zones.svg" )]
    public class FindStuffPlugin : IGooeePluginWithControllers, IGooeeChangeLog, IGooeeSettings, IGooeeLanguages, IGooeeStyleSheet
    {
        public string Name => "FindStuff";
        public string Version => MyPluginInfo.PLUGIN_VERSION;
        public string ScriptResource => "FindStuff.Resources.ui.js";
        public string ChangeLogResource => "FindStuff.Resources.changelog.md";
        public string StyleResource => "FindStuff.Resources.ui.css";

        public IController[] Controllers
        {
            get;
            set;
        }

        public string LanguageResourceFolder => "FindStuff.Resources.lang";

        public GooeeSettings Settings
        {

            get;
            set;
        }

    }
}
