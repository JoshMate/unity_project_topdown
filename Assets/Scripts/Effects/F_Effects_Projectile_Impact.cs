using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class F_Effects_Projectile_Impact : MonoBehaviour
{
    public Color bloodColour;

    private ParticleSystem ps;

    // Automatically destroy the particle system once it has done its emission

    void Start () {
        ParticleSystem ps = GetComponent<ParticleSystem>();
        var main = ps.main;
        main.startColor = bloodColour;
    }

    void Update () 
    {
        
    }


}
