using FindStuff.Configuration;
using Game.Prefabs;
using System.IO;
using UnityEngine;

namespace FindStuff.Helper
{
    internal static class SurfaceExporter
    {
        /// <summary>
        /// Export a surface texture
        /// </summary>
        /// <param name="prefab"></param>
        /// <returns></returns>
        public static string Export( PrefabBase prefab )
        {            
            var components = prefab.components;

            foreach ( var c in components )
            {
                if ( c is RenderedArea renderedArea )
                {
                    var texture = CropTexture( ( Texture2D ) renderedArea.m_Material.GetTexture( "_BaseColorMap" ) );
                    var fileName = prefab.name.Replace( " ", "" );
                    var texturePath = Path.Combine( ConfigBase.MOD_PATH, "Surfaces" );
                    var path = Path.Combine( texturePath, fileName + ".png" );

                    Directory.CreateDirectory( texturePath );

                    if ( !File.Exists( path ) )
                        ExportToPNG( texture, path );

                    return $"coui://findstuffui/Surfaces/{fileName}.png";
                }
            }

            return null;
        }

        /// <summary>
        /// Exports a Texture2D to a file in PNG format.
        /// </summary>
        /// <param name="texture">The Texture2D to export.</param>
        /// <param name="filePath">The file path where the texture should be saved.</param>
        /// <returns>True if the texture was successfully saved, false otherwise.</returns>
        public static bool ExportToPNG( Texture2D texture, string filePath )
        {
            if ( texture == null )
            {
                Debug.LogError( "ExportToPNG failed: Texture2D is null." );
                return false;
            }

            // Convert the texture to a byte array in PNG format
            var pngData = texture.EncodeToPNG( );

            if ( pngData == null )
            {
                Debug.LogError( "ExportToPNG failed: Error in encoding texture to PNG." );
                return false;
            }

            try
            {
                // Write the byte array to the specified file path
                File.WriteAllBytes( filePath, pngData );
                Debug.Log( $"Texture exported successfully to {Path.GetFileName( filePath )}" );
                return true;
            }
            catch ( IOException e )
            {
                // Handle any IO exceptions (like path not found, no permission etc.)
                Debug.LogError( $"ExportToPNG failed: Unable to write file at {filePath}. Error: {e.Message}" );
                return false;
            }
        }

        /// <summary>
        /// Crop a surface texture to 128x128
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        private static Texture2D CropTexture( Texture2D src )
        {
            var tmp = RenderTexture.GetTemporary(
                                src.width,
                                src.height,
                                0,
                                RenderTextureFormat.Default,
                                RenderTextureReadWrite.Default );

            Graphics.Blit( src, tmp );

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = tmp;
            int startX = ( src.width / 2 ) - ( 128 / 2 );
            int startY = ( src.height / 2 ) - ( 128 / 2 );

            Texture2D readableTexture = new Texture2D( 128, 128, TextureFormat.RGBA32, false );
            readableTexture.ReadPixels( new Rect( startX, startY, 128, 128 ), 0, 0 );
            readableTexture.Apply( );
            readableTexture.name = src.name;

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary( tmp );

            return readableTexture;
        }
    }
}
