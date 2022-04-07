using System;
using UnityEngine;

/// Any of this types or interface types are valid. And only this types can be presented.
[AttributeUsage(AttributeTargets.Field)]
public class SerializeReferenceUIRestrictionIncludeTypes : PropertyAttribute
{
    public readonly Type[] Types;
    public SerializeReferenceUIRestrictionIncludeTypes(params Type[] types) => Types = types;
}