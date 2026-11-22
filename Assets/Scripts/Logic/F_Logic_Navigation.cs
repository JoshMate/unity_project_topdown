using System;
using System.Collections;
using NavMeshPlus.Components;
using NavMeshPlus.Extensions;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>
/// Lives on the persistent game manager. Builds a 2D NavMesh for every loaded scene that has F_Scene_Settings,
/// and rebuilds it when an ent that blocks navigation dies. No per-scene or per-prefab setup is needed.
/// </summary>
public class F_Logic_Navigation : MonoBehaviour
{
    [Header("Privates")]
    private NavMeshSurface navigationSurface;
    private Scene navigationScene;
    private Coroutine navigationRebuildRoutine;
    private bool navigationRebuildPending;

    // Events (Header attributes are not valid on events)
    // Fired after the initial build of a scene and after every rebuild
    public static event Action OnNavMeshReady;

    /// <summary>Whether the current scene's NavMesh has been built and can be used by agents.</summary>
    public static bool IsNavMeshReady { get; private set; }

    private void Awake()
    {
        SceneManager.sceneLoaded += BuildForScene;
        SceneManager.sceneUnloaded += ClearForScene;
        F_Ent.OnAnyEntDied += HandleEntDied;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= BuildForScene;
        SceneManager.sceneUnloaded -= ClearForScene;
        F_Ent.OnAnyEntDied -= HandleEntDied;
        IsNavMeshReady = false;
    }

    /// <summary>Rebuilds the NavMesh asynchronously after a short delay. Several requests during the delay or a running build share one extra rebuild.</summary>
    public void RequestRebuild()
    {
        if (navigationSurface == null)
        {
            Debug.LogWarning("F_Logic_Navigation: no NavMesh surface exists for the current scene, so the rebuild was skipped.");
            return;
        }

        navigationRebuildPending = true;
        if (navigationRebuildRoutine == null)
        {
            navigationRebuildRoutine = StartCoroutine(RebuildRoutine());
        }
    }

    // Creates the surface and walkable base for a gameplay scene and builds its NavMesh synchronously.
    private void BuildForScene(Scene loadedScene, LoadSceneMode loadMode)
    {
        F_Scene_Settings sceneSettings = FindSceneSettings(loadedScene);
        if (sceneSettings == null)
        {
            return;
        }

        StopRebuild();
        IsNavMeshReady = false;
        navigationScene = loadedScene;

        Vector3 scenePosition = sceneSettings.transform.position;
        GameObject surfaceObject = new GameObject(F_Utility_Config_Navigation.cfgNavigationSurfaceObjectName);
        SceneManager.MoveGameObjectToScene(surfaceObject, loadedScene);
        surfaceObject.transform.SetPositionAndRotation(scenePosition,
            Quaternion.Euler(F_Utility_Config_Navigation.cfgNavigationSurfaceRotationX, 0f, 0f));

        CreateWalkableBase(surfaceObject.transform, scenePosition, sceneSettings.sceneGroundSize);

        navigationSurface = surfaceObject.AddComponent<NavMeshSurface>();
        navigationSurface.collectObjects = CollectObjects.All;
        navigationSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        navigationSurface.defaultArea = F_Utility_Config_Navigation.cfgNavigationAreaWalkable;
        navigationSurface.agentTypeID = F_Utility_Config_Navigation.cfgNavigationAgentTypeId;
        navigationSurface.hideEditorLogs = F_Utility_Config_Navigation.cfgNavigationHideBuildLogs;
        surfaceObject.AddComponent<CollectSources2d>();

        // Colliders created or moved this frame must be synced before their shapes are collected
        Physics2D.SyncTransforms();
        navigationSurface.BuildNavMesh();
        if (navigationSurface.navMeshData == null)
        {
            Debug.LogWarning($"F_Logic_Navigation: building the NavMesh for scene '{loadedScene.name}' produced no data.");
            return;
        }

        IsNavMeshReady = true;
        OnNavMeshReady?.Invoke();
    }

    // Creates an inert collider covering the scene ground, which the NavMesh is baked onto.
    private void CreateWalkableBase(Transform surfaceTransform, Vector3 scenePosition, Vector2 groundSize)
    {
        GameObject walkableBase = new GameObject(F_Utility_Config_Navigation.cfgNavigationWalkableBaseObjectName);
        walkableBase.transform.SetParent(surfaceTransform, false);
        walkableBase.transform.SetPositionAndRotation(scenePosition, Quaternion.identity);

        int walkableBaseLayer = LayerMask.NameToLayer(F_Utility_Config_Navigation.cfgNavigationWalkableBaseLayer);
        if (walkableBaseLayer >= 0)
        {
            walkableBase.layer = walkableBaseLayer;
        }

        BoxCollider2D walkableCollider = walkableBase.AddComponent<BoxCollider2D>();
        walkableCollider.size = groundSize;
        walkableCollider.isTrigger = false;
        // Collides with nothing, so it never blocks movement, projectiles or free-cell checks
        walkableCollider.excludeLayers = ~0;

        NavMeshModifier walkableModifier = walkableBase.AddComponent<NavMeshModifier>();
        walkableModifier.overrideArea = true;
        walkableModifier.area = F_Utility_Config_Navigation.cfgNavigationAreaWalkable;
    }

    // Requests a rebuild when a navigation-blocking ent dies.
    private void HandleEntDied(F_Ent ent)
    {
        if (IsNavMeshReady && ent != null && ent.BlocksNavigation)
        {
            RequestRebuild();
        }
    }

    // Waits for the dying ents to be destroyed, then rebuilds; loops once more if another request arrived during the build.
    private IEnumerator RebuildRoutine()
    {
        WaitForSeconds rebuildDelay = new WaitForSeconds(F_Utility_Config_Navigation.cfgNavigationRebuildDelay);
        do
        {
            yield return rebuildDelay;
            navigationRebuildPending = false;
            if (navigationSurface == null)
            {
                break;
            }

            AsyncOperation rebuildOperation = navigationSurface.UpdateNavMesh(navigationSurface.navMeshData);
            yield return rebuildOperation;
            OnNavMeshReady?.Invoke();
        }
        while (navigationRebuildPending);

        navigationRebuildRoutine = null;
    }

    // Forgets the surface when its scene unloads.
    private void ClearForScene(Scene unloadedScene)
    {
        if (unloadedScene != navigationScene)
        {
            return;
        }

        StopRebuild();
        navigationSurface = null;
        IsNavMeshReady = false;
    }

    // Stops any running rebuild and clears its pending request.
    private void StopRebuild()
    {
        if (navigationRebuildRoutine != null)
        {
            StopCoroutine(navigationRebuildRoutine);
            navigationRebuildRoutine = null;
        }

        navigationRebuildPending = false;
    }

    // Finds the scene settings object in a scene, or null for scenes without a playable ground (menus, loading).
    private static F_Scene_Settings FindSceneSettings(Scene scene)
    {
        foreach (GameObject rootObject in scene.GetRootGameObjects())
        {
            F_Scene_Settings sceneSettings = rootObject.GetComponentInChildren<F_Scene_Settings>();
            if (sceneSettings != null)
            {
                return sceneSettings;
            }
        }

        return null;
    }
}
