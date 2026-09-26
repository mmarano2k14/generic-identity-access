import "server-only";
import type {
  IdentityManagedPolicyRecord,
  IdentityManagedPolicyStatementRecord,
  IdentityManagedPolicyVersionRecord,
} from "@identity-access/client";
import type { AdminActionFailure } from "../contracts/AdminActionState";
import { IdentityAccessAdminFailurePresentation } from "./IdentityAccessAdminFailurePresentation";
import {
  IdentityAccessAdminPolicyBuilderService,
  type IdentityAccessAdminPolicyModelOption,
} from "./IdentityAccessAdminPolicyBuilderService";
import { IdentityAccessAdminRequest } from "./IdentityAccessAdminRequest";

export interface IdentityAccessAdminManagedPolicyReadResult {
  readonly policies: readonly IdentityManagedPolicyRecord[];
  readonly selectedPolicy: IdentityManagedPolicyRecord | null;
  readonly versions: readonly IdentityManagedPolicyVersionRecord[];
  readonly selectedVersion: IdentityManagedPolicyVersionRecord | null;
  readonly statements: readonly IdentityManagedPolicyStatementRecord[];
  readonly policyBuilderModels: readonly IdentityAccessAdminPolicyModelOption[];
  readonly failure?: AdminActionFailure;
}

/** Server-only read orchestration for the shared managed-policy catalog workspace. */
export class IdentityAccessAdminManagedPolicyReadService {
  readonly #request: IdentityAccessAdminRequest;

  public constructor(request: IdentityAccessAdminRequest) {
    this.#request = request;
  }

  public async load(policyId?: string, policyVersion?: string): Promise<IdentityAccessAdminManagedPolicyReadResult> {
    try {
      const context = this.#request.administrationContext;
      const policies = await this.#request.client.administration.managedPolicies.list(context, { limit: 50 });
      const selectedPolicy = policyId
        ? policies.find((policy) => policy.policyId === policyId)
          ?? await this.#request.client.administration.managedPolicies.get(context, policyId)
        : null;
      const versions = selectedPolicy
        ? await this.#request.client.administration.managedPolicies.listVersions(context, selectedPolicy.policyId)
        : [];
      const requestedVersion = IdentityAccessAdminManagedPolicyReadService.requestedVersion(policyVersion);
      const selectedVersion = selectedPolicy
        ? await this.#selectVersion(selectedPolicy, versions, requestedVersion)
        : null;
      const statements = selectedPolicy && selectedVersion
        ? await this.#request.client.administration.managedPolicies.listStatements(
          context,
          selectedPolicy.policyId,
          selectedVersion.policyVersion,
        )
        : [];
      const policyBuilderModels = selectedPolicy
        ? await new IdentityAccessAdminPolicyBuilderService(this.#request).listModels()
        : [];

      return { policies, selectedPolicy, versions, selectedVersion, statements, policyBuilderModels };
    } catch (error) {
      return {
        policies: [],
        selectedPolicy: null,
        versions: [],
        selectedVersion: null,
        statements: [],
        policyBuilderModels: [],
        failure: IdentityAccessAdminFailurePresentation.fromRead(error),
      };
    }
  }

  async #selectVersion(
    policy: IdentityManagedPolicyRecord,
    versions: readonly IdentityManagedPolicyVersionRecord[],
    requestedVersion?: number,
  ): Promise<IdentityManagedPolicyVersionRecord | null> {
    const versionNumber = requestedVersion
      ?? policy.defaultVersion
      ?? versions.at(-1)?.policyVersion;
    if (versionNumber === undefined) return null;

    return versions.find((version) => version.policyVersion === versionNumber)
      ?? await this.#request.client.administration.managedPolicies.getVersion(
        this.#request.administrationContext,
        policy.policyId,
        versionNumber,
      );
  }

  private static requestedVersion(value?: string): number | undefined {
    if (value === undefined || value.trim() === "") return undefined;
    const parsed = Number(value);
    return Number.isSafeInteger(parsed) && parsed > 0 ? parsed : undefined;
  }
}
