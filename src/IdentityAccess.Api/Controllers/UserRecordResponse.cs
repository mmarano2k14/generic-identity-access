using IdentityAccess.Application.Administration;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAccess.Api.Controllers
{

    /// <summary>Represents the response payload for user record.</summary>
    public sealed record UserRecordResponse(Guid UserId, string DisplayName, UserStatus Status, long Version)
    {
        /// <summary>Creates the response from the supplied domain or persistence record.</summary>
        public static UserRecordResponse From(VersionedRecord<User> record) =>
            new(record.Value.Subject.UserId, record.Value.DisplayName, record.Value.Status, record.Version);
    }
}
