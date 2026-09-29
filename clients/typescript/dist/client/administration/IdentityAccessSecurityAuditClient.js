var _a;
import { IdentityAccessClientError } from "../../errors.js";
import { IdentityAccessPathBuilder } from "../IdentityAccessPathBuilder.js";
import { IdentityAccessValueCodec } from "../IdentityAccessValueCodec.js";
/** Read-only security-audit administration client. */
export class IdentityAccessSecurityAuditClient {
    #admin;
    constructor(admin) {
        this.#admin = admin;
    }
    async list(context, query = {}, signal) {
        const path = `${IdentityAccessPathBuilder.administrationBasePath(context)}/security-audit${this.#query(query)}`;
        return this.#admin.get(path, context, (value) => IdentityAccessValueCodec.array(value, _a.#record), signal);
    }
    #query(value) {
        const query = new URLSearchParams();
        if (value.tenantId !== undefined)
            query.set("tenantId", IdentityAccessValueCodec.uuid(value.tenantId));
        if (value.userId !== undefined)
            query.set("userId", IdentityAccessValueCodec.uuid(value.userId));
        if (value.eventType !== undefined)
            query.set("eventType", IdentityAccessValueCodec.nonEmpty(value.eventType));
        if (value.outcome !== undefined)
            query.set("outcome", _a.#outcome(value.outcome));
        if (value.correlationId !== undefined) {
            const correlationId = IdentityAccessValueCodec.nonEmpty(value.correlationId);
            if (correlationId.length > 64)
                throw new IdentityAccessClientError("configuration");
            query.set("correlationId", correlationId);
        }
        if (value.offset !== undefined) {
            if (!Number.isSafeInteger(value.offset) || value.offset < 0)
                throw new IdentityAccessClientError("configuration");
            query.set("offset", String(value.offset));
        }
        if (value.limit !== undefined) {
            if (!Number.isSafeInteger(value.limit) || value.limit < 1 || value.limit > 200)
                throw new IdentityAccessClientError("configuration");
            query.set("limit", String(value.limit));
        }
        const result = query.toString();
        return result.length === 0 ? "" : `?${result}`;
    }
    static #record(value) {
        const data = IdentityAccessValueCodec.object(value);
        const applicationKey = _a.#optionalText(data.applicationKey);
        const clientId = _a.#optionalText(data.clientId);
        const targetId = _a.#optionalText(data.targetId);
        const reasonCode = _a.#optionalText(data.reasonCode);
        const correlationId = _a.#optionalText(data.correlationId);
        return {
            eventId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.eventId)),
            occurredAt: IdentityAccessValueCodec.timestamp(data.occurredAt),
            eventType: IdentityAccessValueCodec.text(data.eventType),
            outcome: _a.#outcome(IdentityAccessValueCodec.text(data.outcome)),
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
    static #optionalText(value) {
        return value === null || value === undefined ? undefined : IdentityAccessValueCodec.text(value);
    }
    static #outcome(value) {
        if (value !== "Succeeded" && value !== "Denied" && value !== "Failed") {
            throw new IdentityAccessClientError("protocol");
        }
        return value;
    }
}
_a = IdentityAccessSecurityAuditClient;
