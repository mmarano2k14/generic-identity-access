import type {
  IdentityAuthorizationBoundary,
  IdentityCapabilityRequirement,
} from "@generic-identity/contracts";
import {
  NextIdentityServerSession,
  createNextAdministrationContext,
  createNextTenantAdministrationContext,
  createNextTenantFromForm,
  registerNextApplicationSecurityManifestFromForm,
  addNextApplicationScopeTypeFromForm,
  parseNextApplicationSecurityManifestFile,
  requireAuthenticatedIdentitySession,
  presentNextIdentityMutationFailure,
  requireServerCapability,
  searchNextIdentityEntityReferences,
  updateNextTenantFromForm,
  loadNextOrganizationWorkspace,
  createNextOrganizationFromForm,
  updateNextOrganizationFromForm,
  linkNextOrganizationResourceScopeFromForm,
  loadNextGroupWorkspace,
  createNextGroupFromForm,
  createNextGroupFromTemplateFromForm,
  addNextGroupMemberFromForm,
  addNextManagedGroupPolicyBindingFromForm,
  loadNextManagedPolicyWorkspace,
  createNextManagedPolicyFromForm,
  updateNextManagedPolicyFromForm,
  createNextManagedPolicyVersionFromForm,
  publishNextManagedPolicyVersionFromForm,
  addNextManagedPolicyStatementFromForm,
  removeNextManagedPolicyStatementFromForm,
  loadNextResourceScopeWorkspace,
  createNextResourceScopeFromForm,
  updateNextResourceScopeFromForm,
  loadNextDelegatedAuthorityWorkspace,
  createNextScopeAuthorityGroupFromForm,
  updateNextScopeAuthorityGroupFromForm,
  createNextScopeAuthorityPolicyFromForm,
  updateNextScopeAuthorityPolicyFromForm,
  addNextScopeAuthorityMemberFromForm,
  removeNextScopeAuthorityMemberFromForm,
  addNextScopeAuthorityPolicyStatementFromForm,
  removeNextScopeAuthorityPolicyStatementFromForm,
  addNextScopeAuthorityPolicyBindingFromForm,
  removeNextScopeAuthorityPolicyBindingFromForm,
  loadNextMfaWorkspace,
  createNextMfaPolicyFromForm,
  updateNextMfaPolicyFromForm,
  revokeNextMfaAuthenticatorFromForm,
  recoveryRevokeNextMfaAuthenticatorFromForm,
  loadNextSecurityAuditWorkspace,
  loadNextSessionSecurityWorkspace,
  revokeNextUserSessionsFromForm,
  revokeNextClientSessionsFromForm,
} from "@generic-identity/next/server";

export async function serverConsumerProbe(
  session: NextIdentityServerSession,
  boundary: IdentityAuthorizationBoundary,
  requirement: IdentityCapabilityRequirement,
): Promise<string> {
  const current = await requireAuthenticatedIdentitySession(session);
  const administration = createNextAdministrationContext(session, boundary.identityScopeId, boundary.applicationKey);
  void session.client.administration.context.get(administration);
  void createNextTenantFromForm(session, administration, new FormData());
  void registerNextApplicationSecurityManifestFromForm(session, administration, new FormData());
  void addNextApplicationScopeTypeFromForm(session, administration, 1, new FormData());
  void parseNextApplicationSecurityManifestFile(new FormData());
  void updateNextTenantFromForm(session, administration, new FormData());
  void searchNextIdentityEntityReferences(session, administration, {
    identityScopeId: administration.identityScopeId,
    userId: current.userId,
    applicationKey: administration.applicationKey,
    tenantVisibility: "scope-wide",
    activeTenantMemberships: [],
  }, "user", "probe");
  void presentNextIdentityMutationFailure(new Error("probe"));
  if (boundary.tenantId !== undefined) {
    const tenantAdministration = createNextTenantAdministrationContext(
      session,
      boundary.identityScopeId,
      boundary.applicationKey,
      boundary.tenantId,
    );
    void session.client.administration.groups.list(tenantAdministration);
    void createNextOrganizationFromForm(session, tenantAdministration, new FormData());
    void updateNextOrganizationFromForm(session, tenantAdministration, new FormData());
    void linkNextOrganizationResourceScopeFromForm(session, tenantAdministration, new FormData());
    void createNextGroupFromForm(session, tenantAdministration, new FormData());
    void createNextGroupFromTemplateFromForm(session, tenantAdministration, new FormData());
    void addNextGroupMemberFromForm(session, tenantAdministration, new FormData());
    void addNextManagedGroupPolicyBindingFromForm(session, tenantAdministration, new FormData());
  }
  void loadNextGroupWorkspace(session, administration, {
    identityScopeId: administration.identityScopeId,
    userId: current.userId,
    applicationKey: administration.applicationKey,
    tenantVisibility: "scope-wide",
    activeTenantMemberships: [],
  });
  void loadNextOrganizationWorkspace(session, administration, {
    identityScopeId: administration.identityScopeId,
    userId: current.userId,
    applicationKey: administration.applicationKey,
    tenantVisibility: "scope-wide",
    activeTenantMemberships: [],
  });

  void loadNextManagedPolicyWorkspace(session, administration);
  void createNextManagedPolicyFromForm(session, administration, new FormData());
  void updateNextManagedPolicyFromForm(session, administration, new FormData());
  void createNextManagedPolicyVersionFromForm(session, administration, new FormData());
  void publishNextManagedPolicyVersionFromForm(session, administration, new FormData());
  void addNextManagedPolicyStatementFromForm(session, administration, new FormData());
  void removeNextManagedPolicyStatementFromForm(session, administration, new FormData());
  void loadNextResourceScopeWorkspace(session, administration, {
    identityScopeId: administration.identityScopeId,
    userId: current.userId,
    applicationKey: administration.applicationKey,
    tenantVisibility: "scope-wide",
    activeTenantMemberships: [],
  });
  void createNextResourceScopeFromForm(session, administration, {
    identityScopeId: administration.identityScopeId,
    userId: current.userId,
    applicationKey: administration.applicationKey,
    tenantVisibility: "scope-wide",
    activeTenantMemberships: [],
  }, new FormData());
  void updateNextResourceScopeFromForm(session, administration, {
    identityScopeId: administration.identityScopeId,
    userId: current.userId,
    applicationKey: administration.applicationKey,
    tenantVisibility: "scope-wide",
    activeTenantMemberships: [],
  }, new FormData());
  void loadNextDelegatedAuthorityWorkspace(session, administration);
  void createNextScopeAuthorityGroupFromForm(session, administration, new FormData());
  void updateNextScopeAuthorityGroupFromForm(session, administration, new FormData());
  void createNextScopeAuthorityPolicyFromForm(session, administration, new FormData());
  void updateNextScopeAuthorityPolicyFromForm(session, administration, new FormData());
  void addNextScopeAuthorityMemberFromForm(session, administration, new FormData());
  void removeNextScopeAuthorityMemberFromForm(session, administration, new FormData());
  void addNextScopeAuthorityPolicyStatementFromForm(session, administration, new FormData());
  void removeNextScopeAuthorityPolicyStatementFromForm(session, administration, new FormData());
  void addNextScopeAuthorityPolicyBindingFromForm(session, administration, new FormData());
  void removeNextScopeAuthorityPolicyBindingFromForm(session, administration, new FormData());
  void loadNextMfaWorkspace(session, administration);
  void createNextMfaPolicyFromForm(session, administration, new FormData());
  void updateNextMfaPolicyFromForm(session, administration, new FormData());
  void revokeNextMfaAuthenticatorFromForm(session, administration, new FormData());
  void recoveryRevokeNextMfaAuthenticatorFromForm(session, administration, new FormData());
  void loadNextSecurityAuditWorkspace(session, administration, {
    identityScopeId: administration.identityScopeId,
    userId: current.userId,
    applicationKey: administration.applicationKey,
    tenantVisibility: "scope-wide",
    activeTenantMemberships: [],
  });
  void loadNextSessionSecurityWorkspace(session, administration, {
    identityScopeId: administration.identityScopeId,
    userId: current.userId,
    applicationKey: administration.applicationKey,
    tenantVisibility: "scope-wide",
    activeTenantMemberships: [],
  });
  void revokeNextUserSessionsFromForm(session, administration, new FormData());
  void revokeNextClientSessionsFromForm(session, administration, new FormData());
  await requireServerCapability(session, boundary, requirement);
  return current.userId;
}
