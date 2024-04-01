using Game.Modding;
using Game.Simulation;
using Game.UI;
using HarmonyLib;
using System.Reflection;

namespace FindStuff.Patches
{
    internal class UIModManagerPatches
    {
        [HarmonyPatch(typeof(ModManager), "InitializeUIModules")]
        public static class ModManager_InitializeUIModulesPatch
        {
            public static void Postfix(UISystemBootstrapper __instance)
            {
                /*
                * This adds LandValueOverhaul support by running a specific patch only 
                */
                if ( Mod.HasLandValueOverhaul( ) )
                {
                    UnityEngine.Debug.Log( "LandValueOverhaul found. Patch CustomPropertyRenterSystem instead." );
                    Mod._harmony.Patch( Mod.customPropertyRenterSystemType
                        .GetMethod( "OnCreate", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy ), prefix: new HarmonyMethod( typeof( CustomPropertyRenterSystemPatch ).GetMethod( "Prefix" ) )
                        {
                            after = ["LandValueOverhaul_Cities2Harmony"],
                        } );
                }
                else
                {
                    UnityEngine.Debug.Log( "LandValueOverhaul not found. Patch original PropertyRenterSystem." );
                    Mod._harmony.Patch( typeof( PropertyRenterSystem ).GetMethod( "OnUpdate", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy ), prefix: new HarmonyMethod( typeof( PropertyRenterSystemPatch ).GetMethod( nameof( PropertyRenterSystemPatch.Prefix ) ) ) );
                }
            }
        }
    }
}
