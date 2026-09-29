using OrganizationDirectory.Application.Administration;
using OrganizationDirectory.Domain;

namespace OrganizationDirectory.Tests.Support
{
    /// <summary>Shared organization-membership application-service test fixture.</summary>
    internal sealed record OrganizationMembershipServiceFixture(
        OrganizationMembershipAdministrationService Service,
        InMemoryOrganizationStore OrganizationStore,
        Organization Organization,
        TenantMembershipReference TenantMembership,
        TestOrganizationClock Clock);
}
