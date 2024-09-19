using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class F_playerHeldWeaponIndividual : MonoBehaviour
{
    [Header("Object Refs")]
    public Transform firePosition;
    public SpriteRenderer spriteRenderer;

    [Header("Art")]
    public Sprite sprite;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        spriteRenderer.sprite = sprite;
    }
}
