using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class F_Effects_Projectile : MonoBehaviour
{

    public Rigidbody2D rb;

    public GameObject projectileImpactObject;


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

    void OnTriggerEnter2D(Collider2D other)
    {
        switch(other.gameObject.tag)
        {
            case F_Logic_Globals.tagWorldWall:
                CreateProjectileImpact();
                Destroy(gameObject);
            break;
        }
    }

    void CreateProjectileImpact() {

        // Work out the angel to make the particles shoot back towards the shooter
        Quaternion newRotation = Quaternion.LookRotation(-this.rb.velocity, Vector2.right);

        GameObject projecileImpact = Instantiate(projectileImpactObject,this.transform.position, newRotation);
    }
}
