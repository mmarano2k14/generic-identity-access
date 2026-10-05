import type { IdentityAdministrationContext } from "@identity-access/client";
import type {
  IdentityAddScopeTypeRequest,
  IdentityApplicationSecurityCapabilityRecord,
  IdentityApplicationSecurityManifestRequest,
  IdentityApplicationSecurityModelRecord,
  IdentityApplicationSecurityModelSummaryRecord,
  IdentityApplicationSecurityPermissionReference,
  IdentityEffectiveAdministrationContext,
  IdentityScopeTypeRecord,
} from "@generic-identity/contracts/application-security";

export interface GenericIdentityApplicationSecurityModelsClient {
  list(
    context: IdentityAdministrationContext,
    signal?: AbortSignal,
  ): Promise<readonly IdentityApplicationSecurityModelSummaryRecord[]>;

  get(
    context: IdentityAdministrationContext,
    modelVersion: number,
    signal?: AbortSignal,
  ): Promise<IdentityApplicationSecurityModelRecord | null>;
}

export interface GenericIdentityApplicationSecurityManifestsClient {
  register(
    context: IdentityAdministrationContext,
    request: IdentityApplicationSecurityManifestRequest,
    signal?: AbortSignal,
  ): Promise<IdentityApplicationSecurityModelRecord>;
}

export interface GenericIdentityApplicationSecurityScopeTypesClient {
  list(
    context: IdentityAdministrationContext,
    modelVersion: number,
    signal?: AbortSignal,
  ): Promise<readonly IdentityScopeTypeRecord[]>;

  add(
    context: IdentityAdministrationContext,
    modelVersion: number,
    request: IdentityAddScopeTypeRequest,
    signal?: AbortSignal,
  ): Promise<IdentityScopeTypeRecord>;
}

export interface GenericIdentityApplicationCapabilitiesClient {
  /**
   * Returns the capability catalog for one registered model, or null when the
   * model does not exist. The catalog is metadata; authorization remains a
   * server-side operation.
   */
  listForModel(
    context: IdentityAdministrationContext,
    modelVersion: number,
    signal?: AbortSignal,
  ): Promise<readonly IdentityApplicationSecurityCapabilityRecord[] | null>;
}

export interface GenericIdentityApplicationSecurityContextClient {
  get(
    context: IdentityAdministrationContext,
    signal?: AbortSignal,
  ): Promise<IdentityEffectiveAdministrationContext>;
}

export interface GenericIdentityApplicationSecurityClient {
  readonly models: GenericIdentityApplicationSecurityModelsClient;
  readonly manifests: GenericIdentityApplicationSecurityManifestsClient;
  readonly scopeTypes: GenericIdentityApplicationSecurityScopeTypesClient;
  readonly capabilities: GenericIdentityApplicationCapabilitiesClient;
  readonly context: GenericIdentityApplicationSecurityContextClient;
}

interface LegacyApplicationSecurityModelsClient {
  list(
    context: IdentityAdministrationContext,
    signal?: AbortSignal,
  ): Promise<readonly IdentityApplicationSecurityModelSummaryRecord[]>;
  get(
    context: IdentityAdministrationContext,
    modelVersion: number,
    signal?: AbortSignal,
  ): Promise<IdentityApplicationSecurityModelRecord | null>;
  registerManifest(
    context: IdentityAdministrationContext,
    request: IdentityApplicationSecurityManifestRequest,
    signal?: AbortSignal,
  ): Promise<IdentityApplicationSecurityModelRecord>;
  listScopeTypes(
    context: IdentityAdministrationContext,
    modelVersion: number,
    signal?: AbortSignal,
  ): Promise<readonly IdentityScopeTypeRecord[]>;
  addScopeType(
    context: IdentityAdministrationContext,
    modelVersion: number,
    request: IdentityAddScopeTypeRequest,
    signal?: AbortSignal,
  ): Promise<IdentityScopeTypeRecord>;
}

interface LegacyApplicationSecurityContextClient {
  get(
    context: IdentityAdministrationContext,
    signal?: AbortSignal,
  ): Promise<IdentityEffectiveAdministrationContext>;
}

/**
 * Composes the categorized Application Security SDK over the proven legacy
 * transport. No second security-model or authorization implementation is
 * introduced.
 */
export function createGenericIdentityApplicationSecurityClient(
  securityModels: LegacyApplicationSecurityModelsClient,
  contextClient: LegacyApplicationSecurityContextClient,
): GenericIdentityApplicationSecurityClient {
  return {
    models: {
      list: (context, signal) => securityModels.list(context, signal),
      get: (context, modelVersion, signal) => securityModels.get(context, modelVersion, signal),
    },
    manifests: {
      register: (context, request, signal) => securityModels.registerManifest(context, request, signal),
    },
    scopeTypes: {
      list: (context, modelVersion, signal) => securityModels.listScopeTypes(context, modelVersion, signal),
      add: (context, modelVersion, request, signal) => securityModels.addScopeType(context, modelVersion, request, signal),
    },
    capabilities: {
      listForModel: async (context, modelVersion, signal) => {
        const model = await securityModels.get(context, modelVersion, signal);
        return model?.capabilities ?? null;
      },
    },
    context: {
      get: (context, signal) => contextClient.get(context, signal),
    },
  };
}

/**
 * Produces a structured permission reference from a registered model.
 *
 * The function fails closed when the namespace or capability is not declared by
 * the model. It deliberately does not manufacture a TRN string or perform an
 * authorization decision.
 */
export function createApplicationSecurityPermissionReference(
  model: IdentityApplicationSecurityModelRecord,
  rbacNamespace: string,
  capability: Pick<IdentityApplicationSecurityCapabilityRecord, "resource" | "feature" | "action">,
): IdentityApplicationSecurityPermissionReference {
  if (!model.rbacNamespaces.includes(rbacNamespace)) {
    throw new Error(`RBAC namespace '${rbacNamespace}' is not declared by application security model ${model.modelVersion}.`);
  }

  const declared = model.capabilities.some(
    (candidate) =>
      candidate.resource === capability.resource &&
      candidate.feature === capability.feature &&
      candidate.action === capability.action,
  );
  if (!declared) {
    throw new Error(
      `Capability '${capability.resource}/${capability.feature}/${capability.action}' is not declared by application security model ${model.modelVersion}.`,
    );
  }

  return {
    applicationKey: model.applicationKey,
    modelVersion: model.modelVersion,
    rbacProject: model.rbacProject,
    rbacNamespace,
    resource: capability.resource,
    feature: capability.feature,
    action: capability.action,
  };
}
