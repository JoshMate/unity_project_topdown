using UnityEngine;

public enum enumWeaponAmmoType
{
    AmmoEnergy,
    AmmoMagnum,
    AmmoOrdnance,
    AmmoPistol,
    AmmoRifle,
    AmmoShotgun
}

// Which held weapon sprite a firearm draws in front of the character holding it
public enum enumWeaponHeldSprite
{
    SmallArmSmall,
    SmallArmMedium,
    SmallArmBig,
    LongArmSmall,
    LongArmMedium,
    LongArmBig,
    LongArmVeryBig
}

// Values represent hundredths of a second so fractional delays remain valid enum values.
public enum enumWeaponDeployDelay
{
    DeployDelayInstant              = 0,
    DeployDelayOneHandedFast        = 10,
    DeployDelayOneHandedMedium      = 25,
    DeployDelayOneHandedSlow        = 40,
    DeployDelayTwoHandedFast        = 50,
    DeployDelayTwoHandedMedium      = 65,
    DeployDelayTwoHandedSlow        = 80,
    DeployDelayHeavyFast            = 100,
    DeployDelayHeavyMedium          = 125,
    DeployDelayHeavySlow            = 150,
    DeployDelaySlowest              = 200,
}
