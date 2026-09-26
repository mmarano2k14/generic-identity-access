import "server-only";
import type { IdentityApplicationSecurityModelRecord } from "@identity-access/client";
import { IdentityAccessAdminRequest } from "./IdentityAccessAdminRequest";

export interface IdentityAccessAdminPolicyCapabilityOption {
  readonly value: string;
  readonly label: string;
  readonly trnPreviews: readonly string[];
}

export interface IdentityAccessAdminPolicyModelOption {
  readonly modelVersion: number;
  readonly rbacProject: string;
  readonly rbacNamespaces: readonly string[];
  readonly manifestSha256: string;
  readonly capabilities: readonly IdentityAccessAdminPolicyCapabilityOption[];
}

/** Server-only read orchestration for the policy builder capability catalog. */
export class IdentityAccessAdminPolicyBuilderService {
  readonly #request: IdentityAccessAdminRequest;

  public constructor(request: IdentityAccessAdminRequest) {
    this.#request = request;
  }

  public async listModels(): Promise<readonly IdentityAccessAdminPolicyModelOption[]> {
    const context = this.#request.administrationContext;
    const summaries = await this.#request.client.administration.securityModels.list(context);
    const models = await Promise.all(
      summaries.map((summary) => this.#request.client.administration.securityModels.get(context, summary.modelVersion)),
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
}
