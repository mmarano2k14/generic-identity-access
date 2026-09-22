using System.Reflection;

namespace IdentityAccess.Rbac.MultiplexedAdapter
{
    /// <summary>
    /// Holds the validated reflection members for one process-pinned external RBAC distribution.
    /// </summary>
    internal sealed class MultiplexedRbacBinding
    {
        public Type NamespaceEntryType { get; }
        public ConstructorInfo NamespaceEntryConstructor { get; }
        public PropertyInfo NamespaceEntryNameProperty { get; }
        public PropertyInfo NamespaceEntryTrnsProperty { get; }
        public bool NamespaceTrnsAcceptsHashSet { get; }
        public bool NamespaceTrnsAcceptsList { get; }
        public ConstructorInfo? NamespaceTrnsEnumerableConstructor { get; }
        public Type NamespaceListType { get; }

        public Type ContextType { get; }
        public ConstructorInfo ContextConstructor { get; }
        public PropertyInfo ContextKeyProperty { get; }
        public PropertyInfo ProjectProperty { get; }
        public PropertyInfo UserIdProperty { get; }
        public PropertyInfo TenantIdProperty { get; }
        public PropertyInfo TenantGroupIdProperty { get; }
        public PropertyInfo CurrentNamespaceProperty { get; }
        public PropertyInfo NamespacesProperty { get; }
        public PropertyInfo InFlightCountProperty { get; }
        public PropertyInfo TtlSecondsProperty { get; }

        public Type AccessorType { get; }
        public ConstructorInfo AccessorConstructor { get; }
        public MethodInfo AccessorSetMethod { get; }
        public MethodInfo AccessorClearMethod { get; }

        public Type ScopeType { get; }
        public ConstructorInfo ScopeConstructor { get; }

        public Type OptionsType { get; }
        public ConstructorInfo OptionsConstructor { get; }
        public PropertyInfo OptionsProjectProperty { get; }

        public Type OptionsWrapperType { get; }
        public ConstructorInfo OptionsWrapperConstructor { get; }

        public Type BuilderType { get; }
        public ConstructorInfo BuilderConstructor { get; }

        public Type EngineType { get; }
        public ConstructorInfo EngineConstructor { get; }
        public MethodInfo IsAllowedMethod { get; }

        /// <summary>Initializes a validated external RBAC binding.</summary>
        public MultiplexedRbacBinding(
            Type namespaceEntryType,
            ConstructorInfo namespaceEntryConstructor,
            PropertyInfo namespaceEntryNameProperty,
            PropertyInfo namespaceEntryTrnsProperty,
            bool namespaceTrnsAcceptsHashSet,
            bool namespaceTrnsAcceptsList,
            ConstructorInfo? namespaceTrnsEnumerableConstructor,
            Type namespaceListType,
            Type contextType,
            ConstructorInfo contextConstructor,
            PropertyInfo contextKeyProperty,
            PropertyInfo projectProperty,
            PropertyInfo userIdProperty,
            PropertyInfo tenantIdProperty,
            PropertyInfo tenantGroupIdProperty,
            PropertyInfo currentNamespaceProperty,
            PropertyInfo namespacesProperty,
            PropertyInfo inFlightCountProperty,
            PropertyInfo ttlSecondsProperty,
            Type accessorType,
            ConstructorInfo accessorConstructor,
            MethodInfo accessorSetMethod,
            MethodInfo accessorClearMethod,
            Type scopeType,
            ConstructorInfo scopeConstructor,
            Type optionsType,
            ConstructorInfo optionsConstructor,
            PropertyInfo optionsProjectProperty,
            Type optionsWrapperType,
            ConstructorInfo optionsWrapperConstructor,
            Type builderType,
            ConstructorInfo builderConstructor,
            Type engineType,
            ConstructorInfo engineConstructor,
            MethodInfo isAllowedMethod)
        {
            NamespaceEntryType = namespaceEntryType;
            NamespaceEntryConstructor = namespaceEntryConstructor;
            NamespaceEntryNameProperty = namespaceEntryNameProperty;
            NamespaceEntryTrnsProperty = namespaceEntryTrnsProperty;
            NamespaceTrnsAcceptsHashSet = namespaceTrnsAcceptsHashSet;
            NamespaceTrnsAcceptsList = namespaceTrnsAcceptsList;
            NamespaceTrnsEnumerableConstructor = namespaceTrnsEnumerableConstructor;
            NamespaceListType = namespaceListType;
            ContextType = contextType;
            ContextConstructor = contextConstructor;
            ContextKeyProperty = contextKeyProperty;
            ProjectProperty = projectProperty;
            UserIdProperty = userIdProperty;
            TenantIdProperty = tenantIdProperty;
            TenantGroupIdProperty = tenantGroupIdProperty;
            CurrentNamespaceProperty = currentNamespaceProperty;
            NamespacesProperty = namespacesProperty;
            InFlightCountProperty = inFlightCountProperty;
            TtlSecondsProperty = ttlSecondsProperty;
            AccessorType = accessorType;
            AccessorConstructor = accessorConstructor;
            AccessorSetMethod = accessorSetMethod;
            AccessorClearMethod = accessorClearMethod;
            ScopeType = scopeType;
            ScopeConstructor = scopeConstructor;
            OptionsType = optionsType;
            OptionsConstructor = optionsConstructor;
            OptionsProjectProperty = optionsProjectProperty;
            OptionsWrapperType = optionsWrapperType;
            OptionsWrapperConstructor = optionsWrapperConstructor;
            BuilderType = builderType;
            BuilderConstructor = builderConstructor;
            EngineType = engineType;
            EngineConstructor = engineConstructor;
            IsAllowedMethod = isAllowedMethod;
        }
    }
}
