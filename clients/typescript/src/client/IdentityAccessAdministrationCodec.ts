import type {
  IdentityAddPolicyStatementRequest,
  IdentityCreateResourceScopeRequest,
  IdentityGroupMemberRecord,
  IdentityGroupPolicyBindingRecord,
  IdentityGroupRecord,
  IdentityPolicyRecord,
  IdentityPolicyStatementRecord,
  IdentityResourceScopeRecord,
  IdentityScopeAuthorityMemberRecord,
  IdentityScopeAuthorityPolicyBindingRecord,
  IdentityScopeTypeRecord,
  IdentitySessionRevocationResult,
  IdentityTenantMembershipRecord,
  IdentityTenantRecord,
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

  public static groupRecord(value: unknown): IdentityGroupRecord {
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
