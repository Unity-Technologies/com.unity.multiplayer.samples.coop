#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class SerializeReferenceInspectorMiddleMouseMenu
{
    public static void ShowContextMenuForManagedReferenceOnMouseMiddleButton(this SerializedProperty property,
        Rect position, IEnumerable<Func<Type, bool>> filters = null)
    {
        var e = Event.current;
        if (e.type != EventType.MouseDown || !position.Contains(e.mousePosition) || e.button != 2) 
            return;
        
        property.ShowContextMenuForManagedReference(filters);
    } 
}

#endif