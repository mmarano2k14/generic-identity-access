import type {
  IdentityAddPolicyStatementRequest,
  IdentityApplicationSecurityCapabilityRecord,
  IdentityApplicationSecurityManifestRequest,
  IdentityApplicationSecurityModelRecord,
  IdentityApplicationSecurityModelSummaryRecord,
  IdentityCreateResourceScopeRequest,
  IdentityEffectiveAdministrationContext,
  IdentityGroupMemberRecord,
  IdentityGroupPolicyBindingRecord,
  IdentityManagedGroupPolicyBindingRecord,
  IdentityManagedPolicyRecord,
  IdentityManagedPolicyStatementRecord,
  IdentityManagedPolicyVersionRecord,
  IdentityGroupRecord,
  IdentityPolicyRecord,
  IdentityPolicyStatementRecord,
  IdentityResourceScopeRecord,
  IdentityScopeAuthorityGroupRecord,
  IdentityScopeAuthorityMemberRecord,
  IdentityScopeAuthorityPolicyBindingRecord,
  IdentityScopeTypeRecord,
  IdentitySessionRevocationResult,
  IdentityTenantMembershipCandidateRecord,
  IdentityTenantGroupAssignmentRecord,
  IdentityTenantMembershipRecord,
  IdentityTenantRecord,
  IdentityTenantUserRecord,
  IdentityUpdateResourceScopeRequest,
  IdentityUserRecord,
} from "../admin-contracts.js";
import { IdentityAccessClientError } from "../errors.js";
import { IdentityAccessValueCodec, type IdentityJsonObject } from "./IdentityAccessValueCodec.js";

/** Encodes and decodes administration DTOs without owning transport or routing. */
export class IdentityAccessAdministrationCodec {
  public static userRecord(value: unknown): IdentityUserRecord {
    const data = IdentityAccessValueCodec.object(value);
    return {
      userId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.userId)),
      displayName: IdentityAccessValueCodec.text(data.displayName),
      status: IdentityAccessValueCodec.lifecycleStatus(data.status),
      version: IdentityAccessValueCodec.version(data.version),
    };
  }

  public static tenantRecord(value: unknown): IdentityTenantRecord {
    const data = IdentityAccessValueCodec.object(value);
    return {
      tenantId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.tenantId)),
      displayName: IdentityAccessValueCodec.text(data.displayName),
      status: IdentityAccessValueCodec.lifecycleStatus(data.status),
      version: IdentityAccessValueCodec.version(data.version),
    };
  }

  public static effectiveAdministrationContext(value: unknown): IdentityEffectiveAdministrationContext {
    const data = IdentityAccessValueCodec.object(value);
    const tenantVisibility = IdentityAccessValueCodec.text(data.tenantVisibility);
    if (tenantVisibility !== "membership-limited" && tenantVisibility !== "scope-wide") {
      throw new IdentityAccessClientError("protocol");
    }

    return {
      identityScopeId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.identityScopeId)),
      userId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.userId)),
      applicationKey: IdentityAccessValueCodec.slug(IdentityAccessValueCodec.text(data.applicationKey)),
      tenantVisibility,
      activeTenantMemberships: IdentityAccessValueCodec.array(data.activeTenantMemberships, (entry) => {
        const membership = IdentityAccessValueCodec.object(entry);
        return {
          membershipId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(membership.membershipId)),
          tenantId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(membership.tenantId)),
        };
      }),
    };
  }

  public static tenantMembershipRecord(value: unknown): IdentityTenantMembershipRecord {
    const data = IdentityAccessValueCodec.object(value);
    return {
      membershipId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.membershipId)),
      tenantId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.tenantId)),
      userId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.userId)),
      status: IdentityAccessValueCodec.lifecycleStatus(data.status),
      version: IdentityAccessValueCodec.version(data.version),
    };
  }

  public static tenantUserRecord(value: unknown): IdentityTenantUserRecord {
    const data = IdentityAccessValueCodec.object(value);
    return {
      membershipId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.membershipId)),
      tenantId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.tenantId)),
      userId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.userId)),
      displayName: IdentityAccessValueCodec.text(data.displayName),
      userStatus: IdentityAccessValueCodec.lifecycleStatus(data.userStatus),
      membershipStatus: IdentityAccessValueCodec.lifecycleStatus(data.membershipStatus),
      userVersion: IdentityAccessValueCodec.version(data.userVersion),
      membershipVersion: IdentityAccessValueCodec.version(data.membershipVersion),
    };
  }

  public static groupRecord(value: unknown): IdentityGroupRecord {
    const data = IdentityAccessValueCodec.object(value);
    return {
      tenantId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.tenantId)),
      groupId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.groupId)),
      displayName: IdentityAccessValueCodec.text(data.displayName),
      status: IdentityAccessValueCodec.lifecycleStatus(data.status),
      isTemplate: IdentityAccessValueCodec.flag(data.isTemplate),
      version: IdentityAccessValueCodec.version(data.version),
    };
  }

  public static scopeAuthorityGroupRecord(value: unknown): IdentityScopeAuthorityGroupRecord {
    const data = IdentityAccessValueCodec.object(value);
    return {
      groupId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.groupId)),
      displayName: IdentityAccessValueCodec.text(data.displayName),
      status: IdentityAccessValueCodec.lifecycleStatus(data.status),
      version: IdentityAccessValueCodec.version(data.version),
    };
  }

  public static groupMemberRecord(value: unknown): IdentityGroupMemberRecord {
    const data = IdentityAccessValueCodec.object(value);
    return {
      tenantMembershipId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.tenantMembershipId)),
      userId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.userId)),
    };
  }

  public static tenantGroupAssignmentRecord(value: unknown): IdentityTenantGroupAssignmentRecord {
    const data = IdentityAccessValueCodec.object(value);
    return {
      groupId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.groupId)),
      tenantMembershipId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.tenantMembershipId)),
      userId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.userId)),
    };
  }

  public static tenantMembershipCandidateRecord(value: unknown): IdentityTenantMembershipCandidateRecord {
    const data = IdentityAccessValueCodec.object(value);
    const existingMembershipId = data.existingMembershipId === null || data.existingMembershipId === undefined
      ? undefined
      : IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.existingMembershipId));
    const existingMembershipStatus = data.existingMembershipStatus === null || data.existingMembershipStatus === undefined
      ? undefined
      : IdentityAccessValueCodec.lifecycleStatus(data.existingMembershipStatus);
    return {
      userId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.userId)),
      displayName: IdentityAccessValueCodec.text(data.displayName),
      userStatus: IdentityAccessValueCodec.lifecycleStatus(data.userStatus),
      ...(existingMembershipId === undefined ? {} : { existingMembershipId }),
      ...(existingMembershipStatus === undefined ? {} : { existingMembershipStatus }),
    };
  }

  public static policyRecord(value: unknown): IdentityPolicyRecord {
    const data = IdentityAccessValueCodec.object(value);
    return {
      policyId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.policyId)),
      displayName: IdentityAccessValueCodec.text(data.displayName),
      status: IdentityAccessValueCodec.lifecycleStatus(data.status),
      version: IdentityAccessValueCodec.version(data.version),
    };
  }

  public static policyStatementRecord(value: unknown): IdentityPolicyStatementRecord {
    const data = IdentityAccessValueCodec.object(value);
    return {
      statementId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.statementId)),
      modelVersion: IdentityAccessValueCodec.positiveInteger(data.modelVersion),
      resource: IdentityAccessValueCodec.capabilityPatternSegment(IdentityAccessValueCodec.text(data.resource)),
      feature: IdentityAccessValueCodec.capabilityPatternSegment(IdentityAccessValueCodec.text(data.feature)),
      action: IdentityAccessValueCodec.capabilityPatternSegment(IdentityAccessValueCodec.text(data.action)),
    };
  }

  public static managedPolicyRecord(value: unknown): IdentityManagedPolicyRecord {
    const data = IdentityAccessValueCodec.object(value);
    const defaultVersion = data.defaultVersion === null || data.defaultVersion === undefined
      ? undefined
      : IdentityAccessValueCodec.positiveInteger(data.defaultVersion);
    return {
      policyId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.policyId)),
      policyKey: IdentityAccessValueCodec.managedPolicyKey(IdentityAccessValueCodec.text(data.policyKey)),
      displayName: IdentityAccessValueCodec.text(data.displayName),
      status: IdentityAccessValueCodec.lifecycleStatus(data.status),
      ...(defaultVersion === undefined ? {} : { defaultVersion }),
      version: IdentityAccessValueCodec.version(data.version),
    };
  }

  public static managedPolicyVersionRecord(value: unknown): IdentityManagedPolicyVersionRecord {
    const data = IdentityAccessValueCodec.object(value);
    const publishedAt = data.publishedAt === null || data.publishedAt === undefined
      ? undefined
      : IdentityAccessValueCodec.timestamp(data.publishedAt);
    return {
      policyId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.policyId)),
      policyVersion: IdentityAccessValueCodec.positiveInteger(data.policyVersion),
      modelVersion: IdentityAccessValueCodec.positiveInteger(data.modelVersion),
      ...(publishedAt === undefined ? {} : { publishedAt }),
    };
  }

  public static managedPolicyStatementRecord(value: unknown): IdentityManagedPolicyStatementRecord {
    const data = IdentityAccessValueCodec.object(value);
    return {
      statementId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.statementId)),
      policyVersion: IdentityAccessValueCodec.positiveInteger(data.policyVersion),
      modelVersion: IdentityAccessValueCodec.positiveInteger(data.modelVersion),
      resource: IdentityAccessValueCodec.capabilityPatternSegment(IdentityAccessValueCodec.text(data.resource)),
      feature: IdentityAccessValueCodec.capabilityPatternSegment(IdentityAccessValueCodec.text(data.feature)),
      action: IdentityAccessValueCodec.capabilityPatternSegment(IdentityAccessValueCodec.text(data.action)),
    };
  }

  public static managedGroupPolicyBindingRecord(value: unknown): IdentityManagedGroupPolicyBindingRecord {
    const data = IdentityAccessValueCodec.object(value);
    const resourceScopeId = IdentityAccessValueCodec.nullableUuid(data.resourceScopeId);
    return {
      groupId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.groupId)),
      policyId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.policyId)),
      policyVersion: IdentityAccessValueCodec.positiveInteger(data.policyVersion),
      ...(resourceScopeId === undefined ? {} : { resourceScopeId }),
      includeDescendants: IdentityAccessValueCodec.flag(data.includeDescendants),
    };
  }

  public static groupPolicyBindingRecord(value: unknown): IdentityGroupPolicyBindingRecord {
    const data = IdentityAccessValueCodec.object(value);
    const resourceScopeId = IdentityAccessValueCodec.nullableUuid(data.resourceScopeId);
    return {
      groupId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.groupId)),
      policyId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.policyId)),
      ...(resourceScopeId === undefined ? {} : { resourceScopeId }),
      includeDescendants: IdentityAccessValueCodec.flag(data.includeDescendants),
    };
  }

  public static resourceScopeRecord(value: unknown): IdentityResourceScopeRecord {
    const data = IdentityAccessValueCodec.object(value);
    const parentResourceScopeId = IdentityAccessValueCodec.nullableUuid(data.parentResourceScopeId);
    return {
      resourceScopeId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.resourceScopeId)),
      modelVersion: IdentityAccessValueCodec.positiveInteger(data.modelVersion),
      scopeType: IdentityAccessValueCodec.slug(IdentityAccessValueCodec.text(data.scopeType)),
      externalResourceId: IdentityAccessValueCodec.nonEmpty(IdentityAccessValueCodec.text(data.externalResourceId)),
      displayName: IdentityAccessValueCodec.text(data.displayName),
      ...(parentResourceScopeId === undefined ? {} : { parentResourceScopeId }),
      status: IdentityAccessValueCodec.lifecycleStatus(data.status),
      version: IdentityAccessValueCodec.version(data.version),
    };
  }

  public static scopeTypeRecord(value: unknown): IdentityScopeTypeRecord {
    const data = IdentityAccessValueCodec.object(value);
    const parentKey = data.parentKey === null || data.parentKey === undefined
      ? undefined
      : IdentityAccessValueCodec.slug(IdentityAccessValueCodec.text(data.parentKey));
    return {
      key: IdentityAccessValueCodec.slug(IdentityAccessValueCodec.text(data.key)),
      displayName: IdentityAccessValueCodec.text(data.displayName),
      ...(parentKey === undefined ? {} : { parentKey }),
      canAttachToTenant: IdentityAccessValueCodec.flag(data.canAttachToTenant),
    };
  }

  public static applicationSecurityCapabilityRecord(value: unknown): IdentityApplicationSecurityCapabilityRecord {
    const data = IdentityAccessValueCodec.object(value);
    return {
      resource: IdentityAccessValueCodec.slug(IdentityAccessValueCodec.text(data.resource)),
      feature: IdentityAccessValueCodec.slug(IdentityAccessValueCodec.text(data.feature)),
      action: IdentityAccessValueCodec.slug(IdentityAccessValueCodec.text(data.action)),
      displayName: IdentityAccessValueCodec.text(data.displayName),
    };
  }

  public static applicationSecurityModelSummaryRecord(value: unknown): IdentityApplicationSecurityModelSummaryRecord {
    const data = IdentityAccessValueCodec.object(value);
    return {
      schemaVersion: IdentityAccessValueCodec.positiveInteger(data.schemaVersion),
      applicationKey: IdentityAccessValueCodec.slug(IdentityAccessValueCodec.text(data.applicationKey)),
      modelVersion: IdentityAccessValueCodec.positiveInteger(data.modelVersion),
      rbacProject: IdentityAccessValueCodec.rbacContextSegment(IdentityAccessValueCodec.text(data.rbacProject)),
      rbacNamespaces: IdentityAccessValueCodec.array(data.rbacNamespaces, (entry) =>
        IdentityAccessValueCodec.rbacContextSegment(IdentityAccessValueCodec.text(entry))),
      manifestSha256: IdentityAccessValueCodec.sha256(data.manifestSha256),
      capabilityCount: IdentityAccessValueCodec.positiveInteger(data.capabilityCount),
    };
  }

  public static applicationSecurityModelRecord(value: unknown): IdentityApplicationSecurityModelRecord {
    const data = IdentityAccessValueCodec.object(value);
    const capabilities = IdentityAccessValueCodec.array(
      data.capabilities,
      (entry) => IdentityAccessAdministrationCodec.applicationSecurityCapabilityRecord(entry),
    );
    return {
      schemaVersion: IdentityAccessValueCodec.positiveInteger(data.schemaVersion),
      applicationKey: IdentityAccessValueCodec.slug(IdentityAccessValueCodec.text(data.applicationKey)),
      modelVersion: IdentityAccessValueCodec.positiveInteger(data.modelVersion),
      rbacProject: IdentityAccessValueCodec.rbacContextSegment(IdentityAccessValueCodec.text(data.rbacProject)),
      rbacNamespaces: IdentityAccessValueCodec.array(data.rbacNamespaces, (entry) =>
        IdentityAccessValueCodec.rbacContextSegment(IdentityAccessValueCodec.text(entry))),
      manifestSha256: IdentityAccessValueCodec.sha256(data.manifestSha256),
      capabilityCount: capabilities.length,
      capabilities,
    };
  }

  public static applicationSecurityManifestBody(request: IdentityApplicationSecurityManifestRequest): IdentityJsonObject {
    return {
      schemaVersion: IdentityAccessValueCodec.positiveInteger(request.schemaVersion),
      applicationKey: IdentityAccessValueCodec.slug(request.applicationKey),
      modelVersion: IdentityAccessValueCodec.positiveInteger(request.modelVersion),
      rbac: {
        project: IdentityAccessValueCodec.rbacContextSegment(request.rbac.project),
        namespaces: request.rbac.namespaces.map((value) => IdentityAccessValueCodec.rbacContextSegment(value)),
      },
      resources: request.resources.map((resource) => ({
        name: IdentityAccessValueCodec.slug(resource.name),
        features: resource.features.map((feature) => ({
          name: IdentityAccessValueCodec.slug(feature.name),
          actions: feature.actions.map((action) => ({
            name: IdentityAccessValueCodec.slug(action.name),
            displayName: IdentityAccessValueCodec.nonEmpty(action.displayName),
          })),
        })),
      })),
    };
  }

  public static sessionRevocationResult(value: unknown): IdentitySessionRevocationResult {
    const data = IdentityAccessValueCodec.object(value);
    if (typeof data.revokedCount !== "number" || !Number.isSafeInteger(data.revokedCount) || data.revokedCount < 0) {
      throw new IdentityAccessClientError("protocol");
    }
    return { revokedCount: data.revokedCount };
  }

  public static scopeAuthorityMemberRecord(value: unknown): IdentityScopeAuthorityMemberRecord {
    const data = IdentityAccessValueCodec.object(value);
    return {
      groupId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.groupId)),
      userId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.userId)),
    };
  }

  public static scopeAuthorityPolicyBindingRecord(value: unknown): IdentityScopeAuthorityPolicyBindingRecord {
    const data = IdentityAccessValueCodec.object(value);
    return {
      groupId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.groupId)),
      policyId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.policyId)),
    };
  }

  public static policyStatementBody(request: IdentityAddPolicyStatementRequest): IdentityJsonObject {
    return {
      statementId: IdentityAccessValueCodec.optionalUuidOrEmpty(request.statementId),
      modelVersion: IdentityAccessValueCodec.positiveInteger(request.modelVersion),
      resource: IdentityAccessValueCodec.capabilityPatternSegment(request.resource),
      feature: IdentityAccessValueCodec.capabilityPatternSegment(request.feature),
      action: IdentityAccessValueCodec.capabilityPatternSegment(request.action),
    };
  }

  public static resourceScopeBody(
    request: IdentityCreateResourceScopeRequest | IdentityUpdateResourceScopeRequest,
    update: boolean,
  ): IdentityJsonObject {
    const body: IdentityJsonObject = {
      modelVersion: IdentityAccessValueCodec.positiveInteger(request.modelVersion),
      scopeType: IdentityAccessValueCodec.slug(request.scopeType),
      externalResourceId: IdentityAccessValueCodec.nonEmpty(request.externalResourceId),
      displayName: IdentityAccessValueCodec.nonEmpty(request.displayName),
      parentResourceScopeId: request.parentResourceScopeId === undefined
        ? null
        : IdentityAccessValueCodec.uuid(request.parentResourceScopeId),
      status: IdentityAccessValueCodec.lifecycleStatus(request.status ?? 1),
    };

    if (update) {
      const updated = request as IdentityUpdateResourceScopeRequest;
      body.expectedVersion = IdentityAccessValueCodec.version(updated.expectedVersion);
    } else {
      const created = request as IdentityCreateResourceScopeRequest;
      body.resourceScopeId = IdentityAccessValueCodec.optionalUuidOrEmpty(created.resourceScopeId);
    }

    return body;
  }
}
