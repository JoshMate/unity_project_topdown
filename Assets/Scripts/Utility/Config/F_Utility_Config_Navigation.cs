public static class F_Utility_Config_Navigation
{
    // Constants Public: NavMesh areas (Project Settings > Navigation > Areas)
    // Area index NPCs can walk on
    public const int cfgNavigationAreaWalkable = 0;
    // Area index that carves holes in the NavMesh
    public const int cfgNavigationAreaNotWalkable = 1;
    // NavMesh agent type the runtime surface is built for (0 = Humanoid)
    public const int cfgNavigationAgentTypeId = 0;

    // Constants Public: Rebuilding
    // Seconds to wait after a blocking ent dies before rebuilding, so its Destroy has completed and several deaths share one rebuild
    public const float cfgNavigationRebuildDelay = 0.25f;

    // Constants Public: Runtime objects
    // Name of the runtime NavMesh surface created in each gameplay scene
    public const string cfgNavigationSurfaceObjectName = "OB_Logic_NavigationSurface";
    // Name of the runtime walkable ground collider that the NavMesh is baked onto
    public const string cfgNavigationWalkableBaseObjectName = "OB_Logic_NavigationWalkableBase";
    // Physics layer of the walkable base, so default overlap and raycast queries skip it
    public const string cfgNavigationWalkableBaseLayer = "Ignore Raycast";
    // X rotation NavMeshPlus requires so the NavMesh lies on the XY plane
    public const float cfgNavigationSurfaceRotationX = -90f;
    // Hides NavMeshPlus build logs and warnings in the console
    public const bool cfgNavigationHideBuildLogs = true;

    // Constants Public: Sampling
    // Max distance from a position to the NavMesh when sampling a point
    public const float cfgNavigationSampleDistance = 1f;
    // How many random points are tried before giving up on finding one on the NavMesh
    public const int cfgNavigationRandomPointAttempts = 10;
}
