using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class F_Ent : MonoBehaviour
{
    [Header("Ent Information")]
    // Current health points when this reaches zero the entity should run a on death method 
    public string entInfoName = "Entity Name";
    // Current health points when this reaches zero the entity should run a on death method 
    public string entInfoDescription = "Entity Description";
    // What materuial this ent is made out of, which will affect hit sounds, and blood colour based on material
    public enumMaterialType entFeatureMaterial;

    [Header("Ent Health Stats")]
    // When enabled, this ent uses its prefab health values instead of its material health settings.
    public bool entFeatureOverrideMaterialHealth = false;
    // Current health points when this reaches zero the entity should run a on death method 
    public float entStatHealth = 100f;
    // Max health points, current health can't go above this
    public float entStatHealthMax = 100f;
    // How much toxic damage this ent has taken
    public float entStatToxic = 0f;
    // How much toxic damage this ent can take
    public float entStatToxicMax = 1000f;

    [Header("Ent Resistance Stats")]
    // When enabled, this ent uses its prefab resistance values instead of its material resistance settings.
    public bool entFeatureOverrideResistances = false;
    // How much to reduce incoming damage of this type by 1.0 fully immune. 0.0 takes full damge.
    [Range(entResistanceMin, entResistanceMax)] public float entResistanceMelee = 0.0f;
    // How much to reduce incoming damage of this type by 1.0 fully immune. 0.0 takes full damge.
    [Range(entResistanceMin, entResistanceMax)] public float entResistanceBullet = 0.0f;
    // How much to reduce incoming damage of this type by 1.0 fully immune. 0.0 takes full damge.
    [Range(entResistanceMin, entResistanceMax)] public float entResistanceEnergy = 0.0f;
    // How much to reduce incoming damage of this type by 1.0 fully immune. 0.0 takes full damge.
    [Range(entResistanceMin, entResistanceMax)] public float entResistanceFire = 0.0f;
    // How much to reduce incoming damage of this type by 1.0 fully immune. 0.0 takes full damge.
    [Range(entResistanceMin, entResistanceMax)] public float entResistanceExplosive = 0.0f;
    // How much to reduce incoming damage of this type by 1.0 fully immune. 0.0 takes full damge.
    [Range(entResistanceMin, entResistanceMax)] public float entResistanceToxic = 0.0f;
    

    [Header("Ent Features")]
    // Whether or not this ent blocks npcs vision (reserved for NPC vision)
    public bool entFeatureCanSeeThrough = false;
    // Whether or not movers (player, npcs) pass freely through this ent
    public bool entFeatureCanWalkThrough = false;
    // Whether or not projectiles and bullets pass freely over this ent
    public bool entFeatureCanShootOver = false;
    // Whether this ent's sprites shatter into pieces when it dies
    public bool entFeatureShatterOnDeath = true;
    // Whether this ent emits blood particles when hit
    public bool entFeatureCreateBloodOnHit = true;
    // Whether this ent creates a decal when it dies
    public bool entFeatureCreateDecalOnDestroy = true;


    // Ent material config: assigned at runtime from the global settings for entFeatureMaterial, so it is neither serialised nor shown in the Inspector.
    // One clip is picked at random when this ent gets hit
    [NonSerialized] public AudioClip[] entMaterialHitSounds;
    // One clip is picked at random when this ent dies
    [NonSerialized] public AudioClip[] entMaterialDestroySounds;
    // Colour of on-hit particle effects
    [NonSerialized] public Color entMaterialBloodColour = Color.white;
    // Colour of the decal left on the floor when this ent dies
    [NonSerialized] public Color entMaterialDecalColour = Color.white;

    [Header("Constants Private")]
    private const float entHealthFloor = 0f;
    private const float entResistanceMin = 0f;
    private const float entResistanceMax = 1f;
    private const float entSoundVolume = 100f;
    // How far hearing systems hear this ent being hit (bullet and melee impacts) and being destroyed
    private const EnumSoundLoudness entSoundHitLoudness = EnumSoundLoudness.EffectImpactSmall;
    private const EnumSoundLoudness entSoundDestroyLoudness = EnumSoundLoudness.EffectImpactMedium;
    private const float entKnockbackMinimumDirectionSqr = 0.0001f;

    [Header("Privates")]
    // Kept hidden so existing prefabs retain the previous override-both behaviour after the split.
    [SerializeField, HideInInspector] private bool entFeatureOverrideMaterialStats = false;
    private bool entIsDead = false;
    // Knockback speed added on top of a mover's own movement; fades over time
    private Vector2 entKnockbackVelocity;
    private Rigidbody2D entRigidbody;

    // Events (Header attributes are not valid on events)
    // Fired after damage is applied: ent, final damage, damage type
    public event Action<F_Ent, float, enumDamageType> OnDamaged;
    // Fired after healing is applied: ent, amount restored
    public event Action<F_Ent, float> OnHealed;
    // Fired once when health reaches zero
    public event Action<F_Ent> OnDied;

    // Fired for every ent after damage is applied: ent, final damage, damage type, source (may be null)
    public static event Action<F_Ent, float, enumDamageType, GameObject> OnAnyEntDamaged;
    // Fired for every ent after healing is applied: ent, amount restored, source (may be null)
    public static event Action<F_Ent, float, GameObject> OnAnyEntHealed;
    // Fired for every ent once when its health reaches zero, before it is destroyed
    public static event Action<F_Ent> OnAnyEntDied;

    /// <summary>Whether this ent has died.</summary>
    public bool IsDead => entIsDead;

    /// <summary>Knockback speed that movers add to their own movement velocity each physics step.</summary>
    public Vector2 KnockbackVelocity => entKnockbackVelocity;

    /// <summary>Whether this ent moves around (player, NPCs), so it is ignored when building the NavMesh.</summary>
    public bool IsNavigationMover => IsMover;

    /// <summary>Whether this ent carves a hole in the NavMesh: it is solid to movers and is not a mover itself.</summary>
    public bool BlocksNavigation => !entFeatureCanWalkThrough && !IsMover;

    protected virtual void Awake()
    {
        entRigidbody = GetComponent<Rigidbody2D>();

        // Preserve the old combined setting on existing prefab and scene data.
        if (entFeatureOverrideMaterialStats)
        {
            entFeatureOverrideMaterialHealth = true;
            entFeatureOverrideResistances = true;
        }

        if (!entFeatureOverrideMaterialHealth)
        {
            ApplyMaterialHealthStats();
        }
        if (!entFeatureOverrideResistances)
        {
            ApplyMaterialResistanceStats();
        }

        entStatHealth = Mathf.Clamp(entStatHealth, entHealthFloor, entStatHealthMax);
        ApplyMaterialConfig();
        if (IsMover)
        {
            F_Utility_Helper_Physics.AssignPhysicsLayer(gameObject, F_Utility_Config_Physics.cfgPhysicsLayerMover);
        }
        ApplyPassThroughFeatures();
        if (GetComponent<F_Ent_HealthBar>() == null)
        {
            gameObject.AddComponent<F_Ent_HealthBar>();
        }
    }

    /// <summary>Copies the health maximum configured for this ent's material, and starts it at full health.</summary>
    public void ApplyMaterialHealthStats()
    {
        F_Ent_MaterialSettings settings = F_Logic_Globals.GetEntMaterialSettings(entFeatureMaterial);
        if (settings == null)
        {
            return;
        }

        entStatHealthMax = settings.materialHealthMax;
        entStatHealth = settings.materialHealthMax;
    }

    /// <summary>Copies the resistances configured for this ent's material.</summary>
    public void ApplyMaterialResistanceStats()
    {
        F_Ent_MaterialSettings settings = F_Logic_Globals.GetEntMaterialSettings(entFeatureMaterial);
        if (settings == null)
        {
            return;
        }

        entResistanceMelee = settings.materialResistanceMelee;
        entResistanceBullet = settings.materialResistanceBullet;
        entResistanceEnergy = settings.materialResistanceEnergy;
        entResistanceFire = settings.materialResistanceFire;
        entResistanceExplosive = settings.materialResistanceExplosive;
        entResistanceToxic = settings.materialResistanceToxic;
    }

    /// <summary>Copies the health maximum and resistances configured for this ent's material.</summary>
    public void ApplyMaterialStats()
    {
        ApplyMaterialHealthStats();
        ApplyMaterialResistanceStats();
    }

    /// <summary>Copies the hit sound, death sound and blood colour configured for this ent's material.</summary>
    public void ApplyMaterialConfig()
    {
        F_Ent_MaterialSettings settings = F_Logic_Globals.GetEntMaterialSettings(entFeatureMaterial);
        if (settings == null)
        {
            return;
        }

        entMaterialHitSounds = settings.materialHitSounds;
        entMaterialDestroySounds = settings.materialDestroySounds;
        entMaterialBloodColour = settings.materialBloodColour;
        entMaterialDecalColour = settings.materialDecalColour;
    }

    /// <summary>Applies the walk-through and shoot-through features to this ent's colliders and NavMesh modifiers. Call again after changing either flag at runtime.</summary>
    public void ApplyPassThroughFeatures()
    {
        F_Utility_Helper_Physics.ApplyEntPassThroughFeatures(this);
        F_Utility_Helper_Navigation.ApplyEntNavigationFeatures(this);
    }

    // Whether this ent walks or slides around and so passes through walk-through ents; override to true for the player and npcs.
    protected virtual bool IsMover => false;

    // Plays a random non-empty clip from the array at this ent, credited to whoever caused it, falling back when no clips are configured.
    private void PlayEntSound(AudioClip[] sounds, EnumSoundLoudness soundLoudness, GameObject soundOwner, AudioClip[] fallbackSounds = null)
    {
        if (sounds == null || sounds.Length == 0)
        {
            sounds = fallbackSounds;
        }

        if (sounds == null || sounds.Length == 0)
        {
            return;
        }

        AudioClip sound = sounds[UnityEngine.Random.Range(0, sounds.Length)];
        if (sound != null)
        {
            F_Logic_Audio.PlaySound(sound, EnumSoundType.Spatial, soundLoudness, entSoundVolume, transform.position, soundOwner);
        }
    }

    // Start is called before the first frame update
    protected virtual void Start()
    {
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        DecayKnockback();
    }

    /// <summary>Pushes this ent away in a direction. Movers receive a fading knockback speed; other dynamic rigidbodies receive an impulse.</summary>
    /// <param name="knockbackDirection">Direction to push in; it does not need to be normalised.</param>
    /// <param name="knockbackAmount">Starting knockback speed in world units per second.</param>
    public void ApplyKnockback(Vector2 knockbackDirection, float knockbackAmount)
    {
        if (entIsDead || knockbackAmount <= 0f || knockbackDirection.sqrMagnitude <= entKnockbackMinimumDirectionSqr)
        {
            return;
        }

        Vector2 knockbackVelocity = knockbackDirection.normalized * knockbackAmount;
        if (IsMover)
        {
            entKnockbackVelocity += knockbackVelocity;
            return;
        }

        if (entRigidbody != null && entRigidbody.bodyType == RigidbodyType2D.Dynamic)
        {
            entRigidbody.AddForce(knockbackVelocity * entRigidbody.mass, ForceMode2D.Impulse);
        }
    }

    // Fades the mover knockback speed back to zero at the configured rate.
    private void DecayKnockback()
    {
        entKnockbackVelocity = Vector2.MoveTowards(entKnockbackVelocity, Vector2.zero, F_Utility_Config_Physics.cfgPhysicsKnockbackDecayRate * Time.deltaTime);
    }

    /// <summary>Applies damage after resistance. Toxic damage fills the toxic stat instead of reducing health.</summary>
    /// <param name="rawDamage">Damage before resistance.</param>
    /// <param name="damageType">The damage type.</param>
    /// <param name="damageSource">The object that caused the damage, if known.</param>
    /// <returns>The final damage applied.</returns>
    public float TakeDamage(float rawDamage, enumDamageType damageType, GameObject damageSource = null)
    {
        if (entIsDead)
        {
            return 0f;
        }

        float finalDamage = F_Utility_Helper_Damage.CalculateFinalDamage(this, rawDamage, damageType);
        if (finalDamage <= 0f)
        {
            return 0f;
        }

        if (damageType == enumDamageType.Toxic)
        {
            entStatToxic = Mathf.Clamp(entStatToxic + finalDamage, entHealthFloor, entStatToxicMax);
            OnDamaged?.Invoke(this, finalDamage, damageType);
            OnAnyEntDamaged?.Invoke(this, finalDamage, damageType, damageSource);
            PlayEntSound(entMaterialHitSounds, entSoundHitLoudness, damageSource);
            return finalDamage;
        }

        entStatHealth = Mathf.Max(entHealthFloor, entStatHealth - finalDamage);
        OnDamaged?.Invoke(this, finalDamage, damageType);
        OnAnyEntDamaged?.Invoke(this, finalDamage, damageType, damageSource);

        if (entStatHealth <= entHealthFloor)
        {
            Die(damageSource);
        }
        else
        {
            PlayEntSound(entMaterialHitSounds, entSoundHitLoudness, damageSource);
        }

        return finalDamage;
    }

    /// <summary>Restores health up to the maximum. Ignored when dead.</summary>
    /// <param name="amount">Health to restore.</param>
    /// <param name="healSource">The object that caused the healing, if known.</param>
    /// <returns>The amount actually restored.</returns>
    public float Heal(float amount, GameObject healSource = null)
    {
        if (entIsDead || amount <= 0f)
        {
            return 0f;
        }

        float previousHealth = entStatHealth;
        entStatHealth = Mathf.Min(entStatHealthMax, entStatHealth + amount);
        float restored = entStatHealth - previousHealth;
        if (restored > 0f)
        {
            OnHealed?.Invoke(this, restored);
            OnAnyEntHealed?.Invoke(this, restored, healSource);
        }

        return restored;
    }

    /// <summary>Reduces the toxic stat, clamped to zero.</summary>
    /// <param name="amount">Toxic to remove.</param>
    /// <returns>The amount actually removed.</returns>
    public float ReduceToxic(float amount)
    {
        if (amount <= 0f)
        {
            return 0f;
        }

        float previousToxic = entStatToxic;
        entStatToxic = Mathf.Max(entHealthFloor, entStatToxic - amount);
        return previousToxic - entStatToxic;
    }

    // Kills this ent; the killer is credited with the destroy sound.
    private void Die(GameObject killSource)
    {
        entIsDead = true;
        OnDied?.Invoke(this);
        OnAnyEntDied?.Invoke(this);
        PlayEntSound(entMaterialDestroySounds, entSoundDestroyLoudness, killSource, entMaterialHitSounds);
        if (entFeatureCreateDecalOnDestroy)
        {
            CreateDestroyDecal();
        }
        if (entFeatureShatterOnDeath)
        {
            ShatterSprites();
        }
        OnDeath();
        if (DestroyOnDeath)
        {
            Destroy(gameObject);
        }
        
    }

    // Whether the whole ent object is destroyed on death; override to false for objects that must persist, such as the player.
    protected virtual bool DestroyOnDeath => true;

    // Draws a decal in the material's decal colour on the floor decal layer, centred under the ent's visual centre.
    private void CreateDestroyDecal()
    {
        Bounds entBounds = F_Utility_Helper_Decal.GetSpriteBounds(gameObject);
        float largestExtent = Mathf.Max(entBounds.size.x, entBounds.size.y);
        float decalDiameter = Mathf.Max(F_Utility_Config_Decal.cfgDecalSizeMin, largestExtent * F_Utility_Config_Decal.cfgDecalSizeMultiplier);
        F_Utility_Helper_Decal.CreateFloorDecal(entBounds.center, entMaterialDecalColour, decalDiameter);
    }

    // Removes all collision from this ent and its children so a dead ent no longer blocks movement or projectiles.
    private void DisableColliders()
    {
        foreach (Collider2D entCollider in GetComponentsInChildren<Collider2D>())
        {
            entCollider.enabled = false;
        }
    }

    // Shatters every visible sprite on this ent and hides the originals; the pieces are independent objects.
    private void ShatterSprites()
    {
        foreach (SpriteRenderer spriteRenderer in GetComponentsInChildren<SpriteRenderer>())
        {
            if (spriteRenderer.enabled && spriteRenderer.sprite != null)
            {
                F_Utility_Helper_Shatter.ShatterSprite(spriteRenderer);
                spriteRenderer.enabled = false;
            }
        }
    }

    // Override to add death behaviour such as disabling input or destroying the object
    protected virtual void OnDeath()
    {

    }
}
