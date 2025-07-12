using UnityEngine;

public class F_Item : MonoBehaviour
{
    [Header("Object Refs")]
    public SpriteRenderer itemSpriteRenderer;

    [Header("Item Information")]

    public string itemName = "<Item Name>";
    public string itemDescription = "<Item Description>";
    public float itemWeight = 0.0f;
    public int itemValue = 1;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
