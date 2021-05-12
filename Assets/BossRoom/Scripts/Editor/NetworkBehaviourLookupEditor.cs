using MLAPI;
using UnityEditor;
using UnityEngine;

namespace BossRoom.Scripts.Editor
{
    /// <summary>
    /// Custom inspector class for NetworkBehaviourLookup class. Overriding OnInspectorGUI() to add debugging
    /// functionality via the ability to automatically populate the target's array of NetworkBehaviours.
    /// </summary>
    [CustomEditor(typeof(NetworkBehaviourLookup))]
    public class NetworkBehaviourLookupEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var networkBehaviourLookup = (NetworkBehaviourLookup)target;

            if (GUILayout.Button("Populate"))
            {
                PopulateNetworkBehaviours(networkBehaviourLookup);
            }
        }

        void PopulateNetworkBehaviours(NetworkBehaviourLookup networkBehaviourLookup)
        {
            var serializedNetworkBehaviourLookup = new SerializedObject(networkBehaviourLookup);

            var serializedPropertyArray = serializedNetworkBehaviourLookup.FindProperty("m_NetworkBehaviours");
            if (serializedPropertyArray.serializedObject == null)
            {
                return;
            }

            NetworkBehaviour[] networkBehaviours = networkBehaviourLookup.GetComponents<NetworkBehaviour>();
            serializedPropertyArray.ClearArray();

            for (int i = 0; i < networkBehaviours.Length; i++)
            {
                serializedPropertyArray.InsertArrayElementAtIndex(i);
                var arrayElement = serializedPropertyArray.GetArrayElementAtIndex(i);
                arrayElement.objectReferenceValue = networkBehaviours[i];
            }

            serializedNetworkBehaviourLookup.ApplyModifiedProperties();
        }
    }
}
