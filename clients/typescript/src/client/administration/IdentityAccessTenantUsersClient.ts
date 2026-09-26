import type {
  IdentityTenantAdministrationContext,
  IdentityTenantUserListOptions,
  IdentityTenantUserRecord,
} from "../../admin-contracts.js";
import { IdentityAccessAdministrationCodec } from "../IdentityAccessAdministrationCodec.js";
import { IdentityAccessPathBuilder } from "../IdentityAccessPathBuilder.js";
import { IdentityAccessValueCodec } from "../IdentityAccessValueCodec.js";
import { IdentityAccessAdministrationTransport } from "./IdentityAccessAdministrationTransport.js";

/** Reads tenant-constrained user projections without exposing the scope-wide directory. */
export class IdentityAccessTenantUsersClient {
  readonly #admin: IdentityAccessAdministrationTransport;

  public constructor(admin: IdentityAccessAdministrationTransport) {
    this.#admin = admin;
  }

  public async list(
    context: IdentityTenantAdministrationContext,
    options?: IdentityTenantUserListOptions,
    signal?: AbortSignal,
  ): Promise<readonly IdentityTenantUserRecord[]> {
    const path = `${IdentityAccessPathBuilder.tenantUsersPath(context)}${IdentityAccessPathBuilder.tenantUserListQuery(options)}`;
    return this.#admin.get(
      path,
      context,
      (value) => IdentityAccessValueCodec.array(value, IdentityAccessAdministrationCodec.tenantUserRecord),
      signal,
    );
  }
}
