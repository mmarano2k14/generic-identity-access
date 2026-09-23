import { IdentityAccessHttpTransport } from "../IdentityAccessHttpTransport.js";
import { IdentityAccessAdministrationTransport } from "./IdentityAccessAdministrationTransport.js";
import { IdentityAccessGroupsClient } from "./IdentityAccessGroupsClient.js";
import { IdentityAccessMembershipsClient } from "./IdentityAccessMembershipsClient.js";
import { IdentityAccessMfaClient } from "./IdentityAccessMfaClient.js";
import { IdentityAccessPoliciesClient } from "./IdentityAccessPoliciesClient.js";
import { IdentityAccessResourceScopesClient } from "./IdentityAccessResourceScopesClient.js";
import { IdentityAccessScopeAuthorityClient } from "./IdentityAccessScopeAuthorityClient.js";
import { IdentityAccessSecurityModelsClient } from "./IdentityAccessSecurityModelsClient.js";
import { IdentityAccessSessionsClient } from "./IdentityAccessSessionsClient.js";
import { IdentityAccessTenantsClient } from "./IdentityAccessTenantsClient.js";
import { IdentityAccessUsersClient } from "./IdentityAccessUsersClient.js";

/** Groups administration responsibilities while keeping each domain in its own class. */
export class IdentityAccessAdministrationClient {
  public readonly users: IdentityAccessUsersClient;
  public readonly tenants: IdentityAccessTenantsClient;
  public readonly memberships: IdentityAccessMembershipsClient;
  public readonly mfa: IdentityAccessMfaClient;
  public readonly groups: IdentityAccessGroupsClient;
  public readonly policies: IdentityAccessPoliciesClient;
  public readonly resourceScopes: IdentityAccessResourceScopesClient;
  public readonly securityModels: IdentityAccessSecurityModelsClient;
  public readonly sessions: IdentityAccessSessionsClient;
  public readonly scopeAuthority: IdentityAccessScopeAuthorityClient;

  public constructor(transport: IdentityAccessHttpTransport) {
    const admin = new IdentityAccessAdministrationTransport(transport);
    this.users = new IdentityAccessUsersClient(admin);
    this.tenants = new IdentityAccessTenantsClient(admin);
    this.memberships = new IdentityAccessMembershipsClient(admin);
    this.mfa = new IdentityAccessMfaClient(admin);
    this.groups = new IdentityAccessGroupsClient(admin);
    this.policies = new IdentityAccessPoliciesClient(admin);
    this.resourceScopes = new IdentityAccessResourceScopesClient(admin);
    this.securityModels = new IdentityAccessSecurityModelsClient(admin);
    this.sessions = new IdentityAccessSessionsClient(admin);
    this.scopeAuthority = new IdentityAccessScopeAuthorityClient(admin);
  }
}
