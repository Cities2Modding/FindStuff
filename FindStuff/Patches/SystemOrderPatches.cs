using FindStuff.Systems;
using Game;
using Game.Buildings;

namespace FindStuff.Patches
{
    public static class SystemOrderPatches
    {
        public static void Postfix( UpdateSystem updateSystem )
        {
            updateSystem?.UpdateAt<CheckPloppableRICOSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem?.UpdateAt<PloppableRICOSystem>(SystemUpdatePhase.Modification5);
            updateSystem?.UpdateAt<PloppableRICORentSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem?.UpdateAt<CustomZoneCheckSystem>(SystemUpdatePhase.ModificationEnd);
        }
    }
}
