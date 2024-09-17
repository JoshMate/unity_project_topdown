using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class F_Effects_Projectile_Impact : MonoBehaviour
{
    private ParticleSystem ps;

    // Automatically destroy the particle system once it has done its emission

    void Start () {
            ps = GetComponent<ParticleSystem>();
    }

    void Update () {
        if(ps)
        {
            if(!ps.IsAlive())
            {
                Destroy(gameObject);
            }
        }
    }


}
