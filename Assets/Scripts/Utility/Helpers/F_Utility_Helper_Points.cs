using UnityEngine;
using UnityEngine.SceneManagement;

public static class F_Utility_Helper_Points
{
    /// <summary>Finds the first active point of the given type anywhere in a scene's hierarchy.</summary>
    /// <param name="scene">The scene to search.</param>
    /// <param name="pointType">The point type to look for.</param>
    /// <returns>The matching point, or null when the scene has none.</returns>
    public static F_Point FindPointInScene(Scene scene, enumPointType pointType)
    {
        foreach (GameObject rootObject in scene.GetRootGameObjects())
        {
            foreach (F_Point point in rootObject.GetComponentsInChildren<F_Point>())
            {
                if (point.pointType == pointType)
                {
                    return point;
                }
            }
        }

        return null;
    }
}
