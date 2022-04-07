#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
 
public static class SerializeReferenceGenericSelectionMenu
{
    /// Purpose.
    /// This is generic selection menu (will appear at position).
    /// Filtering. 
    /// You can add substring filter here to filter by search string.
    /// As well ass type or interface restrictions.
    /// As well as any custom restriction that is based on input type.
    /// And it will be performed on each Appropriate type found by TypeCache.
    public static void ShowContextMenuForManagedReference(this SerializedProperty property, Rect position, IEnumerable<Func<Type,bool>> filters = null)
    { 
        var context = new GenericMenu();
        FillContextMenu(filters, context, property);
        context.DropDown(position);
    }

    /// Purpose.
    /// This is generic selection menu (will appear at click position).
    /// Filtering. 
    /// You can add substring filter here to filter by search string.
    /// As well ass type or interface restrictions.
    /// As well as any custom restriction that is based on input type.
    /// And it will be performed on each Appropriate type found by TypeCache.
    public static void ShowContextMenuForManagedReference(this SerializedProperty property, IEnumerable<Func<Type, bool>> filters = null)
    {
        var context = new GenericMenu();
        FillContextMenu(filters, context, property);
        context.ShowAsContext();
    }
    
    private static void FillContextMenu(IEnumerable<Func<Type, bool>> enumerableFilters, GenericMenu contextMenu, SerializedProperty property)
    {
        var filters = enumerableFilters.ToList();// Prevents possible multiple enumerations
        
        // Adds "Make Null" menu command
        contextMenu.AddItem(new GUIContent("Null"), false, property.SetManagedReferenceToNull);
         
        // Collects appropriate types
        var appropriateTypes = property.GetAppropriateTypesForAssigningToManagedReference(filters);
        
        // Adds appropriate types to menu
        foreach (var appropriateType in appropriateTypes)
            AddItemToContextMenu(appropriateType, contextMenu, property); 
    }
    
    private static void AddItemToContextMenu(Type type, GenericMenu genericMenuContext, SerializedProperty property)
    {
        var assemblyName =  type.Assembly.ToString().Split('(', ',')[0];
        var entryName = type + "  ( " + assemblyName + " )";
        genericMenuContext.AddItem(new GUIContent(entryName), false, AssignNewInstanceCommand, new GenericMenuParameterForAssignInstanceCommand(type, property));
    }
    
    private static void AssignNewInstanceCommand(object objectGenericMenuParameter )
    {
        var parameter = (GenericMenuParameterForAssignInstanceCommand) objectGenericMenuParameter;
        var type = parameter.Type;
        var property = parameter.Property;
        property.AssignNewInstanceOfTypeToManagedReference(type);
    }

    private readonly struct GenericMenuParameterForAssignInstanceCommand
    {
        public GenericMenuParameterForAssignInstanceCommand(Type type, SerializedProperty property)
        {
            Type = type;
            Property = property;
        }
        
        public readonly SerializedProperty Property;
        public readonly Type Type;
    }
} 

#endif