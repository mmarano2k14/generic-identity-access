using IdentityAccess.Domain;

namespace IdentityAccess.Application.Routing;

/// <summary>Server-side placement request. Construction does not authenticate or authorize a caller.</summary>
public sealed record DatabaseRouteRequest
{
    public ApplicationKey Application { get; }
    public Guid IdentityScopeId { get; }
    public IdentityDataSet DataSet { get; }

    public DatabaseRouteRequest(ApplicationKey application, Guid identityScopeId,
        IdentityDataSet dataSet = IdentityDataSet.IdentityDirectory)
    {
        ArgumentNullException.ThrowIfNull(application);
        if (identityScopeId == Guid.Empty)
            throw new ArgumentException("An identity scope is required.", nameof(identityScopeId));
        if (!Enum.IsDefined(dataSet))
            throw new ArgumentOutOfRangeException(nameof(dataSet), "The data set is not supported.");
        Application = application;
        IdentityScopeId = identityScopeId;
        DataSet = dataSet;
    }
}
