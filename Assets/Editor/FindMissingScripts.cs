using UnityEngine;
using UnityEditor;
public class FindMissingScripts : EditorWindow
{
    [MenuItem("Window/FindMissingScripts")]
    public static void ShowWindow()
    {
        FindInSelected();
    }

    private static void FindInSelected()
    {
        GameObject[] allObjects = FindObjectsOfType<GameObject>();

        string result = "";
        int go_count = 0, components_count = 0, missing_count = 0;
        foreach (GameObject g in allObjects)
        {
            go_count++;
            var components = g.GetComponentsInChildren<MonoBehaviour>();
            for (int i = 0; i < components.Length; i++)
            {
                components_count++;
                if (components[i] == null)
                {
                    missing_count++;
                    string gameObjectName = g.name;
                    Transform t = g.transform;
                    while (t.parent != null)
                    {
                        gameObjectName = t.parent.name +"/"+gameObjectName;
                        t = t.parent;
                    }

                    var currentMessage = $"{gameObjectName} has an empty script attached in position: {i}\n";
                    Debug.Log(currentMessage, g);
                    result += currentMessage;
                }
            }
        }
        Debug.Log(result);

        Debug.Log(string.Format("Searched {0} GameObjects, {1} components, found {2} missing", go_count, components_count, missing_count));
    }
}
