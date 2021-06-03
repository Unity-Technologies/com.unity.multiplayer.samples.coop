using System;
using UnityEditor;
using UnityEngine;

namespace BossRoom.Scripts.Editor
{
    /// <summary>
    /// Custom inspector for <see cref="GuidScriptableObject"/>, and is responsible for assigning GUID.
    /// </summary>
    [CustomEditor(typeof(GuidScriptableObject), true)]
    public class GuidScriptableObjectEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var guidScriptableObject = (GuidScriptableObject)target;

            var guidScriptableObjectSerializedObject = new SerializedObject(guidScriptableObject);

            var serializedGuidProperty = guidScriptableObjectSerializedObject.FindProperty("m_Guid");

            if (serializedGuidProperty.arraySize == 0)
            {
                var guidByteArray = Guid.NewGuid().ToByteArray();

                serializedGuidProperty.arraySize = 16;
                for (var i = 0; i < guidByteArray.Length; i++)
                {
                    serializedGuidProperty.GetArrayElementAtIndex(i).intValue = guidByteArray[i];
                }

                guidScriptableObjectSerializedObject.ApplyModifiedProperties();
            }
        }
    }
}
