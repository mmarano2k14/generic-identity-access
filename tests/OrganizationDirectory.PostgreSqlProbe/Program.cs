using Npgsql;
using OrganizationDirectory.Application.Storage;
using OrganizationDirectory.Domain;
using OrganizationDirectory.Infrastructure.PostgreSql;

namespace OrganizationDirectory.PostgreSqlProbe
{
    public static class Program
    {
        public static async Task<int> Main()
        {
            var connectionString =
                Environment.GetEnvironmentVariable("IDENTITY_ACCESS_POSTGRES_DEFAULT") ??
                Environment.GetEnvironmentVariable("ORGANIZATION_DIRECTORY_POSTGRES_DEFAULT");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                Console.Error.WriteLine("IDENTITY_ACCESS_POSTGRES_DEFAULT is required for the shared-database persistence probe.");
                return 2;
            }

            await using var dataSource = NpgsqlDataSource.Create(connectionString);
            var tenant = await ResolveActiveTenantMembershipAsync(dataSource);
            if (tenant is null)
            {
                Console.Error.WriteLine("No active Identity Access tenant membership exists. Bootstrap Identity Access first.");
                return 3;
            }

            var store = new PostgreSqlOrganizationStore(dataSource);
            var membershipStore = new PostgreSqlOrganizationMembershipStore(dataSource);
            var tenantMembershipReader = new PostgreSqlTenantMembershipReferenceReader(dataSource);
            var identityScopeId = new IdentityScopeId(tenant.Value.IdentityScopeId);
            var tenantId = new TenantId(tenant.Value.TenantId);
            var tenantMembership = new TenantMembershipReference(
                identityScopeId,
                tenantId,
                new TenantMembershipId(tenant.Value.MembershipId));
            var rootReference = new OrganizationReference(identityScopeId, tenantId, new OrganizationId(Guid.NewGuid()));
            var childReference = new OrganizationReference(identityScopeId, tenantId, new OrganizationId(Guid.NewGuid()));
            var suffix = Guid.NewGuid().ToString("N")[..10];
            var now = DateTimeOffset.UtcNow;

            try
            {
                var root = new Organization(
                    rootReference,
                    new OrganizationKey($"probe-root-{suffix}"),
                    "Probe Root",
                    new OrganizationType("organization"),
                    null,
                    OrganizationStatus.Active,
                    0,
                    now,
                    now);

                var persistedRoot = await store.CreateAsync(root, CancellationToken.None);
                Require(persistedRoot.RowVersion == 1, "Created root row version must be 1.");

                var child = new Organization(
                    childReference,
                    new OrganizationKey($"probe-child-{suffix}"),
                    "Probe Child",
                    new OrganizationType("business"),
                    rootReference,
                    OrganizationStatus.Active,
                    0,
                    now,
                    now);

                var persistedChild = await store.CreateAsync(child, CancellationToken.None);
                Require(persistedChild.RowVersion == 1, "Created child row version must be 1.");

                var fetched = await store.GetAsync(childReference, CancellationToken.None);
                Require(fetched is not null && fetched.Key.Value == $"probe-child-{suffix}", "GetAsync did not return the child.");

                var byKey = await store.FindByKeyAsync(identityScopeId, tenantId, new OrganizationKey($"probe-child-{suffix}"), CancellationToken.None);
                Require(byKey is not null && byKey.Reference.OrganizationId == childReference.OrganizationId, "FindByKeyAsync returned the wrong organization.");

                var listed = await store.ListAsync(identityScopeId, tenantId, 0, 500, CancellationToken.None);
                Require(listed.Any(item => item.Reference.OrganizationId == rootReference.OrganizationId), "ListAsync did not return the root organization.");
                Require(listed.Any(item => item.Reference.OrganizationId == childReference.OrganizationId), "ListAsync did not return the child organization.");


                var tenantMembershipState = await tenantMembershipReader.GetAsync(
                    tenantMembership,
                    CancellationToken.None);
                Require(
                    tenantMembershipState is not null &&
                    tenantMembershipState.IsActive,
                    "Active Identity Access tenant membership was not resolved.");

                var organizationMembership = new OrganizationMembership(
                    childReference,
                    tenantMembership,
                    OrganizationMembershipStatus.Active,
                    0,
                    now,
                    now);

                var persistedMembership = await membershipStore.CreateAsync(
                    organizationMembership,
                    CancellationToken.None);
                Require(
                    persistedMembership.RowVersion == 1,
                    "Created organization membership row version must be 1.");

                var membershipByOrganization = await membershipStore.ListForOrganizationAsync(
                    childReference,
                    0,
                    100,
                    CancellationToken.None);
                Require(
                    membershipByOrganization.Any(item =>
                        item.TenantMembership == tenantMembership),
                    "Organization-centric membership read did not return the tenant member.");

                var membershipByMember = await membershipStore.ListForTenantMembershipAsync(
                    tenantMembership,
                    0,
                    100,
                    CancellationToken.None);
                Require(
                    membershipByMember.Any(item =>
                        item.Organization == childReference),
                    "Member-centric membership read did not return the organization.");

                var suspendedMembership = new OrganizationMembership(
                    persistedMembership.Organization,
                    persistedMembership.TenantMembership,
                    OrganizationMembershipStatus.Suspended,
                    persistedMembership.RowVersion,
                    persistedMembership.CreatedAt,
                    DateTimeOffset.UtcNow);

                var updatedMembership = await membershipStore.UpdateAsync(
                    suspendedMembership,
                    CancellationToken.None);
                Require(
                    updatedMembership is not null &&
                    updatedMembership.RowVersion == 2 &&
                    updatedMembership.Status == OrganizationMembershipStatus.Suspended,
                    "Organization membership status update did not persist.");

                await RequireExceptionAsync<OrganizationMembershipConcurrencyException>(
                    () => membershipStore.UpdateAsync(
                        suspendedMembership,
                        CancellationToken.None),
                    "A stale organization membership update was not rejected.");

                Require(
                    await membershipStore.DeleteAsync(
                        childReference,
                        tenantMembership,
                        updatedMembership!.RowVersion,
                        CancellationToken.None),
                    "Organization membership delete failed.");

                var changedChild = new Organization(
                    persistedChild.Reference,
                    persistedChild.Key,
                    "Probe Child Updated",
                    persistedChild.Type,
                    persistedChild.Parent,
                    persistedChild.Status,
                    persistedChild.RowVersion,
                    persistedChild.CreatedAt,
                    DateTimeOffset.UtcNow);

                var updatedChild = await store.UpdateAsync(changedChild, CancellationToken.None);
                Require(updatedChild is not null && updatedChild.RowVersion == 2, "UpdateAsync did not increment row version.");

                var staleChild = new Organization(
                    persistedChild.Reference,
                    persistedChild.Key,
                    "Stale Update",
                    persistedChild.Type,
                    persistedChild.Parent,
                    persistedChild.Status,
                    persistedChild.RowVersion,
                    persistedChild.CreatedAt,
                    DateTimeOffset.UtcNow);

                await RequireExceptionAsync<OrganizationConcurrencyException>(
                    () => store.UpdateAsync(staleChild, CancellationToken.None),
                    "A stale update was not rejected.");

                var cyclicRoot = new Organization(
                    persistedRoot.Reference,
                    persistedRoot.Key,
                    persistedRoot.DisplayName,
                    persistedRoot.Type,
                    childReference,
                    persistedRoot.Status,
                    persistedRoot.RowVersion,
                    persistedRoot.CreatedAt,
                    DateTimeOffset.UtcNow);

                await RequireExceptionAsync<OrganizationHierarchyConflictException>(
                    () => store.UpdateAsync(cyclicRoot, CancellationToken.None),
                    "A deep organization cycle was not rejected.");

                await RequireExceptionAsync<OrganizationHierarchyConflictException>(
                    () => store.DeleteAsync(persistedRoot.Reference, persistedRoot.RowVersion, CancellationToken.None),
                    "Deleting a parent with children was not rejected.");

                Require(await store.DeleteAsync(updatedChild!.Reference, updatedChild.RowVersion, CancellationToken.None), "Child delete failed.");
                Require(await store.DeleteAsync(persistedRoot.Reference, persistedRoot.RowVersion, CancellationToken.None), "Root delete failed.");

                Console.WriteLine("PostgreSQL organization store probe: GREEN");
                return 0;
            }
            finally
            {
                await using var cleanup = dataSource.CreateCommand(@"
                    DELETE FROM organization_directory.organization_memberships
                    WHERE identity_scope_id = @identity_scope_id
                      AND tenant_id = @tenant_id
                      AND organization_id = ANY(@organization_ids);

                    UPDATE organization_directory.organizations
                    SET parent_organization_id = NULL
                    WHERE identity_scope_id = @identity_scope_id
                      AND tenant_id = @tenant_id
                      AND organization_id = ANY(@organization_ids);

                    DELETE FROM organization_directory.organizations
                    WHERE identity_scope_id = @identity_scope_id
                      AND tenant_id = @tenant_id
                      AND organization_id = ANY(@organization_ids);");
                cleanup.Parameters.AddWithValue("identity_scope_id", identityScopeId.Value);
                cleanup.Parameters.AddWithValue("tenant_id", tenantId.Value);
                cleanup.Parameters.AddWithValue("organization_ids", new[] { rootReference.OrganizationId.Value, childReference.OrganizationId.Value });
                await cleanup.ExecuteNonQueryAsync();
            }
        }

        private static async Task<(Guid IdentityScopeId, Guid TenantId, Guid MembershipId)?> ResolveActiveTenantMembershipAsync(
            NpgsqlDataSource dataSource)
        {
            await using var command = dataSource.CreateCommand(@"
                SELECT
                    tm.identity_scope_id,
                    tm.tenant_id,
                    tm.membership_id
                FROM identity_access.tenant_memberships tm
                INNER JOIN identity_access.tenants t
                    ON t.identity_scope_id = tm.identity_scope_id
                   AND t.tenant_id = tm.tenant_id
                WHERE tm.status = 1
                  AND t.status = 1
                ORDER BY tm.created_at, tm.membership_id
                LIMIT 1;");

            await using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            return (
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetGuid(2));
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static async Task RequireExceptionAsync<TException>(Func<Task> action, string message)
            where TException : Exception
        {
            try { await action(); }
            catch (TException) { return; }
            throw new InvalidOperationException(message);
        }
    }
}
