import type {
  IdentityTenantAdministrationContext,
  IdentityTenantGroupAssignmentRecord,
} from "../../admin-contracts.js";
import { IdentityAccessAdministrationCodec } from "../IdentityAccessAdministrationCodec.js";
import { IdentityAccessPathBuilder } from "../IdentityAccessPathBuilder.js";
import { IdentityAccessValueCodec } from "../IdentityAccessValueCodec.js";
import { IdentityAccessAdministrationTransport } from "./IdentityAccessAdministrationTransport.js";

/** Aggregate tenant read surface for group assignments. */
export class IdentityAccessTenantGroupAssignmentsClient {
  readonly #admin: IdentityAccessAdministrationTransport;

  public constructor(admin: IdentityAccessAdministrationTransport) {
    this.#admin = admin;
  }

  public async list(
    context: IdentityTenantAdministrationContext,
    signal?: AbortSignal,
  ): Promise<readonly IdentityTenantGroupAssignmentRecord[]> {
    return this.#admin.get(
      `${IdentityAccessPathBuilder.tenantApplicationPath(context)}/group-memberships`,
      context,
      (value) => IdentityAccessValueCodec.array(value, IdentityAccessAdministrationCodec.tenantGroupAssignmentRecord),
      signal,
    );
  }
}
