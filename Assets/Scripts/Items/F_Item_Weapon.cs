using UnityEngine;

public class F_Item_Weapon: MonoBehaviour
{

    [Header("Object Refs")]

    [Header("Weapon Art")]
    public Sprite weaponProjectileSprite;
    public AudioClip weaponFireSound;

    [Header("Weapon Damage Stats")]
    // How much damage the projectile deals when it hits a target
    public float weaponDamage = 1f;
    // What type that damage is 
    public enumDamageType weaponDamageType;
    // How much delay there is between each shot
    public float weaponFireRateDelay = 0.5f;
    // How far the projectile moves before beining destroyed
    public float weaponRange = 10f;
    // How much the damage should be reduced to in a linear fashion (As a percent of starting damge) as the projectile travels across its range (Starting damage that grows if weaponIsRangeFalloffDamageInReverse is true)
    public float weaponRangeDamageFalloffMinPercent = 0.30f;
    
    [Header("Weapon Damage Features")]
    // Wether or not the weapon loses or gains damage as the projectile moves through its range
    public bool weaponIsRangeFalloffDamageInReverse = false;
    

    [Header("Weapon Accuracy Stats")]
    // The starting accuracy of the weapon as a cone
    public float weaponSpreadStart = 0.01f;
    // The maximum ammount of inaccuracy the weapon can reach from recoil
    public float weaponSpreadMax = 0.10f;
    // How much the current spread is increased by with each shot
    public float weaponSpreadRecoilImpulse = 0.02f;
    // How much accuracy is restored each cycle
    public float weaponSpreadRecoilRecoveryAmount = 0.01f;
    // How much time delay between each accuracy recovery cycle
    public float weaponSpreadRecoilRecoveryDelay = 0.01f;
    
    [Header("Weapon Accuracy Features")]
    // Wether or not the weapon gets more accurate with each shot (starting at max spread and improving to Spread Start)
    public bool weaponIsReverseRecoilImpulse = false;

    [Header("Weapon Ammo Stats")]
    // Wether or not the weapon requires ammo to fire
    public bool weaponUsesAmmo = true;
    // The inventory Item being used as Ammo for this weapon (For example OB_Item_Ammo_Pistol)
    public F_Item weaponAmmoType;
    // How much ammo this weapon can hold before it needs to reload
    public int weaponAmmoCapacity = 5;
    // How much ammo is used per shot (The gun can't fire if there isn't enough loaded)
    public int weaponAmmoTakenPerShot = 1;
    // How many seconds this weapon takes to perform a reload cycle
    public float weaponReloadDelay = 3.00f;

    [Header("Weapon Ammo Features")]
    // Wether or not the weapon reloads its entire magazine per reload cycle or just one at a time in a loop (Like loading a shotgun with shells)
    public bool weaponIsReloadOneBulletPerReload = false;

    
    [Header("Weapon Flags")]


    [Header("Constants Private")]


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
