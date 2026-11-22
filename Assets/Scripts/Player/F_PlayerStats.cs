using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// <summary>
/// Player-only stats on top of the shared character stats: HUD, hunger and thirst survival, and inventory slots and carry weight.
/// </summary>
public class F_PlayerStats : F_Character
{
    [Header("Constants Private")]
    private const int playerDefaultInventorySlotsAvailable = 9;
    private const float playerDefaultMaxCarryWeight = 50f;
    // Weight class thresholds, expressed as a fraction of playerStatWeightMax.
    private const float playerWeightThresholdLightWeight = 0.25f;
    private const float playerWeightThresholdMediumWeight = 0.50f;
    private const float playerWeightThresholdHeavyWeight = 0.75f;
    private const float playerWeightThresholdTooMuchWeight = 1.00f;
    // Survival drain tuning.
    private const float playerSurvivalStepAmount = 1f;
    private const float playerSurvivalLowStatRatio = 0.5f;
    private const float playerSurvivalFastDrainDivisor = 2f;
    private const float playerSurvivalStatFloor = 0f;

    [Header("Player Object Refs")]
    public F_GUI_HUD_Manager playerRefHudManager;
    public F_GUI_CharacterScreen_Manager playerRefCharacterScreenManager;

    [Header("Player Survival Stats")]
    public float playerStatHunger;
    public float playerStatHungerMax;
    public float playerStatHungerDrainRateDelay;
    public float playerStatThirst;
    public float playerStatThirstMax;
    public float playerStatThirstDrainRateDelay;

    [Header("Player Inventory Stats")]
    public float playerStatWeight;
    public float playerStatWeightMax = playerDefaultMaxCarryWeight;
    // Carry-weight classification, driven by weight as a percentage of playerStatWeightMax. Used for future gameplay effects on the player.
    public enumWeightClass playerStatWeightClass;
    public int playerStatInventorySlotsAvailable = playerDefaultInventorySlotsAvailable;
    public int playerStatInventorySlotsMax;

    [Header("Privates")]
    private F_PlayerInventory playerInventory;
    private float playerLastHungerDrainTime = 0f;
    private float playerLastThirstDrainTime = 0f;


    protected override void Awake()
    {
        base.Awake();
        playerInventory = GetComponent<F_PlayerInventory>();
        RefreshInventorySlotStats();
        if (playerStatWeightMax <= 0f)
        {
            playerStatWeightMax = playerDefaultMaxCarryWeight;
        }

        UpdateInventoryWeight();
    }

    /// <summary>Sets the number of inventory slots the player can currently use.</summary>
    /// <param name="availableSlotCount">The requested number of unlocked inventory slots.</param>
    public void SetInventorySlotsAvailable(int availableSlotCount)
    {
        playerStatInventorySlotsAvailable = availableSlotCount;
        RefreshInventorySlotStats();
    }

    /// <summary>Unlocks additional inventory slots without exceeding the configured maximum.</summary>
    /// <param name="slotsToUnlock">The number of additional slots to unlock.</param>
    public void UnlockInventorySlots(int slotsToUnlock)
    {
        if (slotsToUnlock <= 0)
        {
            return;
        }

        RefreshInventorySlotStats();
        int remainingSlots = playerStatInventorySlotsMax - playerStatInventorySlotsAvailable;
        playerStatInventorySlotsAvailable += slotsToUnlock >= remainingSlots ? remainingSlots : slotsToUnlock;
        RefreshInventorySlotStats();
    }

    private void RefreshInventorySlotStats()
    {
        if (playerInventory == null)
        {
            playerInventory = GetComponent<F_PlayerInventory>();
        }

        playerStatInventorySlotsMax = playerInventory != null && playerInventory.invSlotInventory != null
            ? playerInventory.invSlotInventory.Count
            : 0;
        playerStatInventorySlotsAvailable = Mathf.Clamp(playerStatInventorySlotsAvailable, 0, playerStatInventorySlotsMax);
        if (playerInventory != null)
        {
            playerInventory.ApplyInventorySlotAvailability(playerStatInventorySlotsAvailable);
        }
    }

    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
    }

    // The player persists after death to avoid breaking systems that reference it.
    protected override bool DestroyOnDeath => false;

    // Logs player death; death UI is out of scope for now.
    protected override void OnDeath()
    {
        Debug.Log("Player died.");
    }

    protected override void Update()
    {
        base.Update();
        UpdateInventoryWeight();
        UpdateWeightClass();
        HUDUpdate();
        FoodHungerUpdate();
    }

    private void UpdateInventoryWeight()
    {
        if (playerInventory != null)
        {
            playerStatWeight = playerInventory.CalculateCurrentWeight();
        }
    }

    // Recalculates playerStatWeightClass from the current weight-to-max-weight ratio.
    private void UpdateWeightClass()
    {
        float weightPercent = playerStatWeightMax > 0f ? playerStatWeight / playerStatWeightMax : 0f;

        if (weightPercent >= playerWeightThresholdTooMuchWeight)
        {
            playerStatWeightClass = enumWeightClass.TooMuchWeight;
        }
        else if (weightPercent >= playerWeightThresholdHeavyWeight)
        {
            playerStatWeightClass = enumWeightClass.HeavyWeight;
        }
        else if (weightPercent >= playerWeightThresholdMediumWeight)
        {
            playerStatWeightClass = enumWeightClass.MediumWeight;
        }
        else if (weightPercent >= playerWeightThresholdLightWeight)
        {
            playerStatWeightClass = enumWeightClass.LightWeight;
        }
        else
        {
            playerStatWeightClass = enumWeightClass.FreeWeight;
        }
    }

    // Update HUD
    private void HUDUpdate()
    {
        playerRefHudManager.healthBar.barText.text = Math.Ceiling(entStatHealth).ToString();
        playerRefHudManager.healthBar.barTextMax.text = Math.Ceiling(entStatHealthMax).ToString();
        playerRefHudManager.healthBar.barSlider.value = entStatHealth;
        playerRefHudManager.healthBar.barSlider.maxValue = entStatHealthMax;

        playerRefHudManager.staminaBar.barText.text = Math.Ceiling(characterStatStamina).ToString();
        playerRefHudManager.staminaBar.barTextMax.text = Math.Ceiling(characterStatStaminaMax).ToString();
        playerRefHudManager.staminaBar.barSlider.value = characterStatStamina;
        playerRefHudManager.staminaBar.barSlider.maxValue = characterStatStaminaMax;

        playerRefHudManager.hungerBar.barText.text = Math.Ceiling(playerStatHunger).ToString();
        playerRefHudManager.hungerBar.barTextMax.text = Math.Ceiling(playerStatHungerMax).ToString();
        playerRefHudManager.hungerBar.barSlider.value = playerStatHunger;
        playerRefHudManager.hungerBar.barSlider.maxValue = playerStatHungerMax;

        playerRefHudManager.thirstBar.barText.text = Math.Ceiling(playerStatThirst).ToString();
        playerRefHudManager.thirstBar.barTextMax.text = Math.Ceiling(playerStatThirstMax).ToString();
        playerRefHudManager.thirstBar.barSlider.value = playerStatThirst;
        playerRefHudManager.thirstBar.barSlider.maxValue = playerStatThirstMax;

        playerRefHudManager.toxicBar.barText.text = Math.Ceiling(entStatToxic).ToString();
        playerRefHudManager.toxicBar.barTextMax.text = Math.Ceiling(entStatToxicMax).ToString();
        playerRefHudManager.toxicBar.barSlider.value = entStatToxic;
        playerRefHudManager.toxicBar.barSlider.maxValue = entStatToxicMax;

        playerRefHudManager.weightBar.barText.text = playerStatWeight.ToString("0.##", CultureInfo.InvariantCulture);
        playerRefHudManager.weightBar.barTextMax.text = playerStatWeightMax.ToString("0.##", CultureInfo.InvariantCulture);
        playerRefHudManager.weightBar.barSlider.maxValue = playerStatWeightMax;
        playerRefHudManager.weightBar.barSlider.value = playerStatWeight;
        playerRefHudManager.weightBar.SetWeightClass(playerStatWeightClass);
    }

    // Handle Food and Water Drain
    private void FoodHungerUpdate()
    {
        // Hunger
        // Hunger Drains faster if Health is low
        float hungerDrainRateDelayFinal = playerStatHungerDrainRateDelay;
        if (entStatHealth <= entStatHealthMax * playerSurvivalLowStatRatio)
        {
            hungerDrainRateDelayFinal = playerStatHungerDrainRateDelay / playerSurvivalFastDrainDivisor;
        }
        if (Time.time > playerLastHungerDrainTime + hungerDrainRateDelayFinal)
        {
            playerLastHungerDrainTime = Time.time;
            playerStatHunger = Math.Clamp(playerStatHunger - playerSurvivalStepAmount, playerSurvivalStatFloor, playerStatHungerMax);
        }

        // Thirst
        // Thirst Drains faster if Stamina is below half
        float thirstDrainRateDelayFinal = playerStatThirstDrainRateDelay;
        if (characterStatStamina <= characterStatStaminaMax * playerSurvivalLowStatRatio)
        {
            thirstDrainRateDelayFinal = playerStatThirstDrainRateDelay / playerSurvivalFastDrainDivisor;
        }
        if (Time.time > playerLastThirstDrainTime + thirstDrainRateDelayFinal)
        {
            playerLastThirstDrainTime = Time.time;
            playerStatThirst = Math.Clamp(playerStatThirst - playerSurvivalStepAmount, playerSurvivalStatFloor, playerStatThirstMax);
        }
    }
}
