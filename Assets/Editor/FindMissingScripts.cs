using UnityEngine;
using UnityEditor;

/// <summary>
/// Small editor utility to help with finding stripped scripts.
/// This is useful when excluding assemblies from a build for example.
/// </summary>
public class FindMissingScripts : EditorWindow
{
    [MenuItem("Window/FindMissingScripts")]
    public static void ShowWindow()
    {
        FindInSelected();
    }

    static void FindInSelected()
    {
        GameObject[] allObjects = FindObjectsOfType<GameObject>();

        string result = "";
        int gameObjectsCount = 0, componentsCount = 0, missingCount = 0;
        foreach (GameObject gameObject in allObjects)
        {
            gameObjectsCount++;
            var components = gameObject.GetComponentsInChildren<MonoBehaviour>();
            for (int i = 0; i < components.Length; i++)
            {
                componentsCount++;
                if (components[i] == null)
                {
                    missingCount++;
                    string gameObjectName = gameObject.name;
                    Transform t = gameObject.transform;
                    while (t.parent != null)
                    {
                        gameObjectName = t.parent.name + "/" + gameObjectName;
                        t = t.parent;
                    }

                    var currentMessage = $"{gameObjectName} has an empty script attached in position: {i}\n";
                    Debug.Log(currentMessage, gameObject); // so we can click on the gameObject's context
                    result += currentMessage;
                }
            }
        }

        Debug.Log(result);
        Debug.Log($"Searched {gameObjectsCount} GameObjects, {componentsCount} components, found {missingCount} missing");
    }
}
