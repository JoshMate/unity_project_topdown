using UnityEngine;

public class F_Item : MonoBehaviour
{
    [Header("Object Refs")]
    public SpriteRenderer itemSpriteRenderer;

    [Header("Item Information")]

    public string itemName = "<Item Name>";
    public string itemDescription = "<Item Description>";
    public enumItemType itemType = enumItemType.MiscJunk;
    public enumItemRarity itemRarity = enumItemRarity.Rarity00Junk;
    public float itemWeight = 0.0f;
    public int itemValue = 1;

    [Header("Item Flags")]
    public bool isInInventorySlot = false;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // When an item is picked up or moved to a slot call this to move it "Out of the world" so it doesn't exist on the floor and in your inventory at the same time
    public void MoveItemToInventory()
    {
        isInInventorySlot = true;
        this.gameObject.transform.position = new Vector3(-1000, -1000, -1000);
    }

    public void MoveItemOutOfInventory()
    {
        isInInventorySlot = false;
    }
}
