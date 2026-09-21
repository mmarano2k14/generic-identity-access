namespace IdentityAccess.Domain;

/// <summary>Structural membership state, not a current authorization decision.</summary>
public sealed class TenantMembership
{
    public Guid MembershipId { get; }
    public TenantReference Tenant { get; }
    public SubjectReference Subject { get; }
    public MembershipStatus Status { get; }

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
