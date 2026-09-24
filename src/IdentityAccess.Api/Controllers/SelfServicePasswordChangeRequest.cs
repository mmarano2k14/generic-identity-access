using System.ComponentModel.DataAnnotations;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Represents an authenticated self-service password-change request.</summary>
    public sealed record SelfServicePasswordChangeRequest(
        [param: DataType(DataType.Password)] string CurrentPassword,
        [param: DataType(DataType.Password)] string NewPassword);
}
