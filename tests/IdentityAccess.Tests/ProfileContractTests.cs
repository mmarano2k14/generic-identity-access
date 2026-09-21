using IdentityAccess.Application;
using IdentityAccess.Contracts;
using IdentityAccess.Domain;

namespace IdentityAccess.Tests;

public sealed class ProfileContractTests
{
    [Fact]
    public void User_projection_preserves_identity_and_explicit_status()
    {
        var user = new User(new(Guid.NewGuid(), Guid.NewGuid()), "  User  ", UserStatus.Suspended);
        var profile = DirectoryProfileMapper.ToProfile(user);
        Assert.Equal(user.Subject.IdentityScopeId, profile.IdentityScopeId);
        Assert.Equal(user.Subject.UserId, profile.UserId);
        Assert.Equal("User", profile.DisplayName);
        Assert.Equal("suspended", profile.Status);
    }

    [Fact]
    public void Tenant_projection_preserves_scope()
    {
        var tenant = new Tenant(new(Guid.NewGuid(), Guid.NewGuid()), "Tenant");
        var profile = DirectoryProfileMapper.ToProfile(tenant);
        Assert.Equal(tenant.Reference.IdentityScopeId, profile.IdentityScopeId);
        Assert.Equal(tenant.Reference.TenantId, profile.TenantId);
        Assert.Equal("active", profile.Status);
    }

    [Fact]
    public void Group_projection_preserves_application_and_tenant()
    {
        var reference = new GroupReference(new(Guid.NewGuid(), Guid.NewGuid()), new("runtime-console"), Guid.NewGuid());
        var profile = DirectoryProfileMapper.ToProfile(new UserGroup(reference, "Observers"));
        Assert.Equal(reference.Tenant.IdentityScopeId, profile.IdentityScopeId);
        Assert.Equal(reference.Tenant.TenantId, profile.TenantId);
        Assert.Equal("runtime-console", profile.ApplicationKey);
        Assert.Equal(reference.GroupId, profile.GroupId);
    }

    [Fact]
    public void Profile_contracts_exclude_security_and_storage_fields()
    {
        string[] forbidden = ["Password", "PasswordHash", "AccessToken", "RefreshToken", "MfaSecret", "ConnectionString", "DestinationKey", "DatabaseName", "RouteKey"];
        Type[] profiles = [typeof(UserProfileResponse), typeof(TenantProfileResponse), typeof(GroupProfileResponse)];
        foreach (var profile in profiles)
            foreach (var property in profile.GetProperties())
                Assert.DoesNotContain(property.Name, forbidden);
    }

    [Fact]
    public void Foundation_does_not_claim_storage_or_security_is_ready()
    {
        var status = new FoundationStatus();
        var info = status.Describe();
        Assert.False(info.StorageConfigured);
        Assert.False(info.AuthenticationConfigured);
        Assert.False(info.AuthorizationConfigured);
        Assert.False(status.Readiness().Ready);
        Assert.Contains("rbac-integration", status.Readiness().BlockingCapabilities);
    }
}
