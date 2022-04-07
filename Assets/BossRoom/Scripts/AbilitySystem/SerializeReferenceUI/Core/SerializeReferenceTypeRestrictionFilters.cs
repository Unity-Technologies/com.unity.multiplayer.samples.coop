    using System;
using System.Linq;

public static class SerializeReferenceTypeRestrictionFilters
{
    public static Func<Type, bool> TypeIsNotSubclassOrEqualOrHasInterface(Type[] types) => type =>
        TypeIsSubclassOrEqualOrHasInterface(types).Invoke(type) == false;

    public static Func<Type, bool> TypeIsSubclassOrEqualOrHasInterface(Type[] types) => type =>
        types.Any(e => e.IsInterface ? TypeHasInterface(type, e) : TypeIsSubclassOrEqual(type, e));

    public static bool TypeIsSubclassOrEqual(Type type, Type comparator) =>  type.IsSubclassOf(comparator) || type == comparator;
    public static bool TypeHasInterface(this Type type, Type comparator) =>  type.GetInterface(comparator.ToString()) != null;
}