using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class F_Logic_Camera : MonoBehaviour
{
   public Camera playerCamera;
   public Transform playerObject;

   // Start is called before the first frame update
   void Start()
   {
        
   }

   // Update is called once per frame

 
   void Update () 
   {
      if (playerObject != null)
      {
         playerCamera.transform.position = new Vector3 (playerObject.position.x, playerObject.position.y, -5);
      }
   }

}
