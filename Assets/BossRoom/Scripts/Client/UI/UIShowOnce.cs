using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple behavior to add to a UI obj to deactivate on start if already seen this session
/// </summary>
public class UIShowOnce : MonoBehaviour
{
    // keep a list of UI objects already shown, so that
    // this behavior can be shared by different UI elements
    private static List<string> s_Shown = null;

    void Start()
    {
        if (s_Shown == null)
        {
            s_Shown = new List<string>();
        }

        string objName = transform.name;
        if (s_Shown.Contains(objName))
        {
            gameObject.SetActive(false);
            return;
        }
        s_Shown.Add(objName);
    }
}
