namespace Project.Scripts.Shared
{
    public static class GameConstants
    {
        public static class Players
        {
            public const int HumanPlayerId = 0;
        }

        public static class Movement
        {
            public const float PlayerMoveSpeed = 3f;
            public const float HumanInputDeadZoneSqr = 0.0001f;
            public const float AxisEpsilon = 1e-5f;
            public const float MaxDriftThreshold = 2.0f;
            public const int MaxInputHistory = 256;
            public const int MaxHumanInputsPerFixedStep = 32;
            public const float CollisionExtrapolationSeconds = 0.08f;
            public const float ReconcileCorrectionMaxSpeed = 10f;
            public const float ReconcileMinErrorDistance = 2.0f;
            public const float MaxInputDeltaTimeSeconds = 0.05f;
        }

        public static class Interpolation
        {
            public const float DelayMinSeconds = 0.05f;
            public const float DelayMaxSeconds = 0.6f;
            public const float DelaySmoothing = 0.12f;
            public const float DelayExtraMargin = 0.04f;
        }

        public static class Eggs
        {
            public const float CollectionRadius = 1.0f;
            public const float FallbackVisualScale = 0.5f;
            public const float MinSpawnIntervalFloor = 0.05f;
            public const int PoolPrewarmPerPrefab = 8;
        }

        public static class Bots
        {
            public const float WaypointReachedDistance = 0.1f;
            public const int InvalidEggTargetId = -1;
        }

        public static class DebugGizmos
        {
            public const float PathNodeWireCubeSize = 0.5f;
            public const float BotTargetSphereRadius = 0.3f;
        }

        public static class Camera
        {
            public const float DefaultOrthographicSize = 8f;
        }

        public static class Time
        {
            public const float MillisecondsToSeconds = 0.001f;
        }

        public static class Grid
        {
            public const float GizmoDrawShrink = 0.1f;
        }
    }
}
