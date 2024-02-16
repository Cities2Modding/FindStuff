using Game.Prefabs;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using static Colossal.AssetPipeline.Diagnostic.Report;

namespace FindStuff.Helper
{
    public class SurfaceHelper( EntityManager entityManager ) : IBaseHelper
    {
        public string PrefabType => "Surface";

        public string CategoryType => "Misc";

        public Dictionary<string, object> CreateMeta( PrefabBase prefab, Entity entity )
        {
            return new Dictionary<string, object>( );
        }

        public List<string> CreateTags( PrefabBase prefab, Entity entity )
        {
            List<string> tags = new List<string>( );

            if ( entityManager == null )
                return tags;

            var lowerName = prefab.name.ToLowerInvariant( );

            tags.Add( "surface" );

            if ( lowerName.Contains( "grass" ) )
                tags.Add( "grass" );
            else if ( lowerName.Contains( "agriculture" ) )
                tags.Add( "agriculture" );
            else if ( lowerName.Contains( "sand" ) )
                tags.Add( "sand" );
            else if ( lowerName.Contains( "concrete" ) )
                tags.Add( "concrete" );
            else if ( lowerName.Contains( "forestry" ) )
                tags.Add( "forestry" );
            else if ( lowerName.Contains( "pavement" ) )
                tags.Add( "pavement" );
            else if ( lowerName.Contains( "landfill" ) )
                tags.Add( "landfill" );
            else if ( lowerName.Contains( "tiles " ) )
                tags.Add( "tiles" );
            else if ( lowerName.Contains( "oil " ) )
                tags.Add( "oil" );
            else if ( lowerName.Contains( "ore " ) )
                tags.Add( "ore" );
            else if ( lowerName.Contains( "asphalt" ) )
                tags.Add( "asphalt" );

            return tags.OrderBy( t => t ).ToList( );
        }

        public bool IsValidPrefab( PrefabBase prefab, Entity entity )
        {
            if ( entityManager == null || prefab == null || entity == Entity.Null )
                return false;

            return prefab is SurfacePrefab &&
                entityManager.HasComponent<RenderedAreaData>( entity ) && 
                entityManager.HasComponent<SurfaceData>( entity ) &&
                !prefab.name.ToLowerInvariant( ).EndsWith( "_placeholder" ) &&
                !prefab.name.ToLowerInvariant( ).EndsWith( " placeholder" );
        }
    }
}
