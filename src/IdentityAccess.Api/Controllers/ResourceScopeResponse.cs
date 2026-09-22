using IdentityAccess.Application.Administration;
using IdentityAccess.Application.Storage;
using IdentityAccess.Domain;
using Microsoft.AspNetCore.Mvc;

namespace IdentityAccess.Api.Controllers
{

    /// <summary>Represents the response payload for resource scope.</summary>
    public sealed record ResourceScopeResponse(Guid ResourceScopeId, int ModelVersion, string ScopeType,
        string ExternalResourceId, string DisplayName, Guid? ParentResourceScopeId, ResourceScopeStatus Status, long Version)
    {
        /// <summary>Creates the response from the supplied domain or persistence record.</summary>
        public static ResourceScopeResponse From(VersionedRecord<ResourceScope> row) =>
            new(row.Value.Reference.ResourceScopeId, row.Value.ModelVersion, row.Value.Type.Value,
                row.Value.ExternalResourceId, row.Value.DisplayName, row.Value.ParentResourceScopeId,
                row.Value.Status, row.Version);
    }
}
