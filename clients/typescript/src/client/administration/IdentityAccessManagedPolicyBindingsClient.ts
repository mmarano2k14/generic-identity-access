import type {
  IdentityAddManagedGroupPolicyBindingRequest,
  IdentityAdministrationListOptions,
  IdentityManagedGroupPolicyBindingRecord,
  IdentityManagedPolicyRecord,
  IdentityTenantAdministrationContext,
} from "../../admin-contracts.js";
import { IdentityAccessAdministrationCodec } from "../IdentityAccessAdministrationCodec.js";
import { IdentityAccessPathBuilder } from "../IdentityAccessPathBuilder.js";
import { IdentityAccessValueCodec } from "../IdentityAccessValueCodec.js";
import { IdentityAccessAdministrationTransport } from "./IdentityAccessAdministrationTransport.js";

/** Tenant-scoped client for attaching shared managed-policy versions to groups. */
export class IdentityAccessManagedPolicyBindingsClient {
  readonly #admin: IdentityAccessAdministrationTransport;

  public constructor(admin: IdentityAccessAdministrationTransport) {
    this.#admin = admin;
  }

  public async listAvailablePolicies(
    context: IdentityTenantAdministrationContext,
    options?: IdentityAdministrationListOptions,
    signal?: AbortSignal,
  ): Promise<readonly IdentityManagedPolicyRecord[]> {
    const path = `${this.basePath(context)}/available-policies${IdentityAccessPathBuilder.administrationListQuery(options)}`;
    return this.#admin.get(
      path,
      context,
      (value) => IdentityAccessValueCodec.array(value, IdentityAccessAdministrationCodec.managedPolicyRecord),
      signal,
    );
  }

  public async list(
    context: IdentityTenantAdministrationContext,
    groupIdValue: string,
    signal?: AbortSignal,
  ): Promise<readonly IdentityManagedGroupPolicyBindingRecord[]> {
    const path = `${this.basePath(context)}/groups/${IdentityAccessValueCodec.uuid(groupIdValue)}`;
    return this.#admin.get(
      path,
      context,
      (value) => IdentityAccessValueCodec.array(value, IdentityAccessAdministrationCodec.managedGroupPolicyBindingRecord),
      signal,
    );
  }

  public async add(
    context: IdentityTenantAdministrationContext,
    groupIdValue: string,
    request: IdentityAddManagedGroupPolicyBindingRequest,
    signal?: AbortSignal,
  ): Promise<IdentityManagedGroupPolicyBindingRecord> {
    const path = `${this.basePath(context)}/groups/${IdentityAccessValueCodec.uuid(groupIdValue)}`;
    return this.#admin.post(
      path,
      context,
      {
        policyId: IdentityAccessValueCodec.uuid(request.policyId),
        policyVersion: request.policyVersion === undefined
          ? null
          : IdentityAccessValueCodec.positiveInteger(request.policyVersion),
        resourceScopeId: request.resourceScopeId === undefined
          ? null
          : IdentityAccessValueCodec.uuid(request.resourceScopeId),
        includeDescendants: request.includeDescendants ?? false,
      },
      (value) => IdentityAccessAdministrationCodec.managedGroupPolicyBindingRecord(value),
      signal,
    );
  }

  public async remove(
    context: IdentityTenantAdministrationContext,
    groupIdValue: string,
    policyIdValue: string,
    policyVersion: number,
    resourceScopeIdValue?: string,
    signal?: AbortSignal,
  ): Promise<boolean> {
    const query = resourceScopeIdValue === undefined
      ? ""
      : `?resourceScopeId=${encodeURIComponent(IdentityAccessValueCodec.uuid(resourceScopeIdValue))}`;
    const path = `${this.basePath(context)}/groups/${IdentityAccessValueCodec.uuid(groupIdValue)}/${IdentityAccessValueCodec.uuid(policyIdValue)}/versions/${IdentityAccessValueCodec.positiveInteger(policyVersion)}${query}`;
    return this.#admin.delete(path, context, signal);
  }

  private basePath(context: IdentityTenantAdministrationContext): string {
    return `${IdentityAccessPathBuilder.tenantApplicationPath(context)}/managed-policy-bindings`;
  }
}
