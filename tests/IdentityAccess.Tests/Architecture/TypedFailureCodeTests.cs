using IdentityAccess.Application.Authentication;
using IdentityAccess.Authorization;
using IdentityAccess.Rbac;

namespace IdentityAccess.Tests.Architecture
{
    /// <summary>
    /// Protects internal failure contracts from regressing to free-form string codes.
    /// </summary>
    public sealed class TypedFailureCodeTests
    {
        /// <summary>
        /// Verifies that authorization and authentication result contracts expose typed
        /// failure-code properties rather than arbitrary strings.
        /// </summary>
        [Fact]
        public void Result_failure_codes_are_typed()
        {
            Assert.Equal(
                typeof(IdentityAuthorizationFailureCode?),
                typeof(IdentityAuthorizationResult).GetProperty(nameof(IdentityAuthorizationResult.FailureCode))!.PropertyType);

            Assert.Equal(
                typeof(RbacAuthorizationFailureCode?),
                typeof(RbacAuthorizationResult).GetProperty(nameof(RbacAuthorizationResult.FailureCode))!.PropertyType);

            Assert.Equal(
                typeof(AuthenticationFailureCode?),
                typeof(PasswordLoginResult).GetProperty(nameof(PasswordLoginResult.FailureCode))!.PropertyType);

            Assert.Equal(
                typeof(AuthenticationFailureCode?),
                typeof(LogoutResult).GetProperty(nameof(LogoutResult.FailureCode))!.PropertyType);
        }

        /// <summary>
        /// Verifies that the stable failure enums remain distinct by subsystem.
        /// </summary>
        [Fact]
        public void Failure_code_types_remain_semantically_separate()
        {
            Assert.NotEqual(typeof(IdentityAuthorizationFailureCode), typeof(RbacAuthorizationFailureCode));
            Assert.NotEqual(typeof(AuthenticationFailureCode), typeof(RbacAuthorizationFailureCode));
        }
    }
}
