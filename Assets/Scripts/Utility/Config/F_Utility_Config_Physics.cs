public static class F_Utility_Config_Physics
{
    // Constants Public: Physics layers (Project Settings > Tags and Layers), separate from sprite sorting layers
    // Layer for anything that walks or slides around (player, NPCs); passes through ents with entFeatureCanWalkThrough
    public const string cfgPhysicsLayerMover = "Physics_Mover";
    // Layer for projectiles and bullets; passes over ents with entFeatureCanShootOver
    public const string cfgPhysicsLayerProjectile = "Physics_Projectile";

    // Constants Public: Knockback
    // How quickly knockback speed on movers (player, NPCs) fades, in world units per second, per second
    public const float cfgPhysicsKnockbackDecayRate = 25f;
}
