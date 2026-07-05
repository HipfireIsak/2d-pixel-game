using UnityEngine;
using AetherEcho.Core;

namespace AetherEcho.World
{
    public static class WorldAtmosphere
    {
        public static void ApplyDrakantosStyle()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.72f, 0.76f, 0.68f);
            RenderSettings.fog = false;
        }

        public static void ApplyToCamera(Camera camera)
        {
            if (camera == null)
            {
                return;
            }

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.09f, 0.12f, 0.1f);
        }
    }
}
