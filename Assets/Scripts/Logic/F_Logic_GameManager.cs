using UnityEngine;

public class F_Logic_GameManager : MonoBehaviour
{

    [Header("Object Refs")]
    public F_Logic_Globals globalsObject;
    public F_Logic_Camera cameraObject;
    public F_Logic_GUI guiObject;
    public F_PlayerController playerObject;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
