using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class F_PlayerHeldWeapon : MonoBehaviour
{

    [Header("Art")]
    public Sprite weaponSprite;
    public SpriteRenderer spriteRenderer;

    [Header("Object Refs")]
    public GameObject projectileObject;
    public GameObject projectileImpactObject;
    public Transform projectileFirePoint;

    public float pojecileSpeed;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        spriteRenderer.sprite = weaponSprite;
    }

    public void FireWeapon()
    {
        
       GameObject projecile = Instantiate(projectileObject,projectileFirePoint.position, projectileFirePoint.rotation);
       projecile.GetComponent<Rigidbody2D>().AddForce(projectileFirePoint.right * pojecileSpeed, ForceMode2D.Impulse);
       projecile.GetComponent<F_Effects_Projectile>().projectileImpactObject = projectileImpactObject;
    }
}
