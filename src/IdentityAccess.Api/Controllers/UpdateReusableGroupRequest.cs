using IdentityAccess.Domain;

namespace IdentityAccess.Api.Controllers
{
    /// <summary>Updates a real group definition and its reusable-template marker.</summary>
    public sealed record UpdateReusableGroupRequest(
        string DisplayName,
        GroupStatus Status,
        bool IsTemplate,
        long ExpectedVersion);
}
