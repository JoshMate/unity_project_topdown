using UnityEngine;

public class F_Logic_GUI : MonoBehaviour
{

    [Header("Object Refs")]
    public GameObject hudScreen;
    public GameObject characterMenuScreen;


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
