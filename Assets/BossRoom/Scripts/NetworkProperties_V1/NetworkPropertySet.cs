using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.NetworkProperties
{
    [Serializable]
    public struct NetworkPropertyDefinition
    {
        public string Name;
        public PropertyType Type;
        public PropertySerializationData DefaultValue;
    }

    [CustomPropertyDrawer(typeof(NetworkPropertyDefinition))]
    public class IngredientDrawerUIE : PropertyDrawer
    {
        // Draw the property inside the given rect
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Using BeginProperty / EndProperty on the parent property means that
            // prefab override logic works on the entire property.
            EditorGUI.BeginProperty(position, label, property);

            var nameRect = new Rect(position.x, position.y, 100, position.height);
            var typeRect = new Rect(position.x + 100, position.y, 100, position.height);
            var defaultValueRect = new Rect(position.x + 200, position.y, 100, position.height);

            EditorGUI.PropertyField(nameRect, property.FindPropertyRelative("Name"), GUIContent.none);
            EditorGUI.PropertyField(typeRect, property.FindPropertyRelative("Type"), GUIContent.none);

            var type = (PropertyType)property.FindPropertyRelative("Type").enumValueIndex;

            var defaultValueProperty = property.FindPropertyRelative("DefaultValue");
            switch (type)
            {
                case PropertyType.Int:
                    int value = EditorGUI.IntField(defaultValueRect, /*defaultValueProperty.GetValue<PropertySerializationData>()?.Int ?? 0*/0);
                    //defaultValueProperty.SetValue(value);
                    break;
                case PropertyType.Float:
                    EditorGUI.FloatField(defaultValueRect, 0);
                    break;
                default:
                    throw new NotSupportedException("Not supported property type");
            }

            //EditorGUI(defaultValueRect, property.FindPropertyRelative("Type"), GUIContent.none);

            // // Draw label
            // position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            //
            // // Don't make child fields be indented
            // var indent = EditorGUI.indentLevel;
            // EditorGUI.indentLevel = 0;
            //
            // // Calculate rects
            // var amountRect = new Rect(position.x, position.y, 30, position.height);
            // var unitRect = new Rect(position.x + 35, position.y, 50, position.height);
            // var nameRect = new Rect(position.x + 90, position.y, position.width - 90, position.height);
            //
            // // Draw fields - passs GUIContent.none to each so they are drawn without labels
            // EditorGUI.PropertyField(amountRect, property.FindPropertyRelative("amount"), GUIContent.none);
            // EditorGUI.PropertyField(unitRect, property.FindPropertyRelative("unit"), GUIContent.none);
            // EditorGUI.PropertyField(nameRect, property.FindPropertyRelative("name"), GUIContent.none);
            //
            // // Set indent back to what it was
            // EditorGUI.indentLevel = indent;

            EditorGUI.EndProperty();
        }
    }

    [Serializable]
    public unsafe struct PropertySerializationData
    {
        private unsafe fixed byte m_Data[4];

        public int Int
        {
            get
            {
                fixed (byte* ptr = m_Data)
                {
                    return *(int*)ptr;
                }
            }
            set
            {
                fixed (byte* ptr = m_Data)
                {
                    *(int*)ptr = value;
                }
            }
        }
    }

    public enum PropertyType
    {
        Int = 0,
        Float = 1,
    }

    [CreateAssetMenu]
    public class NetworkPropertySet : ScriptableObject
    {
        public List<NetworkPropertyDefinition> Properties = new List<NetworkPropertyDefinition>();
        public List<NetworkPropertySet> Children;
    }


    public static class SerializedPropertyExtensions
    {
        // public static T? GetValue<T>(this SerializedProperty property) where T: struct
        // {
        //     Type parentType = property.serializedObject.targetObject.GetType();
        //     System.Reflection.FieldInfo fi = parentType.GetField(property.propertyPath);
        //     var value = fi.GetValue(property.serializedObject.targetObject);
        //     return value as T?;
        // }
        // public static void SetValue<T>(this SerializedProperty property, T value)
        // {
        //     Type parentType = property.serializedObject.targetObject.GetType();
        //     System.Reflection.FieldInfo fi = parentType.GetField(property.propertyPath);
        //     fi.SetValue(property.serializedObject.targetObject, value);
        // }
    }
}
