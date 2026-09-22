using System.Reflection;
using IdentityAccess.Authorization;

namespace IdentityAccess.Tests.Architecture
{
    /// <summary>
    /// Protects public service constructors from leaking internal implementation types into the
    /// supported API surface.
    /// </summary>
    public sealed class PublicConstructorAccessibilityTests
    {
        /// <summary>
        /// Verifies public constructors in the authorization assembly expose only publicly
        /// accessible parameter types from that assembly.
        /// </summary>
        [Fact]
        public void Authorization_public_constructors_do_not_expose_internal_types()
        {
            var assembly = typeof(IIdentityAuthorizationService).Assembly;

            var violations = assembly
                .GetExportedTypes()
                .SelectMany(
                    type => type.GetConstructors(
                        BindingFlags.Public |
                        BindingFlags.Instance))
                .SelectMany(
                    constructor => constructor
                        .GetParameters()
                        .Where(
                            parameter =>
                                parameter.ParameterType.Assembly == assembly &&
                                !IsPubliclyAccessible(parameter.ParameterType))
                        .Select(
                            parameter =>
                                $"{constructor.DeclaringType!.FullName} -> {parameter.ParameterType.FullName}"))
                .ToArray();

            Assert.Empty(violations);
        }

        private static bool IsPubliclyAccessible(Type type)
        {
            if (type.IsGenericType)
            {
                return IsPubliclyAccessible(
                        type.GetGenericTypeDefinition()) &&
                    type.GetGenericArguments()
                        .All(IsPubliclyAccessible);
            }

            return type.IsPublic ||
                type.IsNestedPublic;
        }
    }
}
