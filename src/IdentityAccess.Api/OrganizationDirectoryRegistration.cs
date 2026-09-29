using IdentityAccess.Api.Features;
using IdentityAccess.Api.Security;
using Npgsql;
using OrganizationDirectory.Application;
using OrganizationDirectory.Application.Administration;
using OrganizationDirectory.Application.Storage;
using OrganizationDirectory.Infrastructure.PostgreSql;

namespace IdentityAccess.Api
{
    /// <summary>Registers Organization Directory services inside the Identity Access host.</summary>
    internal static class OrganizationDirectoryRegistration
    {
        /// <summary>Registers the Organization Directory application service when shared PostgreSQL is configured.</summary>
        public static void AddOrganizationDirectory(this WebApplicationBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.Services.AddSingleton<IOrganizationSecurityAuditWriter, OrganizationSecurityAuditWriter>();

            var connectionString =
                Environment.GetEnvironmentVariable("IDENTITY_ACCESS_POSTGRES_DEFAULT") ??
                Environment.GetEnvironmentVariable("ORGANIZATION_DIRECTORY_POSTGRES_DEFAULT");

            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                builder.Services.AddSingleton(_ => NpgsqlDataSource.Create(connectionString));
                builder.Services.AddSingleton<IOrganizationStore, PostgreSqlOrganizationStore>();
                builder.Services.AddSingleton<IOrganizationMembershipStore, PostgreSqlOrganizationMembershipStore>();
                builder.Services.AddSingleton<ITenantMembershipReferenceReader, PostgreSqlTenantMembershipReferenceReader>();
                builder.Services.AddSingleton<IOrganizationResourceScopeLinkStore, PostgreSqlOrganizationResourceScopeLinkStore>();
                builder.Services.AddSingleton<IResourceScopeReferenceReader, PostgreSqlResourceScopeReferenceReader>();
                builder.Services.AddSingleton<IOrganizationClock, SystemOrganizationClock>();
                builder.Services.AddSingleton<IOrganizationAdministrationService, OrganizationAdministrationService>();
                builder.Services.AddSingleton<IOrganizationMembershipAdministrationService, OrganizationMembershipAdministrationService>();
                builder.Services.AddSingleton<IOrganizationResourceScopeLinkAdministrationService, OrganizationResourceScopeLinkAdministrationService>();
            }

            builder.Services.AddSingleton(provider =>
                new OptionalFeature<IOrganizationAdministrationService>(
                    provider.GetService<IOrganizationAdministrationService>()));

            builder.Services.AddSingleton(provider =>
                new OptionalFeature<IOrganizationMembershipAdministrationService>(
                    provider.GetService<IOrganizationMembershipAdministrationService>()));

            builder.Services.AddSingleton(provider =>
                new OptionalFeature<IOrganizationResourceScopeLinkAdministrationService>(
                    provider.GetService<IOrganizationResourceScopeLinkAdministrationService>()));
        }
    }
}
