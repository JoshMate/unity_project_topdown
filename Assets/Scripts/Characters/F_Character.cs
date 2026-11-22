using System;
using UnityEngine;

/// <summary>
/// Generic base for all characters (player, NPCs, enemies). Adds stamina, movement speeds, sprinting
/// and material-specific footsteps on top of the shared ent stats.
/// </summary>
public class F_Character : F_Ent
{

    [Header("Character Stamina Stats")]
    // Current stamina, drained while sprinting and regenerated while resting.
    public float characterStatStamina;
    // Max stamina, current stamina can't go above this.
    public float characterStatStaminaMax;
    // Seconds between each stamina point regenerated.
    public float characterStatStaminaRegenDelay;
    // Seconds between each stamina point drained while sprinting.
    public float characterStatStaminaSprintDrainDelay;

    [Header("Character Movement Stats")]
    // Normal movement speed.
    public float characterStatSpeedMove;
    // Movement speed while sprinting with stamina remaining.
    public float characterStatSpeedSprint;

    [Header("Character Features")]
    // Whether this character plays footstep sounds while moving; turn off for silent NPCs.
    public bool characterFeatureFootstepsEnabled = true;

    [Header("Character Status Flags")]
    // Whether this character is currently trying to sprint.
    public bool characterStatusIsSprinting = false;

    [Header("Constants Private")]
    private const float characterStaminaRegenStartDelay = 2f;
    private const float characterStaminaStepAmount = 1f;
    private const float characterStatFloor = 0f;
    private const float characterFootstepMaxSpeed = 7f;
    private const float characterFootstepMinimumMaxSpeed = 0.01f;
    private const float characterFootstepMaxSoundsPerSecond = 5f;
    private const float characterFootstepSneakLoudnessMaxSpeedRatio = 0.33f;
    private const float characterFootstepWalkLoudnessMaxSpeedRatio = 0.66f;
    private const float characterFootstepMaxVolume = 100f;

    [Header("Privates")]
    private float characterLastStaminaUseTime = 0f;
    private float characterLastStaminaRegenTime = 0f;
    private float characterLastStaminaSprintDrainTime = 0f;
    private Rigidbody2D characterMovementRigidbody;
    private Vector2 characterFootstepLastPosition;
    private float characterFootstepRateAccumulator;

    /// <summary>Whether this character has stamina left to sprint with.</summary>
    public bool CanSprint => characterStatStamina > characterStatFloor;

    // Characters walk around, so they pass through walk-through ents.
    protected override bool IsMover => true;

    protected override void Awake()
    {
        base.Awake();
        characterMovementRigidbody = GetComponent<Rigidbody2D>();
        characterFootstepLastPosition = transform.position;
    }

    protected virtual void OnEnable()
    {
        characterFootstepLastPosition = transform.position;
        characterFootstepRateAccumulator = 0f;
    }

    protected override void Update()
    {
        base.Update();
        StaminaUpdate();
        FootstepsUpdate();
    }

    /// <summary>Starts sprinting; stamina drains while sprinting.</summary>
    public void SprintStart()
    {
        characterStatusIsSprinting = true;
    }

    /// <summary>Stops sprinting and stamps the time so stamina regen waits before starting.</summary>
    public void SprintEnd()
    {
        characterLastStaminaUseTime = Time.time;
        characterStatusIsSprinting = false;
    }

    /// <summary>Returns the sprint speed while sprinting with stamina remaining, otherwise the normal move speed.</summary>
    /// <returns>The speed this character should currently move at.</returns>
    public float GetCurrentMoveSpeed()
    {
        return characterStatusIsSprinting && CanSprint ? characterStatSpeedSprint : characterStatSpeedMove;
    }

    // Handles stamina regeneration while resting and drain while sprinting.
    private void StaminaUpdate()
    {
        if (!characterStatusIsSprinting && Time.time > characterLastStaminaUseTime + characterStaminaRegenStartDelay)
        {
            if (Time.time > characterLastStaminaRegenTime + characterStatStaminaRegenDelay)
            {
                characterLastStaminaRegenTime = Time.time;
                characterStatStamina = Math.Clamp(characterStatStamina + characterStaminaStepAmount, characterStatFloor, characterStatStaminaMax);
            }
        }
        if (characterStatusIsSprinting && CanSprint)
        {
            if (Time.time > characterLastStaminaSprintDrainTime + characterStatStaminaSprintDrainDelay)
            {
                characterLastStaminaSprintDrainTime = Time.time;
                characterStatStamina = Math.Clamp(characterStatStamina - characterStaminaStepAmount, characterStatFloor, characterStatStaminaMax);
            }
        }
    }

    // Movement speed directly controls both the footstep rate and playback volume, up to the max speed.
    private void FootstepsUpdate()
    {
        float deltaTime = Time.deltaTime;
        if (!characterFeatureFootstepsEnabled || IsDead || deltaTime <= 0f || characterFootstepMaxSpeed < characterFootstepMinimumMaxSpeed)
        {
            characterFootstepLastPosition = transform.position;
            characterFootstepRateAccumulator = 0f;
            return;
        }

        Vector2 currentPosition = transform.position;
        float measuredSpeed = Vector2.Distance(currentPosition, characterFootstepLastPosition) / deltaTime;
        characterFootstepLastPosition = currentPosition;

        float currentSpeed = characterMovementRigidbody != null
            ? characterMovementRigidbody.linearVelocity.magnitude
            : measuredSpeed;
        if (currentSpeed <= 0f)
        {
            characterFootstepRateAccumulator = 0f;
            return;
        }

        float cappedSpeed = Mathf.Min(currentSpeed, characterFootstepMaxSpeed);
        float speedRatio = cappedSpeed / characterFootstepMaxSpeed;
        float soundsPerSecond = speedRatio * characterFootstepMaxSoundsPerSecond * F_Utility_Config_Footsteps.cfgFootstepFrequencyCoefficient;
        characterFootstepRateAccumulator += soundsPerSecond * deltaTime;
        while (characterFootstepRateAccumulator >= 1f)
        {
            characterFootstepRateAccumulator -= 1f;
            PlayFootstep(speedRatio);
        }
    }

    // Selects the loudness enum from speed bands and scales playback volume linearly with speed.
    private void PlayFootstep(float speedRatio)
    {
        EnumSoundLoudness loudness = speedRatio <= characterFootstepSneakLoudnessMaxSpeedRatio
            ? EnumSoundLoudness.FootstepSneak
            : speedRatio <= characterFootstepWalkLoudnessMaxSpeedRatio
                ? EnumSoundLoudness.FootstepWalk
                : EnumSoundLoudness.FootstepRun;
        float volume = speedRatio * characterFootstepMaxVolume;

        if (!F_Utility_Helper_Ground.TryGetGroundMaterial(transform.position, out enumMaterialType groundMaterial))
        {
            return;
        }

        F_Ent_MaterialSettings materialSettings = F_Logic_Globals.GetEntMaterialSettings(groundMaterial);
        AudioClip[] footstepSounds = materialSettings?.materialFootStepSounds;
        if (footstepSounds == null || footstepSounds.Length == 0)
        {
            return;
        }

        AudioClip footstepClip = footstepSounds[UnityEngine.Random.Range(0, footstepSounds.Length)];
        F_Logic_Audio.PlaySound(footstepClip, EnumSoundType.Spatial, loudness, volume, transform.position);
    }
}
