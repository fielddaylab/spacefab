using UnityEngine;

namespace FieldDay.Rendering {
    static public class ShaderMath {
        static public Vector2 ComputePixelTiledTexCoords(Vector2 texCoord, Vector2 tileSizePixels, Vector2 pivot, Vector2 screenSize) {
            Vector2 tiles;
            tiles.x = screenSize.x / tileSizePixels.x;
            tiles.y = screenSize.y / tileSizePixels.y;
            
            Vector2 result;
            result.x = (texCoord.x * tiles.x) - (pivot.x * (int)tiles.x);
            result.y = (texCoord.y * tiles.y) - (pivot.y * (int)tiles.y);
            return result;
        }
    }
}