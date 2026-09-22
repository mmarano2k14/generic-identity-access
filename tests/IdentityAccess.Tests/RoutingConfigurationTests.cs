using System.Text;
using System.Text.Json.Nodes;
using IdentityAccess.Infrastructure.ConfigurationRouting;

namespace IdentityAccess.Tests
{

    public sealed class RoutingConfigurationTests
    {
        [Fact]
        public void Valid_configuration_has_one_immutable_revision()
        {
            var provider = ConfigurationRoutingProvider.Parse(RoutingFixture.Json);
            Assert.Equal(7L, provider.ConfigurationRevision);
            Assert.Equal(2, provider.DestinationCount);
            Assert.Equal(3, provider.RouteCount);
            Assert.Equal(3, provider.AuthenticationContextCount);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("null")]
        [InlineData("[]")]
        [InlineData("{}")] 
        [InlineData("{malformed}")]
        [InlineData("""{"schemaVersion":"1",}""")]
        public void Invalid_json_is_rejected_without_empty_default_configuration(string json) =>
            Assert.Equal(RoutingConfigurationFailure.InvalidJson,
                Assert.Throws<RoutingConfigurationException>(() => ConfigurationRoutingProvider.Parse(json)).Code);

        [Theory]
        [InlineData("schemaVersion", "2", RoutingConfigurationFailure.UnsupportedSchema)]
        [InlineData("routingProvider", "catalogue", RoutingConfigurationFailure.UnsupportedProvider)]
        [InlineData("placementGranularity", "tenant", RoutingConfigurationFailure.UnsupportedPlacementGranularity)]
        [InlineData("placementGranularity", "table", RoutingConfigurationFailure.UnsupportedPlacementGranularity)]
        public void Unsupported_contract_choices_fail_instead_of_selecting_a_default(
            string field, string value, RoutingConfigurationFailure expected) =>
            Reject(d => d[field] = value, expected);

        [Theory]
        [InlineData("schemaVersion")]
        [InlineData("revision")]
        [InlineData("routingProvider")]
        [InlineData("placementGranularity")]
        [InlineData("destinations")]
        [InlineData("routes")]
        [InlineData("authenticationContexts")]
        public void Every_root_field_is_required(string field) =>
            Reject(d => d.Remove(field), RoutingConfigurationFailure.InvalidJson);

        [Fact]
        public void Unknown_fields_are_rejected_instead_of_ignored()
        {
            Reject(d => d["catalogueConnection"] = "DO_NOT_ECHO", RoutingConfigurationFailure.InvalidJson);
            Reject(d => d["destinations"]![0]!["connectionString"] = "Password=DO_NOT_ECHO", RoutingConfigurationFailure.InvalidJson);
            Reject(d => d["routes"]![0]!["tenantId"] = "DO_NOT_ECHO", RoutingConfigurationFailure.InvalidJson);
        }

        [Fact]
        public void Property_names_are_case_sensitive()
        {
            Reject(d => { d.Remove("revision"); d["Revision"] = 7; }, RoutingConfigurationFailure.InvalidJson);
        }

        [Theory]
        [InlineData("""{"revision":7,"revision":8}""")]
        [InlineData("""{"revision":7,"revis\u0069on":8}""")]
        [InlineData("""{"nested":{"state":"active","state":"disabled"}}""")]
        public void Duplicate_json_properties_are_rejected_recursively(string json) =>
            Assert.Equal(RoutingConfigurationFailure.DuplicateJsonProperty,
                Assert.Throws<RoutingConfigurationException>(() => ConfigurationRoutingProvider.Parse(json)).Code);

        [Theory]
        [InlineData("destinations")]
        [InlineData("routes")]
        [InlineData("authenticationContexts")]
        public void Null_collections_and_null_entries_are_rejected(string collection)
        {
            Reject(d => d[collection] = null, RoutingConfigurationFailure.InvalidValue);
            Reject(d => d[collection]![0] = null, RoutingConfigurationFailure.InvalidValue);
        }

        [Fact]
        public void Empty_route_or_destination_catalogues_are_not_activated()
        {
            Reject(d => d["routes"] = new JsonArray(), RoutingConfigurationFailure.InvalidValue);
            Reject(d => d["destinations"] = new JsonArray(), RoutingConfigurationFailure.InvalidValue);
            var provider = ConfigurationRoutingProvider.Parse(RoutingFixture.Change(d => d["authenticationContexts"] = new JsonArray()));
            Assert.Equal(0, provider.AuthenticationContextCount);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Positive_revisions_are_required(long version)
        {
            Reject(d => d["revision"] = version, RoutingConfigurationFailure.InvalidValue);
            Reject(d => d["routes"]![0]!["version"] = version, RoutingConfigurationFailure.InvalidValue);
        }

        [Fact]
        public void Destination_keys_and_route_keys_are_unique_even_when_entries_are_identical()
        {
            Reject(d => d["destinations"]!.AsArray().Add(d["destinations"]![0]!.DeepClone()),
                RoutingConfigurationFailure.DuplicateDestination);
            Reject(d => d["routes"]!.AsArray().Add(d["routes"]![0]!.DeepClone()),
                RoutingConfigurationFailure.DuplicateRoute);
        }

        [Fact]
        public void Duplicate_disabled_routes_do_not_create_a_last_write_wins_policy() =>
            Reject(d =>
            {
                var duplicate = d["routes"]![0]!.DeepClone();
                duplicate["state"] = "disabled";
                d["routes"]!.AsArray().Add(duplicate);
            }, RoutingConfigurationFailure.DuplicateRoute);

        [Fact]
        public void Route_must_reference_a_registered_destination() =>
            Reject(d => d["routes"]![0]!["destinationKey"] = "unknown", RoutingConfigurationFailure.UnknownDestination);

        [Theory]
        [InlineData("active")]
        [InlineData("disabled")]
        public void Same_identity_scope_cannot_have_conflicting_authorities_across_applications(string state) =>
            Reject(d =>
            {
                d["routes"]![2]!["identityScopeId"] = RoutingFixture.ScopeA.ToString("D");
                d["routes"]![2]!["destinationKey"] = "identity-b";
                d["routes"]![2]!["state"] = state;
            }, RoutingConfigurationFailure.ConflictingScopePlacement);

        [Fact]
        public void Authentication_contexts_are_unique_and_reference_registered_application_scope_pairs()
        {
            Reject(d => d["authenticationContexts"]!.AsArray().Add(d["authenticationContexts"]![0]!.DeepClone()),
                RoutingConfigurationFailure.DuplicateAuthenticationContext);
            Reject(d => d["authenticationContexts"]![0]!["applicationKey"] = "unregistered",
                RoutingConfigurationFailure.UnknownAuthenticationRoute);
        }

        [Theory]
        [InlineData("destinations", "state", "enabled")]
        [InlineData("routes", "state", "ACTIVE")]
        [InlineData("authenticationContexts", "state", "unknown")]
        [InlineData("routes", "applicationKey", "APP-A")]
        [InlineData("routes", "identityScopeId", "not-a-guid")]
        [InlineData("routes", "identityScopeId", "00000000-0000-0000-0000-000000000000")]
        [InlineData("routes", "dataSet", "credentials")]
        [InlineData("destinations", "connectionSecretRef", "Host=localhost;Password=DO_NOT_ECHO")]
        public void Invalid_values_are_rejected_without_echoing_input(string collection, string field, string value) =>
            Reject(d => d[collection]![0]![field] = value, RoutingConfigurationFailure.InvalidValue);

        [Fact]
        public void Non_postgresql_destinations_are_not_accepted() =>
            Reject(d => d["destinations"]![0]!["provider"] = "mongodb", RoutingConfigurationFailure.UnsupportedProvider);

        [Fact]
        public void Input_size_and_catalogue_cardinality_are_bounded()
        {
            Assert.Equal(RoutingConfigurationFailure.ConfigurationTooLarge,
                Assert.Throws<RoutingConfigurationException>(() =>
                    ConfigurationRoutingProvider.Parse(new string(' ', ConfigurationRoutingProvider.MaximumConfigurationBytes + 1))).Code);
            Reject(d =>
            {
                var entries = new JsonArray();
                for (var i = 0; i <= ConfigurationRoutingProvider.MaximumDestinations; i++)
                    entries.Add(d["destinations"]![0]!.DeepClone());
                d["destinations"] = entries;
            }, RoutingConfigurationFailure.InvalidValue);
        }

        [Fact]
        public void File_loader_accepts_utf8_bom_and_never_watches_the_file()
        {
            var path = Path.GetTempFileName();
            try
            {
                File.WriteAllText(path, RoutingFixture.Json, new UTF8Encoding(true));
                var provider = ConfigurationRoutingProvider.LoadFile(path);
                File.WriteAllText(path, "invalid replacement");
                Assert.Equal(7L, provider.ConfigurationRevision);
                Assert.Equal(RoutingConfigurationFailure.InvalidJson,
                    Assert.Throws<RoutingConfigurationException>(() => ConfigurationRoutingProvider.LoadFile(path)).Code);
            }
            finally { File.Delete(path); }
        }

        [Fact]
        public void Invalid_utf8_file_is_rejected()
        {
            var path = Path.GetTempFileName();
            try
            {
                File.WriteAllBytes(path, [0xc3, 0x28]);
                Assert.Equal(RoutingConfigurationFailure.InvalidJson,
                    Assert.Throws<RoutingConfigurationException>(() => ConfigurationRoutingProvider.LoadFile(path)).Code);
            }
            finally { File.Delete(path); }
        }

        [Fact]
        public void Missing_file_error_does_not_disclose_its_path()
        {
            var path = Path.Combine(Path.GetTempPath(), $"DO_NOT_ECHO_{Guid.NewGuid():N}.json");
            var error = Assert.Throws<RoutingConfigurationException>(() => ConfigurationRoutingProvider.LoadFile(path));
            Assert.Equal(RoutingConfigurationFailure.FileUnavailable, error.Code);
            Assert.Null(error.InnerException);
            Assert.DoesNotContain("DO_NOT_ECHO", error.ToString());
        }

        private static void Reject(Action<JsonObject> change, RoutingConfigurationFailure expected)
        {
            var error = Assert.Throws<RoutingConfigurationException>(() =>
                ConfigurationRoutingProvider.Parse(RoutingFixture.Change(change)));
            Assert.Equal(expected, error.Code);
            Assert.Null(error.InnerException);
            Assert.DoesNotContain("DO_NOT_ECHO", error.ToString());
        }
    }
}
