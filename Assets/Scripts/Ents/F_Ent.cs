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

    [Header("Ent Health Stats")]
    // Current health points when this reaches zero the entity should run a on death method 
    public float entStatHealth = 100f;
    // Max health points, current health can't go above this
    public float entStatHealthMax = 100f;
    // How much toxic damage this ent has taken
    public float entStatToxic = 0f;
    // How much toxic damage this ent can take
    public float entStatToxicMax = 1000f;

    [Header("Ent Resistance Stats")]
    // How much to reduce incoming damage of this type by 1.0 fully immune. 0.0 takes full damge.
    public float entResistanceMelee = 0.0f;
    // How much to reduce incoming damage of this type by 1.0 fully immune. 0.0 takes full damge.
    public float entResistanceBullet = 0.0f;
    // How much to reduce incoming damage of this type by 1.0 fully immune. 0.0 takes full damge.
    public float entResistanceEnergy = 0.0f;
    // How much to reduce incoming damage of this type by 1.0 fully immune. 0.0 takes full damge.
    public float entResistanceFire = 0.0f;
    // How much to reduce incoming damage of this type by 1.0 fully immune. 0.0 takes full damge.
    public float entResistanceExplosive = 0.0f;
    // How much to reduce incoming damage of this type by 1.0 fully immune. 0.0 takes full damge.
    public float entResistanceToxic = 0.0f;

    [Header("Ent Features")]
    // What materuial this ent is made out of, which will affect hit sounds, and blood colour based on material
    public enumMaterialType entFeatureMaterial;
    // Whether or not this ent blocks npcs vision
    public bool entFeatureCanSeeThrough = false;
    // Whether or not this ent can be moved through by the player or npcs
    public bool entFeatureCanWalkThrough = false;
    // Whether or not this bullets should collide with this ent or pass straight over
    public bool entFeatureCanShootThrough = false;
    // Whether this ent's sprites shatter into pieces when it dies
    public bool entFeatureShatterOnDeath = true;


    [Header("Ent Material Config (assigned from entFeatureMaterial)")]
    // One clip is picked at random when this ent gets hit
    public AudioClip[] entMaterialHitSounds;
    // One clip is picked at random when this ent dies
    public AudioClip[] entMaterialDeathSounds;
    // Colour of on-hit particle effects
    public Color entMaterialBloodColour = Color.white;

    [Header("Constants Private")]
    private const float entHealthFloor = 0f;
    private const float entSoundVolume = 100f;

    [Header("Privates")]
    private bool entIsDead = false;

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

    /// <summary>Whether this ent has died.</summary>
    public bool IsDead => entIsDead;

    protected virtual void Awake()
    {
        entStatHealth = Mathf.Clamp(entStatHealth, entHealthFloor, entStatHealthMax);
        ApplyMaterialConfig();
        if (GetComponent<F_Ent_HealthBar>() == null)
        {
            gameObject.AddComponent<F_Ent_HealthBar>();
        }
    }

    /// <summary>Copies the hit sound, death sound and blood colour configured for this ent's material.</summary>
    public void ApplyMaterialConfig()
    {
        F_Ent_MaterialSettings settings = F_Logic_Globals.GetEntMaterialSettings(entFeatureMaterial);
        if (settings == null)
        {
            entMaterialBloodColour = F_Utility_Helper_Damage.GetBloodColour(entFeatureMaterial);
            return;
        }

        entMaterialHitSounds = settings.materialHitSounds;
        entMaterialDeathSounds = settings.materialDeathSounds;
        entMaterialBloodColour = settings.materialBloodColour;
    }

    // Plays a random non-empty clip from the array, if any.
    private void PlayEntSound(AudioClip[] sounds)
    {
        if (sounds == null || sounds.Length == 0)
        {
            return;
        }

        AudioClip sound = sounds[UnityEngine.Random.Range(0, sounds.Length)];
        if (sound != null)
        {
            F_Logic_Audio.PlaySound(sound, EnumSoundType.Spatial, EnumSoundLoudness.NoSound, entSoundVolume, transform.position);
        }
    }

    // Start is called before the first frame update
    protected virtual void Start()
    {
    }

    // Update is called once per frame
    protected virtual void Update()
    {
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
            PlayEntSound(entMaterialHitSounds);
            return finalDamage;
        }

        entStatHealth = Mathf.Max(entHealthFloor, entStatHealth - finalDamage);
        OnDamaged?.Invoke(this, finalDamage, damageType);
        OnAnyEntDamaged?.Invoke(this, finalDamage, damageType, damageSource);

        if (entStatHealth <= entHealthFloor)
        {
            Die();
        }
        else
        {
            PlayEntSound(entMaterialHitSounds);
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

    private void Die()
    {
        entIsDead = true;
        OnDied?.Invoke(this);
        PlayEntSound(entMaterialDeathSounds);
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
