using System.ComponentModel.DataAnnotations;
using System.Reflection;
using IdentityAccess.Api.Controllers;

namespace IdentityAccess.Tests.Api
{
    public sealed class RecordRequestValidationMetadataTests
    {
        [Theory]
        [InlineData(typeof(PasswordLoginRequest))]
        [InlineData(typeof(CreatePasswordCredentialRequest))]
        [InlineData(typeof(ChangePasswordRequest))]
        public void Password_validation_metadata_is_attached_to_record_constructor_parameter(Type requestType)
        {
            var constructor = Assert.Single(requestType.GetConstructors(BindingFlags.Instance | BindingFlags.Public));
            var passwordParameter = Assert.Single(constructor.GetParameters(), parameter => parameter.Name == "Password");
            var passwordProperty = requestType.GetProperty("Password", BindingFlags.Instance | BindingFlags.Public);

            Assert.NotNull(passwordProperty);
            Assert.Contains(passwordParameter.GetCustomAttributes(inherit: true), attribute => attribute is DataTypeAttribute);
            Assert.DoesNotContain(passwordProperty.GetCustomAttributes(inherit: true), attribute => attribute is ValidationAttribute);
        }

        [Theory]
        [InlineData(typeof(SelfServicePasswordChangeRequest), "CurrentPassword")]
        [InlineData(typeof(SelfServicePasswordChangeRequest), "NewPassword")]
        [InlineData(typeof(RecoveryPasswordResetRequest), "RecoveryCode")]
        [InlineData(typeof(RecoveryPasswordResetRequest), "NewPassword")]
        public void Sensitive_credential_validation_metadata_is_attached_to_record_constructor_parameter(
            Type requestType,
            string parameterName)
        {
            var constructor = Assert.Single(requestType.GetConstructors(BindingFlags.Instance | BindingFlags.Public));
            var parameter = Assert.Single(
                constructor.GetParameters(),
                candidate => string.Equals(candidate.Name, parameterName, StringComparison.OrdinalIgnoreCase));
            var property = requestType.GetProperty(parameterName, BindingFlags.Instance | BindingFlags.Public);

            Assert.NotNull(property);
            Assert.Contains(parameter.GetCustomAttributes(inherit: true), attribute => attribute is DataTypeAttribute);
            Assert.DoesNotContain(property.GetCustomAttributes(inherit: true), attribute => attribute is ValidationAttribute);
        }
    }
}
