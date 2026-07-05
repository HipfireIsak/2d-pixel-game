using UnityEngine;
using AetherEcho.Rendering;

namespace AetherEcho.World
{
    /// <summary>
    /// Legacy entry point kept for minimap color constants. World build uses DungeonTaleWorldBuilder.
    /// </summary>
    public static class WorldBiomeBuilder
    {
        public static readonly Color[] MinimapBiomeColors = DungeonTaleWorldBuilder.MinimapBiomeColors;
    }
}
