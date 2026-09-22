

namespace IdentityAccess.Domain
{

    /// <summary>Structural membership state, not a current authorization decision.</summary>
    public sealed class TenantMembership
    {
        /// <summary>Gets the membership identifier.</summary>
        public Guid MembershipId { get; }
        /// <summary>Gets the tenant.</summary>
        public TenantReference Tenant { get; }
        /// <summary>Gets the subject.</summary>
        public SubjectReference Subject { get; }
        /// <summary>Gets the status.</summary>
        public MembershipStatus Status { get; }

        /// <summary>Initializes a new instance of <see cref="TenantMembership"/>.</summary>
        public TenantMembership(Guid membershipId, TenantReference tenant, SubjectReference subject,
            MembershipStatus status = MembershipStatus.Active)
        {
            ArgumentNullException.ThrowIfNull(tenant);
            ArgumentNullException.ThrowIfNull(subject);
            if (tenant.IdentityScopeId != subject.IdentityScopeId)
                throw new ArgumentException("Tenant and subject must belong to the same identity scope.", nameof(subject));
            MembershipId = ModelGuard.Identifier(membershipId, nameof(membershipId));
            Tenant = tenant;
            Subject = subject;
            Status = ModelGuard.DefinedEnum(status, nameof(status));
        }
    }
}
