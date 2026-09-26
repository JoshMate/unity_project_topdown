using UnityEngine;

public class F_Logic_GameManager : MonoBehaviour
{

    [Header("Object Refs")]
    public F_Logic_Globals globalsObject;
    public F_Logic_Camera cameraObject;
    public F_Logic_GUI guiObject;
    public F_PlayerController playerObject;



    // Awake runs before the initialization scene requests its transition to gameplay.
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
