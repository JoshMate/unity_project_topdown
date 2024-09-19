using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class F_Logic_Globals : MonoBehaviour
{

    [Header("Tag Names")]

    public const string tagEnt = "Tag_Ent";

    [Header("Layers")]

    public const string layerDebugAlwaysTop = "Layer_DebugAlwaysTop";
    public const string layerLogic = "Layer_Logic";
    public const string layerGui = "Layer_Gui";
    public const string layerPopUps = "Layer_PopUps";
    public const string layerHud = "Layer_HudBottom";
    public const string layerProjectile = "Layer_Projectile";
    public const string layerEffect = "Layer_Effect";
    public const string layerParticle = "Layer_Particle";
    public const string layerPlayerOver = "Layer_PlayerOver";
    public const string layerPlayerBody = "Layer_PlayerBody";
    public const string layerPlayerUnder = "Layer_PlayerUnder";
    public const string layerNPCBoss = "Layer_NPCBoss";
    public const string layerNPC = "Layer_NPC";
    public const string layerItem = "Layer_Item";
    public const string layerEntDynamic = "Layer_EntDynamic";
    public const string layerEntUsable = "Layer_EntUsable";
    public const string layerEntDecoration = "Layer_EntDecoration";
    public const string layerWallDecal = "Layer_WallDecal";
    public const string layerWall = "Layer_Wall";
    public const string layerFloorDecal = "Layer_FloorDecal";
    public const string layerFloorDecoration = "Layer_FloorDecoration";
    public const string layerFloor = "Layer_Floor";
    public const string layerBackground = "Layer_Background";

    [Header("Strings")]
    public const string stringGameName = "Project Top Down";

    void Start()
    {
        DontDestroyOnLoad(gameObject);
    }
}
