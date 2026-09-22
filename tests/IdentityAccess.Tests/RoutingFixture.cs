using System.Text.Json.Nodes;
using IdentityAccess.Application.Routing;
using IdentityAccess.Domain;

namespace IdentityAccess.Tests
{

    internal static class RoutingFixture
    {
        internal static readonly Guid ScopeA = Guid.Parse("11111111-1111-4111-8111-111111111111");
        internal static readonly Guid ScopeB = Guid.Parse("22222222-2222-4222-8222-222222222222");
        internal static readonly Guid ScopeC = Guid.Parse("33333333-3333-4333-8333-333333333333");
        internal static DatabaseRouteRequest Request(Guid? scope = null, string application = "app-a") =>
            new(new ApplicationKey(application), scope ?? ScopeA);

        internal static string Change(Action<JsonObject> change)
        {
            var document = JsonNode.Parse(Json)!.AsObject();
            change(document);
            return document.ToJsonString();
        }

        internal const string Json = """
    {
      "schemaVersion": "1",
      "revision": 7,
      "routingProvider": "configuration",
      "placementGranularity": "identity-scope",
      "destinations": [
        {
          "key": "identity-a",
          "provider": "postgresql",
          "connectionSecretRef": "env:IDENTITY_ACCESS_POSTGRES_A",
          "state": "active"
        },
        {
          "key": "identity-b",
          "provider": "postgresql",
          "connectionSecretRef": "env:IDENTITY_ACCESS_POSTGRES_B",
          "state": "active"
        }
      ],
      "routes": [
        {
          "applicationKey": "app-a",
          "identityScopeId": "11111111-1111-4111-8111-111111111111",
          "dataSet": "identity-directory",
          "destinationKey": "identity-a",
          "version": 3,
          "state": "active"
        },
        {
          "applicationKey": "app-a",
          "identityScopeId": "22222222-2222-4222-8222-222222222222",
          "dataSet": "identity-directory",
          "destinationKey": "identity-b",
          "version": 1,
          "state": "active"
        },
        {
          "applicationKey": "app-b",
          "identityScopeId": "33333333-3333-4333-8333-333333333333",
          "dataSet": "identity-directory",
          "destinationKey": "identity-a",
          "version": 1,
          "state": "active"
        }
      ],
      "authenticationContexts": [
        {
          "key": "app-a-primary",
          "applicationKey": "app-a",
          "identityScopeId": "11111111-1111-4111-8111-111111111111",
          "state": "active"
        },
        {
          "key": "app-a-secondary",
          "applicationKey": "app-a",
          "identityScopeId": "22222222-2222-4222-8222-222222222222",
          "state": "active"
        },
        {
          "key": "app-b-primary",
          "applicationKey": "app-b",
          "identityScopeId": "33333333-3333-4333-8333-333333333333",
          "state": "active"
        }
      ]
    }
    """;
    }
}
