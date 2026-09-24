using System.ComponentModel.DataAnnotations;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Represents a recovery-code-backed password-reset request.</summary>
    public sealed record RecoveryPasswordResetRequest(
        string LoginIdentifier,
        [param: DataType(DataType.Password)] string RecoveryCode,
        [param: DataType(DataType.Password)] string NewPassword);
}
