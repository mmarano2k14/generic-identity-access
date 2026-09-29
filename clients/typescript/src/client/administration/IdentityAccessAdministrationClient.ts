import { IdentityAccessHttpTransport } from "../IdentityAccessHttpTransport.js";
import { IdentityAccessAdministrationTransport } from "./IdentityAccessAdministrationTransport.js";
import { IdentityAccessAdministrationContextClient } from "./IdentityAccessAdministrationContextClient.js";
import { IdentityAccessCredentialsClient } from "./IdentityAccessCredentialsClient.js";
import { IdentityAccessGroupsClient } from "./IdentityAccessGroupsClient.js";
import { IdentityAccessMembershipCandidatesClient } from "./IdentityAccessMembershipCandidatesClient.js";
import { IdentityAccessTenantGroupAssignmentsClient } from "./IdentityAccessTenantGroupAssignmentsClient.js";
import { IdentityAccessMembershipsClient } from "./IdentityAccessMembershipsClient.js";
import { IdentityAccessManagedPoliciesClient } from "./IdentityAccessManagedPoliciesClient.js";
import { IdentityAccessManagedPolicyBindingsClient } from "./IdentityAccessManagedPolicyBindingsClient.js";
import { IdentityAccessMfaClient } from "./IdentityAccessMfaClient.js";
import { IdentityAccessResourceScopesClient } from "./IdentityAccessResourceScopesClient.js";
import { IdentityAccessScopeAuthorityClient } from "./IdentityAccessScopeAuthorityClient.js";
import { IdentityAccessSecurityAuditClient } from "./IdentityAccessSecurityAuditClient.js";
import { IdentityAccessSecurityModelsClient } from "./IdentityAccessSecurityModelsClient.js";
import { IdentityAccessSessionsClient } from "./IdentityAccessSessionsClient.js";
import { IdentityAccessTenantsClient } from "./IdentityAccessTenantsClient.js";
import { IdentityAccessTenantUsersClient } from "./IdentityAccessTenantUsersClient.js";
import { IdentityAccessUsersClient } from "./IdentityAccessUsersClient.js";

/** Groups administration responsibilities while keeping each domain in its own class. */
export class IdentityAccessAdministrationClient {
  public readonly context: IdentityAccessAdministrationContextClient;
  public readonly users: IdentityAccessUsersClient;
  public readonly credentials: IdentityAccessCredentialsClient;
  public readonly tenants: IdentityAccessTenantsClient;
  public readonly memberships: IdentityAccessMembershipsClient;
  public readonly membershipCandidates: IdentityAccessMembershipCandidatesClient;
  public readonly tenantGroupAssignments: IdentityAccessTenantGroupAssignmentsClient;
  public readonly tenantUsers: IdentityAccessTenantUsersClient;
  public readonly mfa: IdentityAccessMfaClient;
  public readonly managedPolicies: IdentityAccessManagedPoliciesClient;
  public readonly managedPolicyBindings: IdentityAccessManagedPolicyBindingsClient;
  public readonly groups: IdentityAccessGroupsClient;
  public readonly resourceScopes: IdentityAccessResourceScopesClient;
  public readonly securityModels: IdentityAccessSecurityModelsClient;
  public readonly sessions: IdentityAccessSessionsClient;
  public readonly securityAudit: IdentityAccessSecurityAuditClient;
  public readonly scopeAuthority: IdentityAccessScopeAuthorityClient;

  public constructor(transport: IdentityAccessHttpTransport) {
    const admin = new IdentityAccessAdministrationTransport(transport);
    this.context = new IdentityAccessAdministrationContextClient(admin);
    this.users = new IdentityAccessUsersClient(admin);
    this.credentials = new IdentityAccessCredentialsClient(admin);
    this.tenants = new IdentityAccessTenantsClient(admin);
    this.memberships = new IdentityAccessMembershipsClient(admin);
    this.membershipCandidates = new IdentityAccessMembershipCandidatesClient(admin);
    this.tenantGroupAssignments = new IdentityAccessTenantGroupAssignmentsClient(admin);
    this.tenantUsers = new IdentityAccessTenantUsersClient(admin);
    this.mfa = new IdentityAccessMfaClient(admin);
    this.managedPolicies = new IdentityAccessManagedPoliciesClient(admin);
    this.managedPolicyBindings = new IdentityAccessManagedPolicyBindingsClient(admin);
    this.groups = new IdentityAccessGroupsClient(admin);
    this.resourceScopes = new IdentityAccessResourceScopesClient(admin);
    this.securityModels = new IdentityAccessSecurityModelsClient(admin);
    this.sessions = new IdentityAccessSessionsClient(admin);
    this.securityAudit = new IdentityAccessSecurityAuditClient(admin);
    this.scopeAuthority = new IdentityAccessScopeAuthorityClient(admin);
  }
}
