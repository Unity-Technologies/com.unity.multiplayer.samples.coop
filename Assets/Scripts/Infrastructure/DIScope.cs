using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Unity.Multiplayer.Samples.BossRoom.Shared.Infrastructure
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor)]
    public sealed class Inject : Attribute
    {
    }

    public class NoInstanceToInjectException : Exception
    {
        public NoInstanceToInjectException(string message) : base(message)
        {
        }
    }

    public class ScopeNotFinalizedException : Exception
    {
        public ScopeNotFinalizedException(string message) : base(message)
        {
        }
    }

    public interface IInstanceResolver
    {
        T Resolve<T>()
            where T : class;

        void InjectIn(object obj);
        void InjectIn(GameObject obj);
    }

    public sealed class DIScope : IInstanceResolver, IDisposable
    {
        struct LazyBindDescriptor
        {
            public readonly Type Type;
            public readonly Type[] InterfaceTypes;

            public LazyBindDescriptor(Type type, Type[] interfaceTypes)
            {
                Type = type;
                InterfaceTypes = interfaceTypes;
            }
        }

        static DIScope m_rootScope;

        public static DIScope RootScope
        {
            get
            {
                if (m_rootScope == null)
                {
                    m_rootScope = new DIScope();
                }

                return m_rootScope;
            }
            set => m_rootScope = value;
        }

        readonly DisposableGroup m_DisposableGroup = new DisposableGroup();
        readonly Dictionary<Type, LazyBindDescriptor> m_LazyBindDescriptors = new Dictionary<Type, LazyBindDescriptor>();

        readonly DIScope m_Parent;
        readonly Dictionary<Type, object> m_TypesToInstances = new Dictionary<Type, object>();
        readonly HashSet<object> m_ObjectsWithInjectedDependencies = new HashSet<object>();
        bool m_Disposed;

        bool m_ScopeConstructionComplete;

        public DIScope(DIScope parent = null)
        {
            m_Parent = parent;
            BindInstanceAsSingle<DIScope, IInstanceResolver>(this);
        }

        ~DIScope()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (!m_Disposed)
            {
                m_TypesToInstances.Clear();
                m_ObjectsWithInjectedDependencies.Clear();
                m_DisposableGroup.Dispose();
                m_Disposed = true;
            }
        }

        public T Resolve<T>() where T : class
        {
            var type = typeof(T);

            if (!m_ScopeConstructionComplete)
            {
                throw new ScopeNotFinalizedException(
                    $"Trying to Resolve type {type}, but the DISCope is not yet finalized! You should call FinalizeScopeConstruction before any of the Resolve calls.");
            }

            //if we have this type as lazy-bound instance - we are going to instantiate it now
            if (m_LazyBindDescriptors.TryGetValue(type, out var lazyBindDescriptor))
            {
                var instance = (T)InstantiateLazyBoundObject(lazyBindDescriptor);
                m_LazyBindDescriptors.Remove(type);
                foreach (var interfaceType in lazyBindDescriptor.InterfaceTypes)
                {
                    m_LazyBindDescriptors.Remove(interfaceType);
                }
                return instance;
            }

            if (!m_TypesToInstances.TryGetValue(type, out var value))
            {
                if (m_Parent != null)
                {
                    return m_Parent.Resolve<T>();
                }

                throw new NoInstanceToInjectException($"Injection of type {type} failed.");
            }

            return (T)value;
        }

        public void InjectIn(object obj)
        {
            if (m_ObjectsWithInjectedDependencies.Contains(obj))
            {
                return;
            }

            if (CachedReflectionUtility.TryGetInjectableMethod(obj.GetType(), out var injectionMethod))
            {
                var parameters = CachedReflectionUtility.GetMethodParameters(injectionMethod);

                var paramColleciton = new object[parameters.Length];

                for (var i = 0; i < parameters.Length; i++)
                {
                    var parameter = parameters[i];

                    var genericResolveMethod = CachedReflectionUtility.GetTypedResolveMethod(parameter.ParameterType);
                    var resolved = genericResolveMethod.Invoke(this, null);
                    paramColleciton[i] = resolved;
                }

                injectionMethod.Invoke(obj, paramColleciton);
                m_ObjectsWithInjectedDependencies.Add(obj);
            }
        }

        public void InjectIn(GameObject go)
        {
            var components = go.GetComponentsInChildren<Component>(includeInactive: true);

            foreach (var component in components)
            {
                InjectIn(component);
            }
        }

        public void BindInstanceAsSingle<T>(T instance) where T : class
        {
            BindInstanceToType(instance, typeof(T));
        }

        public void BindInstanceAsSingle<TImplementation, TInterface>(TImplementation instance)
            where TImplementation : class, TInterface
            where TInterface : class
        {
            BindInstanceAsSingle<TInterface>(instance);
            BindInstanceAsSingle(instance);
        }

        public void BindInstanceAsSingle<TImplementation, TInterface, TInterface2, TInterface3>(TImplementation instance)
            where TImplementation : class, TInterface, TInterface2, TInterface3
            where TInterface : class
            where TInterface2 : class
            where TInterface3 : class
        {
            BindInstanceAsSingle<TInterface>(instance);
            BindInstanceAsSingle<TInterface2>(instance);
            BindInstanceAsSingle<TInterface3>(instance);
            BindInstanceAsSingle(instance);
        }

        void BindInstanceToType(object instance, Type type)
        {
            m_TypesToInstances[type] = instance;
        }

        public void BindAsSingle<TImplementation, TInterface>()
            where TImplementation : class, TInterface
            where TInterface : class
        {
            LazyBind(typeof(TImplementation), typeof(TInterface));
        }

        public void BindAsSingle<TImplementation, TInterface, TInterface2>()
            where TImplementation : class, TInterface, TInterface2
            where TInterface : class
            where TInterface2 : class
        {
            LazyBind(typeof(TImplementation), typeof(TInterface), typeof(TInterface2));
        }

        public void BindAsSingle<TImplementation, TInterface, TInterface2, TInterface3>()
            where TImplementation : class, TInterface, TInterface2, TInterface3
            where TInterface : class
            where TInterface2 : class
            where TInterface3 : class
        {
            LazyBind(typeof(TImplementation), typeof(TInterface), typeof(TInterface2));
        }

        public void BindAsSingle<T>()
            where T : class
        {
            LazyBind(typeof(T));
        }

        void LazyBind(Type type, params Type[] typeAliases)
        {
            var descriptor = new LazyBindDescriptor(type, typeAliases);

            foreach (var typeAlias in typeAliases)
            {
                m_LazyBindDescriptors[typeAlias] = descriptor;
            }

            m_LazyBindDescriptors[type] = descriptor;
        }

        object InstantiateLazyBoundObject(LazyBindDescriptor descriptor)
        {
            object instance;
            if (CachedReflectionUtility.TryGetInjectableConstructor(descriptor.Type, out var constructor))
            {
                var parameters = GetResolvedInjectionMethodParameters(constructor);
                instance = constructor.Invoke(parameters);
            }
            else
            {
                instance = Activator.CreateInstance(descriptor.Type);
                InjectIn(instance);
            }

            AddToDisposableGroupIfDisposable(instance);

            BindInstanceToType(instance, descriptor.Type);

            if (descriptor.InterfaceTypes != null)
            {
                foreach (var interfaceType in descriptor.InterfaceTypes)
                {
                    BindInstanceToType(instance, interfaceType);
                }
            }

            return instance;
        }

        void AddToDisposableGroupIfDisposable(object instance)
        {
            if (instance is IDisposable disposable)
            {
                m_DisposableGroup.Add(disposable);
            }
        }

        /// <summary>
        /// This method forces the finalization of construction of DI Scope. It would inject all the instances passed to it directly.
        /// Objects that were bound by just type will be instantiated on their first use.
        /// </summary>
        public void FinalizeScopeConstruction()
        {
            if (m_ScopeConstructionComplete)
            {
                return;
            }

            m_ScopeConstructionComplete = true;

            var uniqueObjects = new HashSet<object>(m_TypesToInstances.Values);

            foreach (var objectToInject in uniqueObjects)
            {
                InjectIn(objectToInject);
            }
        }

        object[] GetResolvedInjectionMethodParameters(MethodBase injectionMethod)
        {
            var parameters = CachedReflectionUtility.GetMethodParameters(injectionMethod);

            var paramColleciton = new object[parameters.Length];

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];

                var genericResolveMethod = CachedReflectionUtility.GetTypedResolveMethod(parameter.ParameterType);
                var resolved = genericResolveMethod.Invoke(this, null);
                paramColleciton[i] = resolved;
            }

            return paramColleciton;
        }

        static class CachedReflectionUtility
        {
            static readonly Dictionary<Type, MethodBase> k_CachedInjectableMethods = new Dictionary<Type, MethodBase>();
            static readonly Dictionary<Type, ConstructorInfo> k_CachedInjectableConstructors = new Dictionary<Type, ConstructorInfo>();
            static readonly Dictionary<MethodBase, ParameterInfo[]> k_CachedMethodParameters = new Dictionary<MethodBase, ParameterInfo[]>();
            static readonly Dictionary<Type, MethodInfo> k_CachedResolveMethods = new Dictionary<Type, MethodInfo>();
            static readonly Type k_InjectAttributeType = typeof(Inject);
            static readonly HashSet<Type> k_ProcessedTypes = new HashSet<Type>();
            static MethodInfo k_ResolveMethod;

            public static bool TryGetInjectableConstructor(Type type, out ConstructorInfo method)
            {
                CacheTypeMethods(type);
                return k_CachedInjectableConstructors.TryGetValue(type, out method);
            }

            static void CacheTypeMethods(Type type)
            {
                if (k_ProcessedTypes.Contains(type))
                {
                    return;
                }

                var constructors = type.GetConstructors();
                foreach (var constructorInfo in constructors)
                {
                    var foundInjectionSite = constructorInfo.IsDefined(k_InjectAttributeType);
                    if (foundInjectionSite)
                    {
                        k_CachedInjectableConstructors[type] = constructorInfo;
                        var methodParameters = constructorInfo.GetParameters();
                        k_CachedMethodParameters[constructorInfo] = methodParameters;
                        break;
                    }
                }

                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                foreach (var methodInfo in methods)
                {
                    var foundInjectionSite = methodInfo.IsDefined(k_InjectAttributeType);
                    if (foundInjectionSite)
                    {
                        k_CachedInjectableMethods[type] = methodInfo;
                        var methodParameters = methodInfo.GetParameters();
                        k_CachedMethodParameters[methodInfo] = methodParameters;
                        break;
                    }
                }

                k_ProcessedTypes.Add(type);
            }

            public static bool TryGetInjectableMethod(Type type, out MethodBase method)
            {
                CacheTypeMethods(type);
                return k_CachedInjectableMethods.TryGetValue(type, out method);
            }

            public static ParameterInfo[] GetMethodParameters(MethodBase injectionMethod)
            {
                return k_CachedMethodParameters[injectionMethod];
            }

            public static MethodInfo GetTypedResolveMethod(Type parameterType)
            {
                if (!k_CachedResolveMethods.TryGetValue(parameterType, out var resolveMethod))
                {
                    if (k_ResolveMethod == null)
                    {
                        k_ResolveMethod = typeof(DIScope).GetMethod("Resolve");
                    }

                    resolveMethod = k_ResolveMethod.MakeGenericMethod(parameterType);
                    k_CachedResolveMethods[parameterType] = resolveMethod;
                }

                return resolveMethod;
            }
        }
    }
}
