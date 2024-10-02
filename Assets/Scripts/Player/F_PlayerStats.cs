using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class F_PlayerStats : MonoBehaviour
{
    [Header("Object Refs")]
    public F_GUI_HUD_Manager hudManager;

    [Header("Health Stats")]
    public float health;
    public float healthMax;
    

    [Header("Stamina Stats")]
    public float stamina;
    public float staminaMax; 
    public float staminaRegenRate;
    public float staminaSprintDrainRate;

    [Header("Survival Stats")]
    public float hunger;       
    public float hungerMax; 
    public float thirst; 
    public float thirstMax; 
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
    private float staminaRegenDelay = 0.5f;
    private float lastStaminaSprintDrainTime = 0f;
    private float staminaSprintDrainDelay = 0.5f;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    void Update()
    {
        HUDUpdate();
    }

    void FixedUpdate()
    {
        StaminaUpdate();
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
        hudManager.healthBar.value = health;
        hudManager.healthBar.maxValue = healthMax;

        hudManager.staminaBar.value = stamina;
        hudManager.staminaBar.maxValue = staminaMax;

        hudManager.hungerBar.value = hunger;
        hudManager.hungerBar.maxValue = hungerMax;

        hudManager.thirstBar.value = thirst;
        hudManager.thirstBar.maxValue = thirstMax;

        hudManager.toxicBar.value = toxic;
        hudManager.toxicBar.maxValue = toxicMax;
    }

    // Handle Stamina Regeneration
    private void StaminaUpdate()
    {
        if (isSprinting == false && Time.time > lastStaminaUseTime + staminaRegenStartDelay)
        {
            if (Time.time > lastStaminaRegenTime + staminaRegenDelay)
            {
                lastStaminaRegenTime = Time.time;
                stamina += staminaRegenRate;
                stamina = Math.Clamp(stamina,0,staminaMax);
            }
        }
        if (isSprinting == true && stamina > 0)
        {
            if (Time.time > lastStaminaSprintDrainTime + staminaSprintDrainDelay)
            {
                lastStaminaSprintDrainTime = Time.time;
                stamina -= staminaSprintDrainRate;
                stamina = Math.Clamp(stamina,0,staminaMax);
            }
        }
    }
}
