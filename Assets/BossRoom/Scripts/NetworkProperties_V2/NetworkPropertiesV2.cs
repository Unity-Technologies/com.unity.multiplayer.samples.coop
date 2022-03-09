using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class NetworkPropertySetAttribute : Attribute { }

public enum NetworkPropertySet
{
    None = 0,
    HealthPropertySet = 1,
}

public enum NetworkProperty
{
    None = 0,
    Health = 1,
}

// public enum Access
// {
//     None = 0,
//     Readonly = 1,
//     WriteOnly = 2,
//     ReadWrite = 3,
// }

public class NetworkPropertiesV2 : MonoBehaviour
{
    public List<NetworkPropertySet> PropertySets;
}

[CustomEditor(typeof(NetworkPropertiesV2))]
[CanEditMultipleObjects]
public class NetworkPropertiesV2Editor : Editor
{
    SerializedProperty propertySets;

    void OnEnable()
    {
        propertySets = serializedObject.FindProperty("PropertySets");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(propertySets);
        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.LabelField("HealthPropertySet", EditorStyles.boldLabel);
        EditorGUILayout.IntField("Health", 0);
    }
}

public struct NetworkPropertyHandle : IDisposable
{
    public void Dispose() { }
}

public static class NetworkPropertiesExt
{
    public static NetworkPropertyHandle GetNetworkPropertyHandle(this Component component)
    {
        return default;
    }

    public static T GetNetworkProperty<T>(this Component component, NetworkProperty property)
    {
        return default;
    }

    public static void SetNetworkProperty<T>(this Component component, NetworkProperty property, T value) { }

    public static T GetNetworkProperty<T>(this NetworkPropertyHandle handle, NetworkProperty property)
    {
        return default;
    }

    public static void SetNetworkProperty<T>(this NetworkPropertyHandle handle, NetworkProperty property, T value) { }
}
