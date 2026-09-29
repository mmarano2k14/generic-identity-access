import { IdentityAccessClientError } from "../errors.js";
import { IdentityAccessValueCodec } from "./IdentityAccessValueCodec.js";
/** Encodes and decodes administration DTOs without owning transport or routing. */
export class IdentityAccessAdministrationCodec {
    static userRecord(value) {
        const data = IdentityAccessValueCodec.object(value);
        return {
            userId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.userId)),
            displayName: IdentityAccessValueCodec.text(data.displayName),
            status: IdentityAccessValueCodec.lifecycleStatus(data.status),
            version: IdentityAccessValueCodec.version(data.version),
        };
    }
    static tenantRecord(value) {
        const data = IdentityAccessValueCodec.object(value);
        return {
            tenantId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.tenantId)),
            displayName: IdentityAccessValueCodec.text(data.displayName),
            status: IdentityAccessValueCodec.lifecycleStatus(data.status),
            version: IdentityAccessValueCodec.version(data.version),
        };
    }
    static effectiveAdministrationContext(value) {
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
    static tenantMembershipRecord(value) {
        const data = IdentityAccessValueCodec.object(value);
        return {
            membershipId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.membershipId)),
            tenantId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.tenantId)),
            userId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.userId)),
            status: IdentityAccessValueCodec.lifecycleStatus(data.status),
            version: IdentityAccessValueCodec.version(data.version),
        };
    }
    static tenantUserRecord(value) {
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
    static groupRecord(value) {
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
    static groupTemplateResourceScopeRequirement(value) {
        const data = IdentityAccessValueCodec.object(value);
        return {
            sourceResourceScopeId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.sourceResourceScopeId)),
            modelVersion: IdentityAccessValueCodec.positiveInteger(data.modelVersion),
            scopeType: IdentityAccessValueCodec.slug(IdentityAccessValueCodec.text(data.scopeType)),
            displayName: IdentityAccessValueCodec.text(data.displayName),
        };
    }
    static scopeAuthorityGroupRecord(value) {
        const data = IdentityAccessValueCodec.object(value);
        return {
            groupId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.groupId)),
            displayName: IdentityAccessValueCodec.text(data.displayName),
            status: IdentityAccessValueCodec.lifecycleStatus(data.status),
            version: IdentityAccessValueCodec.version(data.version),
        };
    }
    static groupMemberRecord(value) {
        const data = IdentityAccessValueCodec.object(value);
        return {
            tenantMembershipId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.tenantMembershipId)),
            userId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.userId)),
        };
    }
    static tenantGroupAssignmentRecord(value) {
        const data = IdentityAccessValueCodec.object(value);
        return {
            groupId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.groupId)),
            tenantMembershipId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.tenantMembershipId)),
            userId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.userId)),
        };
    }
    static tenantMembershipCandidateRecord(value) {
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
    static policyRecord(value) {
        const data = IdentityAccessValueCodec.object(value);
        return {
            policyId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.policyId)),
            displayName: IdentityAccessValueCodec.text(data.displayName),
            status: IdentityAccessValueCodec.lifecycleStatus(data.status),
            version: IdentityAccessValueCodec.version(data.version),
        };
    }
    static policyStatementRecord(value) {
        const data = IdentityAccessValueCodec.object(value);
        return {
            statementId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.statementId)),
            modelVersion: IdentityAccessValueCodec.positiveInteger(data.modelVersion),
            resource: IdentityAccessValueCodec.capabilityPatternSegment(IdentityAccessValueCodec.text(data.resource)),
            feature: IdentityAccessValueCodec.capabilityPatternSegment(IdentityAccessValueCodec.text(data.feature)),
            action: IdentityAccessValueCodec.capabilityPatternSegment(IdentityAccessValueCodec.text(data.action)),
        };
    }
    static managedPolicyRecord(value) {
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
    static managedPolicyVersionRecord(value) {
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
    static managedPolicyStatementRecord(value) {
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
    static managedGroupPolicyBindingRecord(value) {
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
    static groupPolicyBindingRecord(value) {
        const data = IdentityAccessValueCodec.object(value);
        const resourceScopeId = IdentityAccessValueCodec.nullableUuid(data.resourceScopeId);
        return {
            groupId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.groupId)),
            policyId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.policyId)),
            ...(resourceScopeId === undefined ? {} : { resourceScopeId }),
            includeDescendants: IdentityAccessValueCodec.flag(data.includeDescendants),
        };
    }
    static passwordCredentialMetadataRecord(value) {
        const data = IdentityAccessValueCodec.object(value);
        const failedAccessCount = data.failedAccessCount;
        if (typeof failedAccessCount !== "number" || !Number.isSafeInteger(failedAccessCount) || failedAccessCount < 0) {
            throw new IdentityAccessClientError("protocol");
        }
        const lockoutUntil = data.lockoutUntil === null || data.lockoutUntil === undefined
            ? undefined
            : IdentityAccessValueCodec.timestamp(data.lockoutUntil);
        return {
            userId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.userId)),
            loginIdentifier: IdentityAccessValueCodec.text(data.loginIdentifier),
            failedAccessCount,
            ...(lockoutUntil === undefined ? {} : { lockoutUntil }),
            version: IdentityAccessValueCodec.version(data.version),
        };
    }
    static resourceScopeRecord(value) {
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
    static organizationRecord(value) {
        const data = IdentityAccessValueCodec.object(value);
        const parentOrganizationId = IdentityAccessValueCodec.nullableUuid(data.parentOrganizationId);
        return {
            identityScopeId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.identityScopeId)),
            tenantId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.tenantId)),
            organizationId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.organizationId)),
            organizationKey: IdentityAccessValueCodec.slug(IdentityAccessValueCodec.text(data.organizationKey)),
            displayName: IdentityAccessValueCodec.text(data.displayName),
            organizationType: IdentityAccessValueCodec.slug(IdentityAccessValueCodec.text(data.organizationType)),
            ...(parentOrganizationId === undefined ? {} : { parentOrganizationId }),
            status: IdentityAccessValueCodec.lifecycleStatus(data.status),
            rowVersion: IdentityAccessValueCodec.version(data.rowVersion),
            createdAt: IdentityAccessValueCodec.timestamp(data.createdAt),
            updatedAt: IdentityAccessValueCodec.timestamp(data.updatedAt),
        };
    }
    static organizationTreeNodeRecord(value) {
        const data = IdentityAccessValueCodec.object(value);
        return {
            organization: IdentityAccessAdministrationCodec.organizationRecord(data.organization),
            children: IdentityAccessValueCodec.array(data.children, (entry) => IdentityAccessAdministrationCodec.organizationTreeNodeRecord(entry)),
        };
    }
    static organizationMembershipRecord(value) {
        const data = IdentityAccessValueCodec.object(value);
        return {
            identityScopeId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.identityScopeId)),
            tenantId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.tenantId)),
            organizationId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.organizationId)),
            tenantMembershipId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.tenantMembershipId)),
            status: IdentityAccessValueCodec.lifecycleStatus(data.status),
            rowVersion: IdentityAccessValueCodec.version(data.rowVersion),
            createdAt: IdentityAccessValueCodec.timestamp(data.createdAt),
            updatedAt: IdentityAccessValueCodec.timestamp(data.updatedAt),
        };
    }
    static organizationResourceScopeLinkRecord(value) {
        const data = IdentityAccessValueCodec.object(value);
        return {
            organizationId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.organizationId)),
            applicationKey: IdentityAccessValueCodec.slug(IdentityAccessValueCodec.text(data.applicationKey)),
            resourceScopeId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.resourceScopeId)),
            scopeType: IdentityAccessValueCodec.slug(IdentityAccessValueCodec.text(data.scopeType)),
            modelVersion: IdentityAccessValueCodec.positiveInteger(data.modelVersion),
            status: IdentityAccessValueCodec.lifecycleStatus(data.status),
            rowVersion: IdentityAccessValueCodec.version(data.rowVersion),
            createdAt: IdentityAccessValueCodec.timestamp(data.createdAt),
            updatedAt: IdentityAccessValueCodec.timestamp(data.updatedAt),
        };
    }
    static scopeTypeRecord(value) {
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
    static applicationSecurityCapabilityRecord(value) {
        const data = IdentityAccessValueCodec.object(value);
        return {
            resource: IdentityAccessValueCodec.slug(IdentityAccessValueCodec.text(data.resource)),
            feature: IdentityAccessValueCodec.slug(IdentityAccessValueCodec.text(data.feature)),
            action: IdentityAccessValueCodec.slug(IdentityAccessValueCodec.text(data.action)),
            displayName: IdentityAccessValueCodec.text(data.displayName),
        };
    }
    static applicationSecurityModelSummaryRecord(value) {
        const data = IdentityAccessValueCodec.object(value);
        return {
            schemaVersion: IdentityAccessValueCodec.positiveInteger(data.schemaVersion),
            applicationKey: IdentityAccessValueCodec.slug(IdentityAccessValueCodec.text(data.applicationKey)),
            modelVersion: IdentityAccessValueCodec.positiveInteger(data.modelVersion),
            rbacProject: IdentityAccessValueCodec.rbacContextSegment(IdentityAccessValueCodec.text(data.rbacProject)),
            rbacNamespaces: IdentityAccessValueCodec.array(data.rbacNamespaces, (entry) => IdentityAccessValueCodec.rbacContextSegment(IdentityAccessValueCodec.text(entry))),
            manifestSha256: IdentityAccessValueCodec.sha256(data.manifestSha256),
            capabilityCount: IdentityAccessValueCodec.positiveInteger(data.capabilityCount),
        };
    }
    static applicationSecurityModelRecord(value) {
        const data = IdentityAccessValueCodec.object(value);
        const capabilities = IdentityAccessValueCodec.array(data.capabilities, (entry) => IdentityAccessAdministrationCodec.applicationSecurityCapabilityRecord(entry));
        return {
            schemaVersion: IdentityAccessValueCodec.positiveInteger(data.schemaVersion),
            applicationKey: IdentityAccessValueCodec.slug(IdentityAccessValueCodec.text(data.applicationKey)),
            modelVersion: IdentityAccessValueCodec.positiveInteger(data.modelVersion),
            rbacProject: IdentityAccessValueCodec.rbacContextSegment(IdentityAccessValueCodec.text(data.rbacProject)),
            rbacNamespaces: IdentityAccessValueCodec.array(data.rbacNamespaces, (entry) => IdentityAccessValueCodec.rbacContextSegment(IdentityAccessValueCodec.text(entry))),
            manifestSha256: IdentityAccessValueCodec.sha256(data.manifestSha256),
            capabilityCount: capabilities.length,
            capabilities,
        };
    }
    static applicationSecurityManifestBody(request) {
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
    static sessionRevocationResult(value) {
        const data = IdentityAccessValueCodec.object(value);
        if (typeof data.revokedCount !== "number" || !Number.isSafeInteger(data.revokedCount) || data.revokedCount < 0) {
            throw new IdentityAccessClientError("protocol");
        }
        return { revokedCount: data.revokedCount };
    }
    static scopeAuthorityMemberRecord(value) {
        const data = IdentityAccessValueCodec.object(value);
        return {
            groupId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.groupId)),
            userId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.userId)),
        };
    }
    static scopeAuthorityPolicyBindingRecord(value) {
        const data = IdentityAccessValueCodec.object(value);
        return {
            groupId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.groupId)),
            policyId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.policyId)),
        };
    }
    static policyStatementBody(request) {
        return {
            statementId: IdentityAccessValueCodec.optionalUuidOrEmpty(request.statementId),
            modelVersion: IdentityAccessValueCodec.positiveInteger(request.modelVersion),
            resource: IdentityAccessValueCodec.capabilityPatternSegment(request.resource),
            feature: IdentityAccessValueCodec.capabilityPatternSegment(request.feature),
            action: IdentityAccessValueCodec.capabilityPatternSegment(request.action),
        };
    }
    static resourceScopeBody(request, update) {
        const body = {
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
            const updated = request;
            body.expectedVersion = IdentityAccessValueCodec.version(updated.expectedVersion);
        }
        else {
            const created = request;
            body.resourceScopeId = IdentityAccessValueCodec.optionalUuidOrEmpty(created.resourceScopeId);
        }
        return body;
    }
}
