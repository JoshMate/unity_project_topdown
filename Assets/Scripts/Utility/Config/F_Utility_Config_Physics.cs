public static class F_Utility_Config_Physics
{
    // Constants Public: Physics layers (Project Settings > Tags and Layers), separate from sprite sorting layers
    // Layer for anything that walks or slides around (player, NPCs); passes through ents with entFeatureCanWalkThrough
    public const string cfgPhysicsLayerMover = "Physics_Mover";
    // Layer for projectiles and bullets; passes over ents with entFeatureCanShootThrough
    public const string cfgPhysicsLayerProjectile = "Physics_Projectile";
}
