using UnityEditor;
using UnityEngine;
using BossRoom.Client;

namespace BossRoom.Editor
{
    /// <summary>
    /// Custom inspector class for UIRadioButton class. Overriding OnInspectorGUI() to inspect all fields.
    /// </summary>
    [CustomEditor(typeof(UIRadioButton))]
    public class UIRadioButtonEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
        }
    }
}
