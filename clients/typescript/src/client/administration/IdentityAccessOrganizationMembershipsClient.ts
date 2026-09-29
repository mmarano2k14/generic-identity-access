import type {
  IdentityAdministrationListOptions,
  IdentityOrganizationMembershipRecord,
  IdentityTenantAdministrationContext,
} from "../../admin-contracts.js";
import { IdentityAccessAdministrationCodec } from "../IdentityAccessAdministrationCodec.js";
import { IdentityAccessPathBuilder } from "../IdentityAccessPathBuilder.js";
import { IdentityAccessValueCodec } from "../IdentityAccessValueCodec.js";
import { IdentityAccessAdministrationTransport } from "./IdentityAccessAdministrationTransport.js";

/** Explicit OrganizationMembership client. Belonging never implies authorization. */
export class IdentityAccessOrganizationMembershipsClient {
  readonly #admin: IdentityAccessAdministrationTransport;

  public constructor(admin: IdentityAccessAdministrationTransport) {
    this.#admin = admin;
  }

  public async listForOrganization(
    context: IdentityTenantAdministrationContext,
    organizationIdValue: string,
    options?: IdentityAdministrationListOptions,
    signal?: AbortSignal,
  ): Promise<readonly IdentityOrganizationMembershipRecord[]> {
    const organizationId = IdentityAccessValueCodec.uuid(organizationIdValue);
    const path = `${IdentityAccessPathBuilder.organizationDirectoryPath(context)}/${organizationId}/memberships${IdentityAccessPathBuilder.administrationListQuery(options)}`;
    return this.#admin.get(
      path,
      context,
      (value) => IdentityAccessValueCodec.array(value, IdentityAccessAdministrationCodec.organizationMembershipRecord),
      signal,
    );
  }

  public async listForTenantMembership(
    context: IdentityTenantAdministrationContext,
    tenantMembershipIdValue: string,
    options?: IdentityAdministrationListOptions,
    signal?: AbortSignal,
  ): Promise<readonly IdentityOrganizationMembershipRecord[]> {
    const path = `${IdentityAccessPathBuilder.tenantMembershipOrganizationsPath(context, tenantMembershipIdValue)}${IdentityAccessPathBuilder.administrationListQuery(options)}`;
    return this.#admin.get(
      path,
      context,
      (value) => IdentityAccessValueCodec.array(value, IdentityAccessAdministrationCodec.organizationMembershipRecord),
      signal,
    );
  }

  public async get(
    context: IdentityTenantAdministrationContext,
    organizationIdValue: string,
    tenantMembershipIdValue: string,
    signal?: AbortSignal,
  ): Promise<IdentityOrganizationMembershipRecord | null> {
    const organizationId = IdentityAccessValueCodec.uuid(organizationIdValue);
    const tenantMembershipId = IdentityAccessValueCodec.uuid(tenantMembershipIdValue);
    const path = `${IdentityAccessPathBuilder.organizationDirectoryPath(context)}/${organizationId}/memberships/${tenantMembershipId}`;
    return this.#admin.getNullable(path, context, IdentityAccessAdministrationCodec.organizationMembershipRecord, signal);
  }

  public async add(
    context: IdentityTenantAdministrationContext,
    organizationIdValue: string,
    tenantMembershipIdValue: string,
    signal?: AbortSignal,
  ): Promise<IdentityOrganizationMembershipRecord> {
    const organizationId = IdentityAccessValueCodec.uuid(organizationIdValue);
    const tenantMembershipId = IdentityAccessValueCodec.uuid(tenantMembershipIdValue);
    const path = `${IdentityAccessPathBuilder.organizationDirectoryPath(context)}/${organizationId}/memberships`;
    return this.#admin.post(
      path,
      context,
      { tenantMembershipId },
      IdentityAccessAdministrationCodec.organizationMembershipRecord,
      signal,
    );
  }

  public async activate(
    context: IdentityTenantAdministrationContext,
    organizationIdValue: string,
    tenantMembershipIdValue: string,
    expectedRowVersion: number,
    signal?: AbortSignal,
  ): Promise<IdentityOrganizationMembershipRecord> {
    return this.#lifecycle(
      context,
      organizationIdValue,
      tenantMembershipIdValue,
      "activate",
      expectedRowVersion,
      signal,
    );
  }

  public async suspend(
    context: IdentityTenantAdministrationContext,
    organizationIdValue: string,
    tenantMembershipIdValue: string,
    expectedRowVersion: number,
    signal?: AbortSignal,
  ): Promise<IdentityOrganizationMembershipRecord> {
    return this.#lifecycle(
      context,
      organizationIdValue,
      tenantMembershipIdValue,
      "suspend",
      expectedRowVersion,
      signal,
    );
  }

  public async remove(
    context: IdentityTenantAdministrationContext,
    organizationIdValue: string,
    tenantMembershipIdValue: string,
    expectedRowVersion: number,
    signal?: AbortSignal,
  ): Promise<boolean> {
    const organizationId = IdentityAccessValueCodec.uuid(organizationIdValue);
    const tenantMembershipId = IdentityAccessValueCodec.uuid(tenantMembershipIdValue);
    const version = IdentityAccessValueCodec.version(expectedRowVersion);
    const path = `${IdentityAccessPathBuilder.organizationDirectoryPath(context)}/${organizationId}/memberships/${tenantMembershipId}?expectedRowVersion=${version}`;
    return this.#admin.delete(path, context, signal);
  }

  async #lifecycle(
    context: IdentityTenantAdministrationContext,
    organizationIdValue: string,
    tenantMembershipIdValue: string,
    action: "activate" | "suspend",
    expectedRowVersion: number,
    signal?: AbortSignal,
  ): Promise<IdentityOrganizationMembershipRecord> {
    const organizationId = IdentityAccessValueCodec.uuid(organizationIdValue);
    const tenantMembershipId = IdentityAccessValueCodec.uuid(tenantMembershipIdValue);
    const path = `${IdentityAccessPathBuilder.organizationDirectoryPath(context)}/${organizationId}/memberships/${tenantMembershipId}/${action}`;
    return this.#admin.postJsonAction(
      path,
      context,
      { expectedRowVersion: IdentityAccessValueCodec.version(expectedRowVersion) },
      IdentityAccessAdministrationCodec.organizationMembershipRecord,
      signal,
    );
  }
}
