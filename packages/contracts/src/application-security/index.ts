/**
 * Public contracts for application-owned security models and capability catalogs.
 *
 * These contracts describe registered security metadata only. They do not grant
 * permissions, carry credentials, or replace server-side authorization.
 */
export type {
  IdentityAddScopeTypeRequest,
  IdentityApplicationSecurityCapabilityRecord,
  IdentityApplicationSecurityManifestAction,
  IdentityApplicationSecurityManifestFeature,
  IdentityApplicationSecurityManifestRequest,
  IdentityApplicationSecurityManifestResource,
  IdentityApplicationSecurityModelRecord,
  IdentityApplicationSecurityModelSummaryRecord,
  IdentityEffectiveAdministrationContext,
  IdentityScopeTypeRecord,
} from "@identity-access/client";

/**
 * Structured permission coordinates derived from a registered application
 * security model. This is intentionally not the external TRN wire format and
 * is never proof of authorization.
 */
export interface IdentityApplicationSecurityPermissionReference {
  readonly applicationKey: string;
  readonly modelVersion: number;
  readonly rbacProject: string;
  readonly rbacNamespace: string;
  readonly resource: string;
  readonly feature: string;
  readonly action: string;
}
