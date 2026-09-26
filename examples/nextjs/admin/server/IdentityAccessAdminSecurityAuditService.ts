import "server-only";
import type { IdentitySecurityAuditRecord } from "@identity-access/client";
import { IdentityAccessAdminRequest } from "./IdentityAccessAdminRequest";
import { IdentityAccessAdminSecurityAuditQuery } from "./IdentityAccessAdminSecurityAuditQuery";

/** Loads authorized audit records through the focused TypeScript administration client. */
export class IdentityAccessAdminSecurityAuditService {
  readonly #request: IdentityAccessAdminRequest;

  public constructor(request: IdentityAccessAdminRequest) {
    this.#request = request;
  }

  public async list(query: IdentityAccessAdminSecurityAuditQuery): Promise<readonly IdentitySecurityAuditRecord[]> {
    if (query.validationMessage !== undefined) return [];
    return this.#request.client.administration.securityAudit.list(
      this.#request.administrationContext,
      query.toClientQuery(),
    );
  }
}
