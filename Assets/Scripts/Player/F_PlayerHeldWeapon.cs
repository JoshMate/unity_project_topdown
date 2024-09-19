using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class F_PlayerHeldWeapon : MonoBehaviour
{

    [Header("Art")]
    public Sprite weaponSprite;

    [Header("Object Refs")]
    public GameObject projectileObject;
    public GameObject projectileImpactObject;
    public F_playerHeldWeaponIndividual heldWeaponCurrentlySelected;
    public F_playerHeldWeaponIndividual heldWeaponGunBig;
    public F_playerHeldWeaponIndividual heldWeaponGunSmall;
    public F_playerHeldWeaponIndividual heldWeaponMeleeBig;
    public F_playerHeldWeaponIndividual heldWeaponMeleeSmall;
    

    public float projectileSpeed;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        UpdateHeldWeaponSprite();
    }

    public void FireWeapon()
    {
        GameObject projecile = Instantiate(projectileObject,heldWeaponCurrentlySelected.firePosition.position, heldWeaponCurrentlySelected.firePosition.rotation);
        projecile.GetComponent<Rigidbody2D>().AddForce(heldWeaponCurrentlySelected.firePosition.right * projectileSpeed, ForceMode2D.Impulse);
        projecile.GetComponent<F_Effects_Projectile>().projectileImpactObject = projectileImpactObject;
    }

    public void UpdateHeldWeaponSprite()
    {
        heldWeaponCurrentlySelected = heldWeaponGunBig;

        heldWeaponGunBig.spriteRenderer.enabled = false;
        heldWeaponGunSmall.spriteRenderer.enabled = false;
        heldWeaponMeleeBig.spriteRenderer.enabled = false;
        heldWeaponMeleeSmall.spriteRenderer.enabled = false;
        heldWeaponCurrentlySelected.spriteRenderer.enabled = true;
    }
}
