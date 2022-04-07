#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class ManagedReferenceUtility
{
    /// Creates instance of passed type and assigns it to managed reference
    public static object AssignNewInstanceOfTypeToManagedReference(this SerializedProperty serializedProperty, Type type)
    {
        var instance = Activator.CreateInstance(type);
        
        serializedProperty.serializedObject.Update(); 
        serializedProperty.managedReferenceValue = instance;
        serializedProperty.serializedObject.ApplyModifiedProperties(); 
        
        return instance;
    }

    /// Sets managed reference to null
    public static void SetManagedReferenceToNull(this SerializedProperty serializedProperty)
    {
        serializedProperty.serializedObject.Update();
        serializedProperty.managedReferenceValue = null;
        serializedProperty.serializedObject.ApplyModifiedProperties(); 
    }

    /// Collects appropriate types based on managed reference field type and filters. Filters all derive
    public static IEnumerable<Type> GetAppropriateTypesForAssigningToManagedReference(this SerializedProperty property, List<Func<Type, bool>> filters = null)
    {
        var fieldType = property.GetManagedReferenceFieldType();
        return GetAppropriateTypesForAssigningToManagedReference(fieldType, filters);
    }

    /// Filters derived types of field typ parameter and finds ones whose are compatible with managed reference and filters.
    public static IEnumerable<Type> GetAppropriateTypesForAssigningToManagedReference(Type fieldType, List<Func<Type, bool>> filters = null)
    {
        var appropriateTypes = new List<Type>();

        // Get and filter all appropriate types
        var derivedTypes = TypeCache.GetTypesDerivedFrom(fieldType);
        foreach (var type in derivedTypes)
        {
            // Skips unity engine Objects (because they are not serialized by SerializeReference)
            if (type.IsSubclassOf(typeof(Object)))
                continue;
            // Skip abstract classes because they should not be instantiated
            if (type.IsAbstract)
                continue;
			// Skip generic classes because they can not be instantiated
            if (type.ContainsGenericParameters)
                continue;
            // Skip types that has no public empty constructors (activator can not create them)    
            if (type.IsClass && type.GetConstructor(Type.EmptyTypes) == null) // Structs still can be created (strangely)
                continue;
            // Filter types by provided filters if there is ones
            if (filters != null && filters.All(f => f == null || f.Invoke(type)) == false) 
                continue;

            appropriateTypes.Add(type);
        }

        return appropriateTypes;
    }
    
    /// Gets real type of managed reference
    public static Type GetManagedReferenceFieldType(this SerializedProperty property)
    {
        var realPropertyType = GetRealTypeFromTypename(property.managedReferenceFieldTypename);
        if (realPropertyType != null) 
            return realPropertyType;
        
        Debug.LogError($"Can not get field type of managed reference : {property.managedReferenceFieldTypename}");
        return null;
    }
    
    /// Gets real type of managed reference's field typeName
    public static Type GetRealTypeFromTypename(string stringType)
    {
        var names = GetSplitNamesFromTypename(stringType);
        var realType = Type.GetType($"{names.ClassName}, {names.AssemblyName}");
        return realType;
    }
    
    /// Get assembly and class names from typeName
    public static (string AssemblyName, string ClassName) GetSplitNamesFromTypename(string typename)
    {
        if (string.IsNullOrEmpty(typename))  
            return ("","");
        
        var typeSplitString = typename.Split(char.Parse(" "));
        var typeClassName = typeSplitString[1];
        var typeAssemblyName = typeSplitString[0];
        return (typeAssemblyName,  typeClassName); 
    }
}
#endif