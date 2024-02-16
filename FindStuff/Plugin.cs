using BepInEx;
using HarmonyLib;
using System.Reflection;
using System.Linq;

#if BEPINEX_V6
    using BepInEx.Unity.Mono;
#endif

namespace FindStuff
{
    [BepInPlugin( MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, "0.0.7" )]
    public class Plugin : BaseUnityPlugin
    {
        private void Awake( )
        {
            var harmony = Harmony.CreateAndPatchAll( Assembly.GetExecutingAssembly( ), MyPluginInfo.PLUGIN_GUID + "_Cities2Harmony" );

            var patchedMethods = harmony.GetPatchedMethods( ).ToArray( );

            Logger.LogInfo( System.Environment.NewLine + @" +-+-+-+-+ +-+-+-+-+-+
 |F|i|n|d| |S|t|u|f|f|
 +-+-+-+-+ +-+-+-+-+-+" );
            
            // Plugin startup logic
            Logger.LogInfo( $"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded! Patched methods: " + patchedMethods.Length );

            foreach ( var patchedMethod in patchedMethods )
            {
                Logger.LogInfo( $"Patched method: {patchedMethod.Module.Name}:{patchedMethod.Name}" );
            }
        }
    }
}
