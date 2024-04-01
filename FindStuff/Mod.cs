using Colossal.Logging;
using FindStuff.Patches;
using Game;
using Game.Buildings;
using Game.Common;
using Game.Modding;
using Game.Simulation;
using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;

namespace FindStuff
{
    public class Mod : IMod
    {
        private static ILog _log = LogManager.GetLogger( "Cities2Modding" ).SetShowsErrorsInUI( false );
        public static Type customPropertyRenterSystemType = null;
        public static Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies( );

        private Harmony _harmony;

        public void OnLoad( UpdateSystem updateSystem )
        {
            _harmony = new Harmony( "cities2modding_findstuff" );

            ///*
            // * This adds LandValueOverhaul support by running a specific patch only 
            // */
            //if ( HasLandValueOverhaul( ) )
            //{
            //    UnityEngine.Debug.Log( "LandValueOverhaul found. Patch CustomPropertyRenterSystem instead." );
            //    _harmony.Patch( customPropertyRenterSystemType
            //        .GetMethod( "OnCreate", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy ), postfix: new HarmonyMethod( typeof( CustomPropertyRenterSystemPatch ).GetMethod( "Postfix" ) )
            //        {
            //            after = ["LandValueOverhaul_Cities2Harmony"],
            //        } );
            //}
            //else
            //{
            //    UnityEngine.Debug.Log( "LandValueOverhaul not found. Patch original PropertyRenterSystem." );
            //    _harmony.Patch( typeof( PropertyRenterSystem ).GetMethod( "OnCreate", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy ), postfix: new HarmonyMethod( typeof( PropertyRenterSystemPatch ).GetMethod( nameof( PropertyRenterSystemPatch.Postfix ) ) ) );
            //}

            //_harmony.Patch( typeof( SystemOrder ).GetMethod( "Initialize" ), postfix: new HarmonyMethod( typeof( SystemOrderPatches ).GetMethod( "Postfix" ) ) );
            //_harmony.Patch( typeof( ZoneCheckSystem ).GetMethod( "OnCreate", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy ), postfix: new HarmonyMethod( typeof( ZoneCheckSystem_Patches ).GetMethod( "Postfix" ) ) );

            _log.Info( System.Environment.NewLine + @" +-+-+-+-+ +-+-+-+-+-+
 |F|i|n|d| |S|t|u|f|f|
 +-+-+-+-+ +-+-+-+-+-+" );
        }

        public void OnDispose( )
        {
            _harmony?.UnpatchAll( "cities2modding_findstuff" );
        }

        public static bool HasLandValueOverhaul( )
        {
            Assembly landValueOverhaulAssembly = assemblies.FirstOrDefault( a => a.GetName( ).Name == "LandValueOverhaul" );
            if ( landValueOverhaulAssembly != null )
            {
                customPropertyRenterSystemType = landValueOverhaulAssembly.GetTypes( ).FirstOrDefault( a => a.Name == "CustomPropertyRenterSystem" );
                return true;
            }

            return false;
        }
    }
}
