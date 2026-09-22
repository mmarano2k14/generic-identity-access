using System.Reflection;
using System.Security.Cryptography;
using IdentityAccess.Rbac;

namespace IdentityAccess.Rbac.MultiplexedAdapter
{
    /// <summary>
    /// Loads and validates the exact external type/member shape required by the adapter without
    /// exposing external types outside this integration assembly.
    /// </summary>
    internal static class MultiplexedRbacBindingLoader
    {
        private const string CoreAssemblyFile = "Multiplexed.Rbac.Core.dll";
        private const string AbstractionsAssemblyFile = "Multiplexed.Abstractions.dll";
        private const string CoreAssemblyName = "Multiplexed.Rbac.Core";
        private const string AbstractionsAssemblyName = "Multiplexed.Abstractions";

        /// <summary>Loads, validates, fingerprints, and pins one external distribution.</summary>
        public static MultiplexedRbacBindingResult Load(
            MultiplexedRbacAdapterOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            var corePath = Path.Combine(
                options.ReferenceDirectory,
                CoreAssemblyFile);

            var abstractionsPath = Path.Combine(
                options.ReferenceDirectory,
                AbstractionsAssemblyFile);

            if (!File.Exists(corePath) ||
                !File.Exists(abstractionsPath))
            {
                return Failure(
                    RbacAuthorizationFailureCode.ExternalBinariesMissing,
                    "ExternalDistributionIncomplete");
            }

            string? coreVersion = null;
            string? coreSha256 = null;
            string? abstractionsVersion = null;
            string? abstractionsSha256 = null;

            try
            {
                var coreName = AssemblyName.GetAssemblyName(corePath);
                var abstractionsName = AssemblyName.GetAssemblyName(abstractionsPath);

                coreVersion = coreName.Version?.ToString() ?? "unknown";
                abstractionsVersion = abstractionsName.Version?.ToString() ?? "unknown";
                coreSha256 = Sha256(corePath);
                abstractionsSha256 = Sha256(abstractionsPath);

                if (!string.Equals(
                        coreName.Name,
                        CoreAssemblyName,
                        StringComparison.Ordinal) ||
                    !string.Equals(
                        abstractionsName.Name,
                        AbstractionsAssemblyName,
                        StringComparison.Ordinal))
                {
                    return Failure(
                        RbacAuthorizationFailureCode.ExternalContractMismatch,
                        "ExternalAssemblyIdentityMismatch",
                        coreVersion,
                        coreSha256,
                        abstractionsVersion,
                        abstractionsSha256);
                }

                var abstractions = Assembly.LoadFrom(abstractionsPath);
                var core = Assembly.LoadFrom(corePath);

                EnsureLoadedFromExpectedPath(
                    abstractions,
                    abstractionsPath);

                EnsureLoadedFromExpectedPath(
                    core,
                    corePath);

                var optionsAssembly = Assembly.Load(
                    "Microsoft.Extensions.Options");

                var namespaceEntryType = RequiredType(
                    abstractions,
                    "Multiplexed.Abstractions.Core.ExecutionContext.NamespaceEntry");

                var namespaceEntryConstructor =
                    RequiredParameterlessConstructor(
                        namespaceEntryType);

                var namespaceEntryNameProperty =
                    RequiredWritableProperty(
                        namespaceEntryType,
                        "Name",
                        typeof(string));

                var namespaceEntryTrnsProperty =
                    RequiredWritableProperty(
                        namespaceEntryType,
                        "Trns");

                var namespaceTrnsAcceptsHashSet =
                    namespaceEntryTrnsProperty.PropertyType.IsAssignableFrom(
                        typeof(HashSet<string>));

                var namespaceTrnsAcceptsList =
                    namespaceEntryTrnsProperty.PropertyType.IsAssignableFrom(
                        typeof(List<string>));

                var namespaceTrnsEnumerableConstructor =
                    namespaceEntryTrnsProperty.PropertyType.GetConstructor(
                        [typeof(IEnumerable<string>)]);

                if (!namespaceTrnsAcceptsHashSet &&
                    !namespaceTrnsAcceptsList &&
                    namespaceTrnsEnumerableConstructor is null)
                {
                    throw new MultiplexedRbacContractException(
                        $"External string collection type is unsupported: {namespaceEntryTrnsProperty.DeclaringType?.FullName}.{namespaceEntryTrnsProperty.Name}");
                }

                var namespaceListType =
                    typeof(List<>).MakeGenericType(
                        namespaceEntryType);

                var contextType = RequiredType(
                    core,
                    "Multiplexed.Rbac.Core.ExecutionContext.ExecutionContext");

                var contextConstructor =
                    RequiredParameterlessConstructor(
                        contextType);

                var contextKeyProperty =
                    RequiredWritableProperty(
                        contextType,
                        "ContextKey",
                        typeof(string));

                var projectProperty =
                    RequiredWritableProperty(
                        contextType,
                        "Project",
                        typeof(string));

                var userIdProperty =
                    RequiredWritableProperty(
                        contextType,
                        "UserId",
                        typeof(string));

                var tenantIdProperty =
                    RequiredWritableProperty(
                        contextType,
                        "TenantId",
                        typeof(string));

                var tenantGroupIdProperty =
                    RequiredWritableProperty(
                        contextType,
                        "TenantGroupId",
                        typeof(string));

                var currentNamespaceProperty =
                    RequiredWritableProperty(
                        contextType,
                        "CurrentNamespace",
                        typeof(string));

                var namespacesProperty =
                    RequiredWritableProperty(
                        contextType,
                        "Namespaces");

                if (!namespacesProperty.PropertyType
                    .IsAssignableFrom(namespaceListType))
                {
                    throw new MultiplexedRbacContractException(
                        "ExecutionContext.Namespaces is incompatible with List<NamespaceEntry>.");
                }

                var inFlightCountProperty =
                    RequiredWritableProperty(
                        contextType,
                        "InFlightCount",
                        typeof(int));

                var ttlSecondsProperty =
                    RequiredWritableProperty(
                        contextType,
                        "TtlSeconds",
                        typeof(int));

                var accessorType = RequiredType(
                    core,
                    "Multiplexed.Rbac.Core.Runtime.ExecutionContextAccessor");

                var accessorConstructor =
                    RequiredParameterlessConstructor(
                        accessorType);

                var accessorSetMethod =
                    RequiredMethod(
                        accessorType,
                        "Set",
                        typeof(void),
                        contextType);

                var accessorClearMethod =
                    RequiredMethod(
                        accessorType,
                        "Clear",
                        typeof(void));

                var scopeType = RequiredType(
                    core,
                    "Multiplexed.Rbac.Core.Authorization.Scope.AuthorizationScope");

                var scopeConstructor =
                    RequiredParameterlessConstructor(
                        scopeType);

                var optionsType = RequiredType(
                    core,
                    "Multiplexed.Rbac.Core.Authorization.Trn.TrnBuilderOptions");

                var optionsConstructor =
                    RequiredParameterlessConstructor(
                        optionsType);

                var optionsProjectProperty =
                    RequiredWritableProperty(
                        optionsType,
                        "Project",
                        typeof(string));

                var wrapperOpenType = RequiredType(
                    optionsAssembly,
                    "Microsoft.Extensions.Options.OptionsWrapper`1");

                var optionsWrapperType =
                    wrapperOpenType.MakeGenericType(
                        optionsType);

                var optionsWrapperConstructor =
                    RequiredCompatibleConstructor(
                        optionsWrapperType,
                        optionsType);

                var builderType = RequiredType(
                    core,
                    "Multiplexed.Rbac.Core.Authorization.Trn.TrnBuilder");

                var builderConstructor =
                    RequiredCompatibleConstructor(
                        builderType,
                        optionsWrapperType);

                var engineType = RequiredType(
                    core,
                    "Multiplexed.Rbac.Core.Authorization.Engine.TrnAuthorizationEngine");

                var engineConstructor =
                    RequiredCompatibleConstructor(
                        engineType,
                        builderType,
                        scopeType,
                        accessorType);

                var isAllowedMethod =
                    RequiredMethod(
                        engineType,
                        "IsAllowed",
                        typeof(bool),
                        typeof(string),
                        typeof(string),
                        typeof(string));

                var binding = new MultiplexedRbacBinding(
                    namespaceEntryType,
                    namespaceEntryConstructor,
                    namespaceEntryNameProperty,
                    namespaceEntryTrnsProperty,
                    namespaceTrnsAcceptsHashSet,
                    namespaceTrnsAcceptsList,
                    namespaceTrnsEnumerableConstructor,
                    namespaceListType,
                    contextType,
                    contextConstructor,
                    contextKeyProperty,
                    projectProperty,
                    userIdProperty,
                    tenantIdProperty,
                    tenantGroupIdProperty,
                    currentNamespaceProperty,
                    namespacesProperty,
                    inFlightCountProperty,
                    ttlSecondsProperty,
                    accessorType,
                    accessorConstructor,
                    accessorSetMethod,
                    accessorClearMethod,
                    scopeType,
                    scopeConstructor,
                    optionsType,
                    optionsConstructor,
                    optionsProjectProperty,
                    optionsWrapperType,
                    optionsWrapperConstructor,
                    builderType,
                    builderConstructor,
                    engineType,
                    engineConstructor,
                    isAllowedMethod);

                return new MultiplexedRbacBindingResult(
                    binding,
                    new MultiplexedRbacCompatibilityReport(
                        true,
                        coreAssemblyVersion: coreVersion,
                        coreAssemblySha256: coreSha256,
                        abstractionsAssemblyVersion: abstractionsVersion,
                        abstractionsAssemblySha256: abstractionsSha256));
            }
            catch (MultiplexedRbacContractException error)
            {
                return Failure(
                    RbacAuthorizationFailureCode.ExternalContractMismatch,
                    error.Message,
                    coreVersion,
                    coreSha256,
                    abstractionsVersion,
                    abstractionsSha256);
            }
            catch (Exception error) when (
                error is BadImageFormatException or
                    FileLoadException or
                    FileNotFoundException or
                    TypeLoadException or
                    ReflectionTypeLoadException)
            {
                return Failure(
                    RbacAuthorizationFailureCode.ExternalLoadFailed,
                    error.GetType().Name,
                    coreVersion,
                    coreSha256,
                    abstractionsVersion,
                    abstractionsSha256);
            }
            catch (Exception error)
            {
                return Failure(
                    RbacAuthorizationFailureCode.ExternalContractMismatch,
                    error.GetType().Name,
                    coreVersion,
                    coreSha256,
                    abstractionsVersion,
                    abstractionsSha256);
            }
        }

        private static MultiplexedRbacBindingResult Failure(
            RbacAuthorizationFailureCode failureCode,
            string diagnosticDetail,
            string? coreVersion = null,
            string? coreSha256 = null,
            string? abstractionsVersion = null,
            string? abstractionsSha256 = null) =>
            new(
                null,
                new MultiplexedRbacCompatibilityReport(
                    false,
                    failureCode,
                    diagnosticDetail,
                    coreVersion,
                    coreSha256,
                    abstractionsVersion,
                    abstractionsSha256));

        private static string Sha256(
            string path)
        {
            using var stream = File.OpenRead(path);
            return Convert.ToHexString(
                SHA256.HashData(stream));
        }

        private static void EnsureLoadedFromExpectedPath(
            Assembly assembly,
            string expectedPath)
        {
            var comparison =
                OperatingSystem.IsWindows()
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal;

            var actual =
                string.IsNullOrWhiteSpace(
                    assembly.Location)
                    ? string.Empty
                    : Path.GetFullPath(
                        assembly.Location);

            var expected =
                Path.GetFullPath(
                    expectedPath);

            if (!string.Equals(
                    actual,
                    expected,
                    comparison))
            {
                throw new MultiplexedRbacContractException(
                    "External assembly load path does not match the configured distribution.");
            }
        }

        private static Type RequiredType(
            Assembly assembly,
            string name) =>
            assembly.GetType(
                name,
                throwOnError: false,
                ignoreCase: false)
            ?? throw new MultiplexedRbacContractException(
                $"Required external type is unavailable: {name}");

        private static ConstructorInfo RequiredParameterlessConstructor(
            Type type) =>
            type.GetConstructor(
                BindingFlags.Public | BindingFlags.Instance,
                binder: null,
                types: Type.EmptyTypes,
                modifiers: null)
            ?? throw new MultiplexedRbacContractException(
                $"Required public parameterless constructor is unavailable: {type.FullName}");

        private static ConstructorInfo RequiredCompatibleConstructor(
            Type type,
            params Type[] suppliedTypes)
        {
            var constructor = type
                .GetConstructors(
                    BindingFlags.Public |
                    BindingFlags.Instance)
                .SingleOrDefault(
                    candidate =>
                    {
                        var parameters =
                            candidate.GetParameters();

                        if (parameters.Length !=
                            suppliedTypes.Length)
                        {
                            return false;
                        }

                        return parameters
                            .Select(
                                parameter =>
                                    parameter.ParameterType)
                            .Zip(
                                suppliedTypes,
                                (parameterType, suppliedType) =>
                                    parameterType.IsAssignableFrom(
                                        suppliedType))
                            .All(value => value);
                    });

            return constructor
                ?? throw new MultiplexedRbacContractException(
                    $"Required compatible constructor is unavailable: {type.FullName}");
        }

        private static PropertyInfo RequiredWritableProperty(
            Type type,
            string propertyName,
            Type? expectedType = null)
        {
            var property = type.GetProperty(
                propertyName,
                BindingFlags.Public |
                BindingFlags.Instance)
                ?? throw new MultiplexedRbacContractException(
                    $"Required external property is unavailable: {type.FullName}.{propertyName}");

            if (!property.CanWrite)
            {
                throw new MultiplexedRbacContractException(
                    $"Required external property is not writable: {type.FullName}.{propertyName}");
            }

            if (expectedType is not null &&
                property.PropertyType != expectedType)
            {
                throw new MultiplexedRbacContractException(
                    $"Required external property type mismatch: {type.FullName}.{propertyName}");
            }

            return property;
        }

        private static MethodInfo RequiredMethod(
            Type type,
            string methodName,
            Type returnType,
            params Type[] parameterTypes)
        {
            var method = type.GetMethod(
                methodName,
                BindingFlags.Public |
                BindingFlags.Instance,
                binder: null,
                types: parameterTypes,
                modifiers: null)
                ?? throw new MultiplexedRbacContractException(
                    $"Required external method is unavailable: {type.FullName}.{methodName}");

            if (method.ReturnType != returnType)
            {
                throw new MultiplexedRbacContractException(
                    $"Required external method return type mismatch: {type.FullName}.{methodName}");
            }

            return method;
        }


    }
}
