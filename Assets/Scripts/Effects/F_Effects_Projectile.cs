using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class F_Effects_Projectile : MonoBehaviour
{

    public Rigidbody2D rb;

    public GameObject projectileImpactObject;

    public Color bloodColour;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // Look at cursor location
        Vector2 aimDirection = rb.velocity;
        float aimAngle = Mathf.Atan2(aimDirection.y,aimDirection.x) * Mathf.Rad2Deg;
        rb.rotation = aimAngle;
  
    }

    void OnCollisionEnter2D(Collision2D other) 
    {
        switch(other.gameObject.tag)
        {
            case F_Logic_Globals.tagEnt:
                CreateProjectileImpact(other);
                Destroy(gameObject);
            break;
        }
    }

    void CreateProjectileImpact(Collision2D other) {

        // Work out the angel to make the particles shoot back towards the shooter
        Vector2 dir = other.relativeVelocity.normalized;
        Quaternion newRotation = Quaternion.LookRotation(dir, Vector2.right);
        GameObject projecileImpact = Instantiate(projectileImpactObject,this.transform.position, newRotation);
        projecileImpact.GetComponent<F_Effects_Projectile_Impact>().bloodColour = other.gameObject.GetComponent<F_Ent>().bloodColour;
    }
}
