using IdentityAccess.Domain;

namespace IdentityAccess.Application.Authorization
{
    /// <summary>
    /// Provenance-rich capability grant projected from active identity-scope administration group,
    /// policy, and statement state. Wildcard evaluation remains external.
    /// </summary>
    public sealed record AssignedIdentityScopeCapabilityGrant
    {
        /// <summary>Gets the identity scope.</summary>
        public Guid IdentityScopeId { get; }

        /// <summary>Gets the authenticated subject.</summary>
        public SubjectReference Subject { get; }

        /// <summary>Gets the application.</summary>
        public ApplicationKey Application { get; }

        /// <summary>Gets the authority-group identifier.</summary>
        public Guid GroupId { get; }

        /// <summary>Gets the authority-policy identifier.</summary>
        public Guid PolicyId { get; }

        /// <summary>Gets the statement identifier.</summary>
        public Guid StatementId { get; }

        /// <summary>Gets the pinned application security model.</summary>
        public ApplicationSecurityModelReference Model { get; }

        /// <summary>Gets the persisted capability pattern.</summary>
        public CapabilityPattern Pattern { get; }

        /// <summary>Initializes an identity-scope capability grant.</summary>
        public AssignedIdentityScopeCapabilityGrant(
            Guid identityScopeId,
            SubjectReference subject,
            ApplicationKey application,
            Guid groupId,
            Guid policyId,
            Guid statementId,
            ApplicationSecurityModelReference model,
            CapabilityPattern pattern)
        {
            if (identityScopeId == Guid.Empty)
            {
                throw new ArgumentException(
                    "An identity scope is required.",
                    nameof(identityScopeId));
            }

            ArgumentNullException.ThrowIfNull(subject);
            ArgumentNullException.ThrowIfNull(application);
            ArgumentNullException.ThrowIfNull(model);
            ArgumentNullException.ThrowIfNull(pattern);

            if (groupId == Guid.Empty)
                throw new ArgumentException("Group id must not be empty.", nameof(groupId));

            if (policyId == Guid.Empty)
                throw new ArgumentException("Policy id must not be empty.", nameof(policyId));

            if (statementId == Guid.Empty)
                throw new ArgumentException("Statement id must not be empty.", nameof(statementId));

            if (subject.IdentityScopeId != identityScopeId ||
                model.IdentityScopeId != identityScopeId)
            {
                throw new ArgumentException(
                    "Identity-scope grant provenance must remain inside one identity scope.");
            }

            if (model.Application != application)
            {
                throw new ArgumentException(
                    "Identity-scope grant provenance must remain inside one application.");
            }

            IdentityScopeId = identityScopeId;
            Subject = subject;
            Application = application;
            GroupId = groupId;
            PolicyId = policyId;
            StatementId = statementId;
            Model = model;
            Pattern = pattern;
        }
    }
}
