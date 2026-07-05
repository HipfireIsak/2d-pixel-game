using UnityEngine;

namespace AetherEcho.World
{
    public static class WorldRenderMaterials
    {
        private static Material cachedTilemapMaterial;

        public static Material ResolveTilemapMaterial(Material preferred)
        {
            if (preferred != null && preferred.shader != null && preferred.shader.isSupported)
            {
                return preferred;
            }

            if (cachedTilemapMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader == null)
                {
                    shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
                }

                if (shader != null && shader.isSupported)
                {
                    cachedTilemapMaterial = new Material(shader);
                }
                else
                {
                    Debug.LogError("[WorldRenderMaterials] No supported 2D sprite shader found for tilemaps.");
                }
            }

            return cachedTilemapMaterial;
        }
    }
}
