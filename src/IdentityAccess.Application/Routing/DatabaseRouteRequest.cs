using IdentityAccess.Domain;

namespace IdentityAccess.Application.Routing
{

    /// <summary>Server-side placement request. Construction does not authenticate or authorize a caller.</summary>
    public sealed record DatabaseRouteRequest
    {
        /// <summary>Gets the application.</summary>
        public ApplicationKey Application { get; }
        /// <summary>Gets the identity scope identifier.</summary>
        public Guid IdentityScopeId { get; }
        /// <summary>Gets the data set.</summary>
        public IdentityDataSet DataSet { get; }

        /// <summary>Initializes a new instance of <see cref="DatabaseRouteRequest"/>.</summary>
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
}
