using UnityEditor;
using UnityEngine;

namespace Unity.BossRoom.Infrastructure
{
    /// <summary>
    /// Custom inspector class for GameEvent ScriptableObject class. Overriding OnInspectorGUI() to add debugging
    /// functionality via the ability to raise this event within the editor.
    /// </summary>
    [CustomEditor(typeof(GameEvent))]
    public class GameEventEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var gameEvent = (GameEvent)target;

            if (GUILayout.Button("Raise Event"))
            {
                gameEvent.Raise();
            }
        }
    }
}
