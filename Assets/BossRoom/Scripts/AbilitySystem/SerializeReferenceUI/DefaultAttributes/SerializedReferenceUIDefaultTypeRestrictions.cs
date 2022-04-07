using System;
using System.Collections.Generic;
using System.Reflection;

public static class SerializedReferenceUIDefaultTypeRestrictions
{
    public static IEnumerable<Func<Type, bool>> GetAllBuiltInTypeRestrictions(FieldInfo fieldInfo)
    {
        var result = new List<Func<Type, bool>>();
        
        var attributeObjects = fieldInfo.GetCustomAttributes(false);
        foreach (var attributeObject in attributeObjects)
        {
            switch (attributeObject)
            { 
                case SerializeReferenceUIRestrictionIncludeTypes includeTypes:
                    result.Add(SerializeReferenceTypeRestrictionFilters.TypeIsSubclassOrEqualOrHasInterface(includeTypes.Types)); 
                    continue;
                case SerializeReferenceUIRestrictionExcludeTypes excludeTypes:
                    result.Add(SerializeReferenceTypeRestrictionFilters.TypeIsNotSubclassOrEqualOrHasInterface(excludeTypes.Types)); 
                    continue;
            } 
        }
        return result; 
    }
} 