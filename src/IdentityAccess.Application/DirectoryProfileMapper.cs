using IdentityAccess.Contracts;
using IdentityAccess.Domain;

namespace IdentityAccess.Application;

/// <summary>Profile projection only. Callers must authorize access before using or returning a profile.</summary>
public static class DirectoryProfileMapper
{
    public static UserProfileResponse ToProfile(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return new(user.Subject.IdentityScopeId, user.Subject.UserId, user.DisplayName,
            user.Status == UserStatus.Active ? "active" : "suspended");
    }

    public static TenantProfileResponse ToProfile(Tenant tenant)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        return new(tenant.Reference.IdentityScopeId, tenant.Reference.TenantId, tenant.DisplayName,
            tenant.Status == TenantStatus.Active ? "active" : "suspended");
    }

    public static GroupProfileResponse ToProfile(UserGroup group)
    {
        ArgumentNullException.ThrowIfNull(group);
        return new(group.Reference.Tenant.IdentityScopeId, group.Reference.Tenant.TenantId,
            group.Reference.Application.Value, group.Reference.GroupId, group.DisplayName,
            group.Status == GroupStatus.Active ? "active" : "suspended");
    }
}
