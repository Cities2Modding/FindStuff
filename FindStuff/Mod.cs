using Colossal.Logging;
using Game;
using Game.Buildings;
using Game.Modding;
using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using FindStuff.Systems;

namespace FindStuff
{
    public class Mod : IMod
    {
        private static ILog _log = LogManager.GetLogger( "Cities2Modding" ).SetShowsErrorsInUI( false );
        public static Type customPropertyRenterSystemType = null;
        public static Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies( );

        public static Harmony _harmony;

        public void OnLoad( UpdateSystem updateSystem )
        {
            _harmony = new Harmony( "cities2modding_findstuff" );

            _log.Info( System.Environment.NewLine + @" +-+-+-+-+ +-+-+-+-+-+
 |F|i|n|d| |S|t|u|f|f|
 +-+-+-+-+ +-+-+-+-+-+" );

            _harmony.PatchAll();

            updateSystem.World.GetOrCreateSystemManaged<ZoneCheckSystem>().Enabled = false;
            updateSystem?.UpdateAt<CheckPloppableRICOSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem?.UpdateAt<PloppableRICOSystem>(SystemUpdatePhase.Modification5);
            updateSystem?.UpdateAt<PloppableRICORentSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem?.UpdateAt<CustomZoneCheckSystem>(SystemUpdatePhase.ModificationEnd);
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
                customPropertyRenterSystemType = landValueOverhaulAssembly.GetTypes( ).FirstOrDefault( a => a.Name == "PropertyRenterSystem" );
                return true;
            }

            return false;
        }
    }
}
