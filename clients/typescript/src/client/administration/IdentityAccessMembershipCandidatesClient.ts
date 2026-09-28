import type {
  IdentityTenantAdministrationContext,
  IdentityTenantMembershipCandidateRecord,
  IdentityTenantMembershipRecord,
  IdentityMembershipStatus,
} from "../../admin-contracts.js";
import { IdentityAccessAdministrationCodec } from "../IdentityAccessAdministrationCodec.js";
import { IdentityAccessPathBuilder } from "../IdentityAccessPathBuilder.js";
import { IdentityAccessValueCodec } from "../IdentityAccessValueCodec.js";
import { IdentityAccessAdministrationTransport } from "./IdentityAccessAdministrationTransport.js";

/** Exact-login candidate lookup for controlled tenant membership creation. */
export class IdentityAccessMembershipCandidatesClient {
  readonly #admin: IdentityAccessAdministrationTransport;

  public constructor(admin: IdentityAccessAdministrationTransport) {
    this.#admin = admin;
  }

  public async findByLogin(
    context: IdentityTenantAdministrationContext,
    loginIdentifier: string,
    signal?: AbortSignal,
  ): Promise<IdentityTenantMembershipCandidateRecord | null> {
    const login = IdentityAccessValueCodec.nonEmpty(loginIdentifier);
    const path = `${IdentityAccessPathBuilder.administrationBasePath(context)}/tenants/${IdentityAccessValueCodec.uuid(context.tenantId)}/membership-candidates/by-login?loginIdentifier=${encodeURIComponent(login)}`;
    return this.#admin.getNullable(
      path,
      context,
      (value) => IdentityAccessAdministrationCodec.tenantMembershipCandidateRecord(value),
      signal,
    );
  }

  public async createMembershipByLogin(
    context: IdentityTenantAdministrationContext,
    loginIdentifier: string,
    status: IdentityMembershipStatus = 1,
    signal?: AbortSignal,
  ): Promise<IdentityTenantMembershipRecord> {
    const login = IdentityAccessValueCodec.nonEmpty(loginIdentifier);
    const path = `${IdentityAccessPathBuilder.administrationBasePath(context)}/tenants/${IdentityAccessValueCodec.uuid(context.tenantId)}/membership-candidates/by-login/membership`;
    return this.#admin.post(
      path,
      context,
      {
        loginIdentifier: login,
        status: IdentityAccessValueCodec.lifecycleStatus(status),
      },
      (value) => IdentityAccessAdministrationCodec.tenantMembershipRecord(value),
      signal,
    );
  }

}
