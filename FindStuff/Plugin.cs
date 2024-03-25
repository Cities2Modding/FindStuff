using BepInEx;
using HarmonyLib;
using System.Reflection;
using System.Linq;
using Game.Simulation;
using System;
using FindStuff.Patches;
using Game.Common;
using Game.Buildings;

#if BEPINEX_V6
    using BepInEx.Unity.Mono;
#endif

namespace FindStuff
{
    [BepInPlugin( MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, "0.0.10" )]
    [BepInDependency("Gooee", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("LandValueOverhaul", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public static Type customPropertyRenterSystemType = null;
        public static Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

        private void Awake()
        {
            var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID + "_Cities2Harmony");

            /*
             * This adds LandValueOverhaul support by running a specific patch only 
             */
            if (HasLandValueOverhaul())
            {
                UnityEngine.Debug.Log("LandValueOverhaul found. Patch CustomPropertyRenterSystem instead.");
                harmony.Patch(customPropertyRenterSystemType
                    .GetMethod("OnCreate", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy), postfix: new HarmonyMethod(typeof(CustomPropertyRenterSystemPatch).GetMethod("Postfix")) {
                        after = ["LandValueOverhaul_Cities2Harmony"],
                    });
            } else
            {
                UnityEngine.Debug.Log("LandValueOverhaul not found. Patch original PropertyRenterSystem.");
                harmony.Patch(typeof(PropertyRenterSystem).GetMethod("OnCreate", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy), postfix: new HarmonyMethod(typeof(PropertyRenterSystemPatch).GetMethod(nameof(PropertyRenterSystemPatch.Postfix))));
            }

            harmony.Patch(typeof(SystemOrder).GetMethod("Initialize"), postfix: new HarmonyMethod(typeof(SystemOrderPatches).GetMethod("Postfix")));
            harmony.Patch(typeof(ZoneCheckSystem).GetMethod("OnCreate", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy), postfix: new HarmonyMethod(typeof(ZoneCheckSystem_Patches).GetMethod("Postfix")));

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

        public static bool HasLandValueOverhaul()
        {
            Assembly landValueOverhaulAssembly = assemblies.FirstOrDefault(a => a.GetName().Name == "LandValueOverhaul");
            if (landValueOverhaulAssembly != null )
            {
                customPropertyRenterSystemType = landValueOverhaulAssembly.GetTypes().FirstOrDefault(a => a.Name == "CustomPropertyRenterSystem");
                return true;
            }

            return false;
        }
    }
}
