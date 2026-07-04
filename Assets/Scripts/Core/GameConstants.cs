namespace AetherEcho.Core
{
    public static class GameConstants
    {
        public const string BootstrapSceneName = "Bootstrap";
        public const string WorldSceneName = "World";

        public const int MaximumPlayersPerSession = 32;
        public const ushort DefaultServerPort = 7777;
        public const string DefaultServerAddress = "localhost";

        public const float PlayerMoveSpeedMetersPerSecond = 6.5f;
        public const float PlayerSprintMultiplier = 1.4f;
        public const float PlayerVisualScale = 1.1f;
        public const float TreeHeightMultiplier = 1.45f;
        public const float RockHeightMultiplier = 0.42f;
        public const float BaseManaRegenPerSecond = 3.5f;
        public const float ManaRegenPerIntelligence = 0.15f;

        // Classic 45° top-down: rotation (45, 0, 0), no yaw offset.
        public const float CameraPitchDegrees = 45f;
        public const float CameraYawDegrees = 0f;
        public const float CameraHeightMeters = 22f;
        public const float CameraBackOffsetMeters = 22f;
        public const float CameraOrthographicSize = 14f;
        public const float CameraFollowSmoothTime = 0.1f;

        public const string DataSpellsFileName = "spells.json";
        public const string DataClassesFileName = "classes.json";
        public const string DataQuestsFileName = "quests.json";

        public const string DefaultPlayerClass = "Mage";
        public const int DefaultPlayerLevel = 5;

        public const string SpellChronoBlast = "sp_chronoblast_01";
        public const string SpellTemporalBolt = "sp_temporal_bolt";
        public const string SpellManaSurge = "sp_mana_surge";

        public const int ObstacleLayerIndex = 8;

        public const float GroundHeight = 0f;
        public const float FloorVisualHeight = 0.02f;
        public const float WorldHalfExtentMeters = 80f;
        public const int ProceduralTreeCount = 140;
        public const int ProceduralRockCount = 90;
        public const int ProceduralBushCount = 70;
        public const int ProceduralMushroomCount = 45;
        public const float SpawnSafeRadiusMeters = 10f;

        public const float FlatColliderHeight = 0.12f;
        public const float PlayerCollisionRadius = 0.32f;
        public const float EnemyCollisionRadius = 0.38f;
        public const float SpellProjectileSpeedMetersPerSecond = 18f;

        public const int GroundSortingOrder = -10000;
        public const int EntitySortingBaseOffset = 1000;
        public const int SortingOrderPerMeter = 100;
    }
}
