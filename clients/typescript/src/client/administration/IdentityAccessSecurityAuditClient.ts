import type {
  IdentityAdministrationContext,
  IdentitySecurityAuditOutcome,
  IdentitySecurityAuditQuery,
  IdentitySecurityAuditRecord,
} from "../../admin-contracts.js";
import { IdentityAccessClientError } from "../../errors.js";
import { IdentityAccessPathBuilder } from "../IdentityAccessPathBuilder.js";
import { IdentityAccessValueCodec } from "../IdentityAccessValueCodec.js";
import { IdentityAccessAdministrationTransport } from "./IdentityAccessAdministrationTransport.js";

/** Read-only security-audit administration client. */
export class IdentityAccessSecurityAuditClient {
  readonly #admin: IdentityAccessAdministrationTransport;

  public constructor(admin: IdentityAccessAdministrationTransport) {
    this.#admin = admin;
  }

  public async list(
    context: IdentityAdministrationContext,
    query: IdentitySecurityAuditQuery = {},
    signal?: AbortSignal,
  ): Promise<readonly IdentitySecurityAuditRecord[]> {
    const path = `${IdentityAccessPathBuilder.administrationBasePath(context)}/security-audit${this.#query(query)}`;
    return this.#admin.get(
      path,
      context,
      (value) => IdentityAccessValueCodec.array(value, IdentityAccessSecurityAuditClient.#record),
      signal,
    );
  }

  #query(value: IdentitySecurityAuditQuery): string {
    const query = new URLSearchParams();
    if (value.tenantId !== undefined) query.set("tenantId", IdentityAccessValueCodec.uuid(value.tenantId));
    if (value.userId !== undefined) query.set("userId", IdentityAccessValueCodec.uuid(value.userId));
    if (value.eventType !== undefined) query.set("eventType", IdentityAccessValueCodec.nonEmpty(value.eventType));
    if (value.outcome !== undefined) query.set("outcome", IdentityAccessSecurityAuditClient.#outcome(value.outcome));
    if (value.correlationId !== undefined) {
      const correlationId = IdentityAccessValueCodec.nonEmpty(value.correlationId);
      if (correlationId.length > 64) throw new IdentityAccessClientError("configuration");
      query.set("correlationId", correlationId);
    }
    if (value.offset !== undefined) {
      if (!Number.isSafeInteger(value.offset) || value.offset < 0) throw new IdentityAccessClientError("configuration");
      query.set("offset", String(value.offset));
    }
    if (value.limit !== undefined) {
      if (!Number.isSafeInteger(value.limit) || value.limit < 1 || value.limit > 200) throw new IdentityAccessClientError("configuration");
      query.set("limit", String(value.limit));
    }
    const result = query.toString();
    return result.length === 0 ? "" : `?${result}`;
  }

  static #record(value: unknown): IdentitySecurityAuditRecord {
    const data = IdentityAccessValueCodec.object(value);
    const applicationKey = IdentityAccessSecurityAuditClient.#optionalText(data.applicationKey);
    const clientId = IdentityAccessSecurityAuditClient.#optionalText(data.clientId);
    const targetId = IdentityAccessSecurityAuditClient.#optionalText(data.targetId);
    const reasonCode = IdentityAccessSecurityAuditClient.#optionalText(data.reasonCode);
    const correlationId = IdentityAccessSecurityAuditClient.#optionalText(data.correlationId);
    return {
      eventId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.eventId)),
      occurredAt: IdentityAccessValueCodec.timestamp(data.occurredAt),
      eventType: IdentityAccessValueCodec.text(data.eventType),
      outcome: IdentityAccessSecurityAuditClient.#outcome(IdentityAccessValueCodec.text(data.outcome)),
      identityScopeId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.identityScopeId)),
      ...(data.tenantId === null || data.tenantId === undefined ? {} : { tenantId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.tenantId)) }),
      ...(data.userId === null || data.userId === undefined ? {} : { userId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.userId)) }),
      ...(applicationKey === undefined ? {} : { applicationKey }),
      ...(clientId === undefined ? {} : { clientId }),
      ...(targetId === undefined ? {} : { targetId }),
      ...(reasonCode === undefined ? {} : { reasonCode }),
      ...(correlationId === undefined ? {} : { correlationId }),
    };
  }

  static #optionalText(value: unknown): string | undefined {
    return value === null || value === undefined ? undefined : IdentityAccessValueCodec.text(value);
  }

  static #outcome(value: string): IdentitySecurityAuditOutcome {
    if (value !== "Succeeded" && value !== "Denied" && value !== "Failed") {
      throw new IdentityAccessClientError("protocol");
    }
    return value;
  }
}
