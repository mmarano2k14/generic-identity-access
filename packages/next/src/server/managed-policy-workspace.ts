import "server-only";

import type { IdentityAdministrationContext } from "@generic-identity/auth";
import type {
  IdentityApplicationSecurityModelRecord,
  IdentityManagedPolicyRecord,
  IdentityManagedPolicyStatementRecord,
  IdentityManagedPolicyVersionRecord,
} from "@generic-identity/contracts";
import { isAllowedOnServer } from "./authorization";
import { NextIdentityServerSession } from "./session";

const POLICY_LIMIT = 50;

export interface NextManagedPolicyWorkspaceQuery {
  readonly policyId?: string;
  readonly policyVersion?: string | number;
}

export interface NextManagedPolicyCapabilityOption {
  readonly value: string;
  readonly label: string;
  readonly trnPreviews: readonly string[];
}

export interface NextManagedPolicyModelOption {
  readonly modelVersion: number;
  readonly rbacProject: string;
  readonly rbacNamespaces: readonly string[];
  readonly manifestSha256: string;
  readonly capabilities: readonly NextManagedPolicyCapabilityOption[];
}

export interface NextManagedPolicyWorkspacePermissions {
  readonly canReadPolicies: boolean;
  readonly canWritePolicies: boolean;
  readonly canReadStatements: boolean;
  readonly canWriteStatements: boolean;
  readonly canReadSecurityModels: boolean;
}

export interface NextManagedPolicyWorkspace {
  readonly policies: readonly IdentityManagedPolicyRecord[];
  readonly selectedPolicy: IdentityManagedPolicyRecord | null;
  readonly versions: readonly IdentityManagedPolicyVersionRecord[];
  readonly selectedVersion: IdentityManagedPolicyVersionRecord | null;
  readonly statements: readonly IdentityManagedPolicyStatementRecord[];
  readonly policyBuilderModels: readonly NextManagedPolicyModelOption[];
  readonly permissions: NextManagedPolicyWorkspacePermissions;
  readonly policiesTruncated: boolean;
}

/**
 * Composes the identity-scope managed-policy GOLDEN workspace. The catalog is
 * intentionally tenant independent: tenant bindings consume published versions
 * elsewhere, but they never own or edit these definitions.
 */
export async function loadNextManagedPolicyWorkspace(
  session: NextIdentityServerSession,
  context: IdentityAdministrationContext,
  query: NextManagedPolicyWorkspaceQuery = {},
): Promise<NextManagedPolicyWorkspace> {
  const boundary = {
    identityScopeId: context.identityScopeId,
    applicationKey: context.applicationKey,
  };
  const capability = (feature: string, action: string) =>
    isAllowedOnServer(session, boundary, { resource: "identity-access", feature, action });

  const [
    canReadPolicies,
    canWritePolicies,
    canReadStatements,
    canWriteStatements,
    canReadSecurityModels,
  ] = await Promise.all([
    capability("policy", "read"),
    capability("policy", "write"),
    capability("policy-statement", "read"),
    capability("policy-statement", "write"),
    capability("security-model", "read"),
  ]);
  const permissions: NextManagedPolicyWorkspacePermissions = {
    canReadPolicies,
    canWritePolicies,
    canReadStatements,
    canWriteStatements,
    canReadSecurityModels,
  };

  if (!canReadPolicies) return emptyWorkspace(permissions);

  const policies = await session.client.accessControl.managedPolicies.list(context, { limit: POLICY_LIMIT });
  const requestedPolicyId = query.policyId?.trim().toLowerCase();
  const selectedPolicy = requestedPolicyId
    ? policies.find((policy) => policy.policyId === requestedPolicyId)
      ?? await session.client.accessControl.managedPolicies.get(context, requestedPolicyId)
    : null;

  if (!selectedPolicy) {
    return {
      ...emptyWorkspace(permissions),
      policies,
      policiesTruncated: policies.length >= POLICY_LIMIT,
    };
  }

  const versions = await session.client.accessControl.managedPolicies.listVersions(context, selectedPolicy.policyId);
  const requestedVersion = parseRequestedVersion(query.policyVersion);
  const selectedVersion = await selectVersion(session, context, selectedPolicy, versions, requestedVersion);

  const [statements, policyBuilderModels] = await Promise.all([
    selectedVersion && canReadStatements
      ? session.client.accessControl.managedPolicies.listStatements(
          context,
          selectedPolicy.policyId,
          selectedVersion.policyVersion,
        )
      : Promise.resolve([] as readonly IdentityManagedPolicyStatementRecord[]),
    canReadSecurityModels
      ? loadPolicyBuilderModels(session, context)
      : Promise.resolve([] as readonly NextManagedPolicyModelOption[]),
  ]);

  return {
    policies,
    selectedPolicy,
    versions,
    selectedVersion,
    statements,
    policyBuilderModels,
    permissions,
    policiesTruncated: policies.length >= POLICY_LIMIT,
  };
}

async function selectVersion(
  session: NextIdentityServerSession,
  context: IdentityAdministrationContext,
  policy: IdentityManagedPolicyRecord,
  versions: readonly IdentityManagedPolicyVersionRecord[],
  requestedVersion?: number,
): Promise<IdentityManagedPolicyVersionRecord | null> {
  const versionNumber = requestedVersion ?? policy.defaultVersion ?? versions.at(-1)?.policyVersion;
  if (versionNumber === undefined) return null;
  return versions.find((version) => version.policyVersion === versionNumber)
    ?? session.client.accessControl.managedPolicies.getVersion(context, policy.policyId, versionNumber);
}

async function loadPolicyBuilderModels(
  session: NextIdentityServerSession,
  context: IdentityAdministrationContext,
): Promise<readonly NextManagedPolicyModelOption[]> {
  const summaries = await session.client.applicationSecurity.models.list(context);
  const models = await Promise.all(
    summaries.map((summary) => session.client.applicationSecurity.models.get(context, summary.modelVersion)),
  );

  return models
    .filter((model): model is IdentityApplicationSecurityModelRecord => model !== null)
    .sort((left, right) => right.modelVersion - left.modelVersion)
    .map((model) => ({
      modelVersion: model.modelVersion,
      rbacProject: model.rbacProject,
      rbacNamespaces: model.rbacNamespaces,
      manifestSha256: model.manifestSha256,
      capabilities: model.capabilities.map((capability) => ({
        value: `${model.modelVersion}|${capability.resource}|${capability.feature}|${capability.action}`,
        label: `${capability.displayName} · ${capability.resource}:${capability.feature}:${capability.action}`,
        trnPreviews: model.rbacNamespaces.map((namespaceValue) =>
          `trn:${model.rbacProject}:${namespaceValue}:${capability.resource}:${capability.feature}:${capability.action}`),
      })),
    }));
}

function emptyWorkspace(permissions: NextManagedPolicyWorkspacePermissions): NextManagedPolicyWorkspace {
  return {
    policies: [],
    selectedPolicy: null,
    versions: [],
    selectedVersion: null,
    statements: [],
    policyBuilderModels: [],
    permissions,
    policiesTruncated: false,
  };
}

function parseRequestedVersion(value: string | number | undefined): number | undefined {
  if (value === undefined || value === "") return undefined;
  const parsed = typeof value === "number" ? value : Number(value);
  return Number.isSafeInteger(parsed) && parsed > 0 ? parsed : undefined;
}
