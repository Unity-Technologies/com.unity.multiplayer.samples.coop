using System;
using UnityEngine;

/// None of this types or interface types are valid.
[AttributeUsage(AttributeTargets.Field)]
public class SerializeReferenceUIRestrictionExcludeTypes : PropertyAttribute
{
    public readonly Type[] Types;
    public SerializeReferenceUIRestrictionExcludeTypes(params Type[] types) => Types = types;
} 