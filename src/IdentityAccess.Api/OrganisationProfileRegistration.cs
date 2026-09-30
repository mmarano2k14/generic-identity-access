using IdentityAccess.Api.Features;
using IdentityAccess.Api.OrganisationProfiles;
using IdentityAccess.Api.Security;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using OrganisationProfile.Application;
using OrganisationProfile.Application.Composition;
using OrganisationProfile.Application.Registry;
using OrganisationProfile.Application.Storage;
using OrganisationProfile.Application.Templates;
using OrganisationProfile.Infrastructure.PostgreSql;

namespace IdentityAccess.Api
{
    /// <summary>
    /// Registers OrganisationProfile inside the existing Identity Access ASP.NET Core host.
    /// </summary>
    internal static class OrganisationProfileRegistration
    {
        /// <summary>
        /// Registers generic OrganisationProfile services against the shared PostgreSQL deployment.
        /// </summary>
        public static void AddOrganisationProfile(
            this WebApplicationBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.Services.AddSingleton<
                IOrganisationProfileSecurityAuditWriter,
                OrganisationProfileSecurityAuditWriter>();

            builder.Services.TryAddSingleton<
                IDomainRegistryReader,
                UnavailableDomainRegistryReader>();

            var connectionString =
                Environment.GetEnvironmentVariable(
                    "IDENTITY_ACCESS_POSTGRES_DEFAULT")
                ?? Environment.GetEnvironmentVariable(
                    "ORGANISATION_PROFILE_POSTGRES_DEFAULT");

            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                builder.Services.TryAddSingleton(
                    _ => NpgsqlDataSource.Create(connectionString));

                builder.Services.AddSingleton<
                    IOrganizationReferenceReader,
                    PostgreSqlOrganizationReferenceReader>();

                builder.Services.AddSingleton<
                    IOrganisationProfileStore,
                    PostgreSqlOrganisationProfileStore>();

                builder.Services.AddSingleton<
                    IOrganisationProfileTemplateStore,
                    PostgreSqlOrganisationProfileTemplateStore>();

                builder.Services.AddSingleton<
                    IOrganisationProfileTemplateVersionStore,
                    PostgreSqlOrganisationProfileTemplateVersionStore>();

                builder.Services.AddSingleton<
                    IOrganisationProfileDomainOverrideStore,
                    PostgreSqlOrganisationProfileDomainOverrideStore>();

                builder.Services.AddSingleton<
                    IOrganisationProfileVersionStore,
                    PostgreSqlOrganisationProfileVersionStore>();

                builder.Services.AddSingleton<
                    IOrganisationProfileClock,
                    SystemOrganisationProfileClock>();

                builder.Services.AddSingleton<
                    OrganisationProfileDomainRegistryValidator>();

                builder.Services.AddSingleton<
                    OrganisationProfileTemplateContentHasher>();

                builder.Services.AddSingleton<
                    OrganisationProfileEffectiveContentHasher>();

                builder.Services.AddSingleton<
                    OrganisationProfileCompositionResolver>();

                builder.Services.AddSingleton<
                    OrganisationProfileDefinitionService>();

                builder.Services.AddSingleton<
                    OrganisationProfileTemplateDefinitionService>();

                builder.Services.AddSingleton<
                    OrganisationProfileTemplateDraftService>();

                builder.Services.AddSingleton<
                    OrganisationProfileTemplatePublicationService>();

                builder.Services.AddSingleton<
                    OrganisationProfileDomainOverrideService>();

                builder.Services.AddSingleton<
                    OrganisationProfileCompositionService>();
            }

            RegisterOptional<IOrganisationProfileStore>(builder.Services);
            RegisterOptional<IOrganisationProfileTemplateStore>(builder.Services);
            RegisterOptional<IOrganisationProfileTemplateVersionStore>(builder.Services);
            RegisterOptional<IOrganisationProfileDomainOverrideStore>(builder.Services);
            RegisterOptional<IOrganisationProfileVersionStore>(builder.Services);
            RegisterOptional<OrganisationProfileDefinitionService>(builder.Services);
            RegisterOptional<OrganisationProfileTemplateDefinitionService>(builder.Services);
            RegisterOptional<OrganisationProfileTemplateDraftService>(builder.Services);
            RegisterOptional<OrganisationProfileTemplatePublicationService>(builder.Services);
            RegisterOptional<OrganisationProfileDomainOverrideService>(builder.Services);
            RegisterOptional<OrganisationProfileCompositionService>(builder.Services);
        }

        private static void RegisterOptional<TService>(
            IServiceCollection services)
            where TService : class
        {
            services.AddSingleton(provider =>
                new OptionalFeature<TService>(
                    provider.GetService<TService>()));
        }
    }
}
