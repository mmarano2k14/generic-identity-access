using IdentityAccess.Application.Authentication;
using IdentityAccess.Application.Authentication.Mfa;
using IdentityAccess.Application.Authorization;
using IdentityAccess.Application.Storage;
using IdentityAccess.Application.Security;
using IdentityAccess.Infrastructure.PostgreSql.Authentication;
using IdentityAccess.Infrastructure.PostgreSql.Directory;
using IdentityAccess.Infrastructure.PostgreSql.Secrets;
using IdentityAccess.Infrastructure.PostgreSql.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityAccess.Infrastructure.PostgreSql
{
    /// <summary>
    /// Registers PostgreSQL persistence, routing-secret resolution, connection management, and
    /// storage contracts from trusted server configuration.
    /// </summary>
    public static class PostgreSqlServiceCollectionExtensions
    {
        /// <summary>
        /// Adds PostgreSQL persistence when the supplied configuration explicitly enables it.
        /// </summary>
        /// <param name="services">The server service collection.</param>
        /// <param name="section">The <c>IdentityAccess:PostgreSql</c> configuration section.</param>
        /// <returns>The original service collection.</returns>
        public static IServiceCollection AddIdentityAccessPostgreSql(
            this IServiceCollection services,
            IConfigurationSection section)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(section);

            var enabledValue = section["Enabled"];
            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Enabled",
                "MinimumPoolSize",
                "MaximumPoolSize",
                "ConnectionTimeoutSeconds",
                "CommandTimeoutSeconds"
            };

            if (section.GetChildren().Any(child => !allowed.Contains(child.Key)))
            {
                throw new PostgreSqlStorageException(
                    PostgreSqlStorageFailure.InvalidConfiguration);
            }

            if (enabledValue is null or "false")
            {
                if (section.GetChildren().Any(
                    child => !string.Equals(
                        child.Key,
                        "Enabled",
                        StringComparison.OrdinalIgnoreCase)))
                {
                    throw new PostgreSqlStorageException(
                        PostgreSqlStorageFailure.InvalidConfiguration);
                }

                return services;
            }

            if (!string.Equals(enabledValue, "true", StringComparison.Ordinal))
            {
                throw new PostgreSqlStorageException(
                    PostgreSqlStorageFailure.InvalidConfiguration);
            }

            var options = new PostgreSqlStorageOptions
            {
                MinimumPoolSize = ReadInt(
                    section,
                    "MinimumPoolSize",
                    PostgreSqlStorageOptions.DefaultMinimumPoolSize),
                MaximumPoolSize = ReadInt(
                    section,
                    "MaximumPoolSize",
                    PostgreSqlStorageOptions.DefaultMaximumPoolSize),
                ConnectionTimeoutSeconds = ReadInt(
                    section,
                    "ConnectionTimeoutSeconds",
                    PostgreSqlStorageOptions.DefaultConnectionTimeoutSeconds),
                CommandTimeoutSeconds = ReadInt(
                    section,
                    "CommandTimeoutSeconds",
                    PostgreSqlStorageOptions.DefaultCommandTimeoutSeconds)
            };

            options.Validate();

            services.AddSingleton(options);
            services.AddSingleton<IConnectionSecretResolver, EnvironmentConnectionSecretResolver>();
            services.AddSingleton<PostgreSqlConnectionFactory>();
            services.AddSingleton<IIdentityDatabaseConnectionFactory>(
                provider => provider.GetRequiredService<PostgreSqlConnectionFactory>());
            services.AddSingleton<IIdentitySchemaMigrator, PostgreSqlSchemaMigrator>();
            services.AddSingleton<IUserDirectoryStore, PostgreSqlUserDirectoryStore>();
            services.AddSingleton<ITenantDirectoryStore, PostgreSqlTenantDirectoryStore>();
            services.AddSingleton<ITenantMembershipStore, PostgreSqlTenantMembershipStore>();
            services.AddSingleton<IUserGroupStore, PostgreSqlUserGroupStore>();
            services.AddSingleton<IGroupMembershipStore, PostgreSqlGroupMembershipStore>();
            services.AddSingleton<IGroupMembershipMutationStore, PostgreSqlGroupMembershipMutationStore>();
            services.AddSingleton<IApplicationSecurityModelStore, PostgreSqlApplicationSecurityModelStore>();
            services.AddSingleton<IPermissionPolicyStore, PostgreSqlPermissionPolicyStore>();
            services.AddSingleton<IPolicyStatementStore, PostgreSqlPolicyStatementStore>();
            services.AddSingleton<IGroupPolicyBindingStore, PostgreSqlGroupPolicyBindingStore>();
            services.AddSingleton<IGroupPolicyBindingMutationStore, PostgreSqlGroupPolicyBindingMutationStore>();
            services.AddSingleton<IApplicationScopeTypeStore, PostgreSqlApplicationScopeTypeStore>();
            services.AddSingleton<IResourceScopeStore, PostgreSqlResourceScopeStore>();
            services.AddSingleton<IAssignedCapabilityReader, PostgreSqlAssignedCapabilityReader>();
            services.AddSingleton<IIdentityScopeAssignedCapabilityReader, PostgreSqlIdentityScopeAssignedCapabilityReader>();
            services.AddSingleton<IIdentityScopeAdministrationGroupStore, PostgreSqlIdentityScopeAdministrationGroupStore>();
            services.AddSingleton<IIdentityScopeAdministrationMembershipStore, PostgreSqlIdentityScopeAdministrationMembershipStore>();
            services.AddSingleton<IIdentityScopeAdministrationPolicyStore, PostgreSqlIdentityScopeAdministrationPolicyStore>();
            services.AddSingleton<IIdentityScopeAdministrationPolicyStatementStore, PostgreSqlIdentityScopeAdministrationPolicyStatementStore>();
            services.AddSingleton<IIdentityScopeAdministrationBindingStore, PostgreSqlIdentityScopeAdministrationBindingStore>();
            services.AddSingleton<IPasswordCredentialStore, PostgreSqlPasswordCredentialStore>();
            services.AddSingleton<ICredentialMutationStore, PostgreSqlCredentialMutationStore>();
            services.AddSingleton<IAuthenticationSessionStore, PostgreSqlAuthenticationSessionStore>();
            services.AddSingleton<IOidcAuthorizationCodeStore, PostgreSqlOidcAuthorizationCodeStore>();
            services.AddSingleton<IOidcRefreshTokenStore, PostgreSqlOidcRefreshTokenStore>();
            services.AddSingleton<IMfaPolicyStore, PostgreSqlMfaPolicyStore>();
            services.AddSingleton<IUserAuthenticatorStore, PostgreSqlUserAuthenticatorStore>();
            services.AddSingleton<ISecurityAuditWriter, PostgreSqlSecurityAuditWriter>();

            return services;
        }

        private static int ReadInt(
            IConfigurationSection section,
            string key,
            int fallback)
        {
            var value = section[key];

            if (value is null)
            {
                return fallback;
            }

            if (!int.TryParse(value, out var parsed))
            {
                throw new PostgreSqlStorageException(
                    PostgreSqlStorageFailure.InvalidConfiguration);
            }

            return parsed;
        }
    }
}
