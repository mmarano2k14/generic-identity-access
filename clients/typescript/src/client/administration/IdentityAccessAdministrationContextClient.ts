import type {
  IdentityAdministrationContext,
  IdentityEffectiveAdministrationContext,
} from "../../admin-contracts.js";
import { IdentityAccessAdministrationCodec } from "../IdentityAccessAdministrationCodec.js";
import { IdentityAccessPathBuilder } from "../IdentityAccessPathBuilder.js";
import { IdentityAccessAdministrationTransport } from "./IdentityAccessAdministrationTransport.js";

/** Reads the server-trusted effective administration context for the current credential. */
export class IdentityAccessAdministrationContextClient {
  readonly #admin: IdentityAccessAdministrationTransport;

  public constructor(admin: IdentityAccessAdministrationTransport) {
    this.#admin = admin;
  }

  public async get(
    context: IdentityAdministrationContext,
    signal?: AbortSignal,
  ): Promise<IdentityEffectiveAdministrationContext> {
    const path = `${IdentityAccessPathBuilder.administrationBasePath(context)}/administration-context`;
    return this.#admin.get(
      path,
      context,
      (value) => IdentityAccessAdministrationCodec.effectiveAdministrationContext(value),
      signal,
    );
  }
}
