using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class F_PlayerStats : MonoBehaviour
{
    [Header("Object Refs")]
    public F_GUI_HUD_Manager hudManager;
    public F_GUI_CharacterScreen_Manager characterScreenManager;

    [Header("Health Stats")]
    public float health;
    public float healthMax;
    

    [Header("Stamina Stats")]
    public float stamina;
    public float staminaMax; 
    public float staminaRegenDelay;
    public float staminaSprintDrainDelay;

    [Header("Survival Stats")]
    public float hunger;       
    public float hungerMax; 
    public float hungerDrainRateDelay;
    public float thirst; 
    public float thirstMax; 
    public float thirstDrainRateDelay;
    public float toxic; 
    public float toxicMax;

    [Header("Movement Stats")]
    public float speedMove;
    public float speedSprint;

    [Header("Private Checks")]
    public bool isSprinting = false;
    private float lastStaminaUseTime = 0f;
    private float lastStaminaRegenTime = 0f;
    private float staminaRegenStartDelay = 2f;
    private float lastStaminaSprintDrainTime = 0f;
    private float lastHungerDrainTime = 0f;
    private float lastThirstDrainTime = 0f;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    void Update()
    {
        HUDUpdate();
        FoodHungerUpdate();
        StaminaUpdate();
    }

    void FixedUpdate()
    {
        
    }

    public void SprintStart()
    {
        isSprinting = true;
    }

    public void SprintEnd()
    {
        lastStaminaUseTime = Time.time;
        isSprinting = false;
    }

    // Update HUD
    private void HUDUpdate()
    {
        hudManager.healthBar.barText.text = Math.Ceiling(health).ToString(); 
        hudManager.healthBar.barSlider.value = health;
        hudManager.healthBar.barSlider.maxValue = healthMax;

        hudManager.staminaBar.barText.text = Math.Ceiling(stamina).ToString();
        hudManager.staminaBar.barSlider.value = stamina;
        hudManager.staminaBar.barSlider.maxValue = staminaMax;

        hudManager.hungerBar.barText.text = Math.Ceiling(hunger).ToString();
        hudManager.hungerBar.barSlider.value = hunger;
        hudManager.hungerBar.barSlider.maxValue = hungerMax;

        hudManager.thirstBar.barText.text = Math.Ceiling(thirst).ToString();
        hudManager.thirstBar.barSlider.value = thirst;
        hudManager.thirstBar.barSlider.maxValue = thirstMax;

        hudManager.toxicBar.barText.text = Math.Ceiling(toxic).ToString();
        hudManager.toxicBar.barSlider.value = toxic;
        hudManager.toxicBar.barSlider.maxValue = toxicMax;
    }

    // Handle Stamina Regeneration
    private void StaminaUpdate()
    {
        if (isSprinting == false && Time.time > lastStaminaUseTime + staminaRegenStartDelay)
        {
            if (Time.time > lastStaminaRegenTime + staminaRegenDelay)
            {
                lastStaminaRegenTime = Time.time;
                stamina += 1;
                stamina = Math.Clamp(stamina,0,staminaMax);
            }
        }
        if (isSprinting == true && stamina > 0)
        {
            if (Time.time > lastStaminaSprintDrainTime + staminaSprintDrainDelay)
            {
                lastStaminaSprintDrainTime = Time.time;
                stamina -= 1;
                stamina = Math.Clamp(stamina,0,staminaMax);
            }
        }
    }

    // Handle Food and Water Drain

    private void FoodHungerUpdate()
    {
        // Hunger
        // Hunger Drains faster if Health is low
        float hungerDrainRateDelayFinal = hungerDrainRateDelay;
        if (health <= (healthMax / 2))
        {
            hungerDrainRateDelayFinal = hungerDrainRateDelay/2;
        }
        if (Time.time > lastHungerDrainTime + hungerDrainRateDelayFinal)
        {
            lastHungerDrainTime = Time.time;
            hunger -= 1;
            hunger = Math.Clamp(hunger,0,hungerMax);
        }

        // Thirst
        // Thirst Drains faster if Stamina is below half
        float thirstDrainRateDelayFinal = thirstDrainRateDelay;
        if (stamina <= (staminaMax / 2))
        {
            thirstDrainRateDelayFinal = thirstDrainRateDelay/2;
        }
        if (Time.time > lastThirstDrainTime + thirstDrainRateDelayFinal)
        {
            lastThirstDrainTime = Time.time;
            thirst -= 1;
            thirst = Math.Clamp(thirst,0,thirstMax);
        }
        
    }
}
