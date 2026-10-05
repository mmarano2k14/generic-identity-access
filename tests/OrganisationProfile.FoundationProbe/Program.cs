using OrganisationProfile.Domain;
using OrganisationProfileAggregate = global::OrganisationProfile.Domain.OrganisationProfile;

namespace OrganisationProfile.FoundationProbe
{
    /// <summary>Executable Foundation contract probe with no external test-framework dependency.</summary>
    internal static class Program
    {
        private static int Main()
        {
            try
            {
                VerifyIdentityAndKeys();
                VerifyProfileLifecycle();
                VerifyPublishedTemplateVersion();
                VerifyDomainOverrideSemantics();
                VerifyEffectiveProfileDeterminism();

                Console.WriteLine(
                    "OrganisationProfile foundation probe: GREEN");
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception);
                return 1;
            }
        }

        private static void VerifyIdentityAndKeys()
        {
            RequireException<ArgumentException>(
                () => new OrganisationProfileId(Guid.Empty),
                "Empty OrganisationProfileId must be rejected.");

            RequireException<ArgumentException>(
                () => new OrganizationReference(
                    Guid.Empty,
                    Guid.NewGuid(),
                    Guid.NewGuid()),
                "Empty IdentityScopeId must be rejected.");

            RequireException<ArgumentException>(
                () => new OrganisationProfileTemplateKey("Ecommerce"),
                "Uppercase template keys must be rejected.");

            RequireException<ArgumentException>(
                () => new DomainKey("inventory.v2"),
                "Domain keys outside stable slug grammar must be rejected.");

            RequireException<ArgumentOutOfRangeException>(
                () => new DomainVersion(0),
                "DomainVersion must be positive.");
        }

        private static void VerifyProfileLifecycle()
        {
            var now = new DateTimeOffset(
                2026,
                9,
                29,
                12,
                0,
                0,
                TimeSpan.Zero);

            var organization = Organization();
            var pin = new OrganisationProfileTemplatePin(
                new OrganisationProfileTemplateKey("ecommerce-standard"),
                new OrganisationProfileTemplateVersionNumber(3));

            var profile = OrganisationProfileAggregate.Create(
                OrganisationProfileId.New(),
                organization,
                pin,
                now);

            Require(profile.Status == OrganisationProfileStatus.Active,
                "New OrganisationProfile must be active.");

            Require(profile.RowVersion == 0,
                "New OrganisationProfile must use row version zero before persistence.");

            var disabled = profile.WithStatus(
                OrganisationProfileStatus.Disabled,
                now.AddMinutes(1));

            Require(disabled.Status == OrganisationProfileStatus.Disabled,
                "Lifecycle status update failed.");

            Require(disabled.Organization == organization,
                "Profile lifecycle cannot change Organization identity.");

            RequireException<ArgumentException>(
                () => new OrganisationProfileAggregate(
                    profile.OrganisationProfileId,
                    organization,
                    pin,
                    OrganisationProfileStatus.Active,
                    1,
                    now,
                    now.AddMinutes(-1)),
                "UpdatedAt before CreatedAt must be rejected.");
        }

        private static void VerifyPublishedTemplateVersion()
        {
            var domains = new[]
            {
                new OrganisationProfileDomainSelection(
                    new DomainKey("inventory"),
                    new DomainVersion(3)),
                new OrganisationProfileDomainSelection(
                    new DomainKey("commerce"),
                    new DomainVersion(4))
            };

            var published =
                new PublishedOrganisationProfileTemplateVersion(
                    new OrganisationProfileTemplateKey("ecommerce-standard"),
                    new OrganisationProfileTemplateVersionNumber(3),
                    domains,
                    Hash('a'),
                    DateTimeOffset.UtcNow);

            Require(
                published.Domains.Select(item => item.DomainKey.Value)
                    .SequenceEqual(new[] { "commerce", "inventory" }),
                "Published domains must be deterministically key ordered.");

            RequireException<ArgumentException>(
                () => new PublishedOrganisationProfileTemplateVersion(
                    new OrganisationProfileTemplateKey("ecommerce-standard"),
                    new OrganisationProfileTemplateVersionNumber(4),
                    new[]
                    {
                        new OrganisationProfileDomainSelection(
                            new DomainKey("finance"),
                            new DomainVersion(5)),
                        new OrganisationProfileDomainSelection(
                            new DomainKey("finance"),
                            new DomainVersion(6))
                    },
                    Hash('b'),
                    DateTimeOffset.UtcNow),
                "Duplicate DomainKey entries must be rejected.");
        }

        private static void VerifyDomainOverrideSemantics()
        {
            _ = new OrganisationProfileDomainOverride(
                new DomainKey("manufacturing"),
                new DomainVersion(2),
                OrganisationProfileDomainOverrideOperation.Enable);

            _ = new OrganisationProfileDomainOverride(
                new DomainKey("growth"),
                null,
                OrganisationProfileDomainOverrideOperation.Disable);

            RequireException<ArgumentException>(
                () => new OrganisationProfileDomainOverride(
                    new DomainKey("manufacturing"),
                    null,
                    OrganisationProfileDomainOverrideOperation.Enable),
                "Enable override without DomainVersion must be rejected.");

            RequireException<ArgumentException>(
                () => new OrganisationProfileDomainOverride(
                    new DomainKey("growth"),
                    new DomainVersion(2),
                    OrganisationProfileDomainOverrideOperation.Disable),
                "Disable override carrying DomainVersion must be rejected.");
        }

        private static void VerifyEffectiveProfileDeterminism()
        {
            var profileId = OrganisationProfileId.New();
            var effective = new EffectiveOrganisationProfile(
                profileId,
                Organization(),
                new OrganisationProfileVersionNumber(17),
                new OrganisationProfileTemplatePin(
                    new OrganisationProfileTemplateKey("restaurant-standard"),
                    new OrganisationProfileTemplateVersionNumber(2)),
                new[]
                {
                    new OrganisationProfileDomainSelection(
                        new DomainKey("scheduling"),
                        new DomainVersion(2)),
                    new OrganisationProfileDomainSelection(
                        new DomainKey("hospitality"),
                        new DomainVersion(4)),
                    new OrganisationProfileDomainSelection(
                        new DomainKey("finance"),
                        new DomainVersion(5))
                },
                Hash('c'),
                DateTimeOffset.UtcNow);

            Require(
                effective.Domains.Select(item => item.DomainKey.Value)
                    .SequenceEqual(
                        new[] { "finance", "hospitality", "scheduling" }),
                "Effective domains must be deterministically key ordered.");

            Require(
                effective.Version.Value == 17,
                "Semantic profile version must remain distinct and explicit.");
        }

        private static OrganizationReference Organization() =>
            new(
                Guid.Parse("10000000-0000-0000-0000-000000000001"),
                Guid.Parse("20000000-0000-0000-0000-000000000001"),
                Guid.Parse("30000000-0000-0000-0000-000000000001"));

        private static OrganisationProfileContentHash Hash(char character) =>
            new(new string(character, 64));

        private static void Require(
            bool condition,
            string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void RequireException<TException>(
            Action action,
            string message)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException(message);
        }
    }
}
