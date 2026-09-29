import { IdentityAccessClientError } from "../../errors.js";
import { IdentityAccessPathBuilder } from "../IdentityAccessPathBuilder.js";
import { IdentityAccessValueCodec } from "../IdentityAccessValueCodec.js";
/** Provider-neutral MFA administration client. */
export class IdentityAccessMfaClient {
    #admin;
    constructor(admin) {
        this.#admin = admin;
    }
    async listProviders(context, signal) {
        return this.#admin.get(`${this.#base(context)}/providers`, context, (value) => IdentityAccessValueCodec.array(value, IdentityAccessMfaClient.providerRecord), signal);
    }
    async getPolicy(context, signal) {
        return this.#admin.getNullable(`${this.#base(context)}/policy`, context, (value) => IdentityAccessMfaClient.policyRecord(value), signal);
    }
    async createPolicy(context, request, signal) {
        return this.#admin.post(`${this.#base(context)}/policy`, context, {
            mode: IdentityAccessMfaClient.policyMode(request.mode),
            allowedProviders: request.allowedProviders.map(IdentityAccessValueCodec.slug),
        }, (value) => IdentityAccessMfaClient.policyRecord(value), signal);
    }
    async updatePolicy(context, request, signal) {
        return this.#admin.put(`${this.#base(context)}/policy`, context, {
            mode: IdentityAccessMfaClient.policyMode(request.mode),
            allowedProviders: request.allowedProviders.map(IdentityAccessValueCodec.slug),
            expectedVersion: IdentityAccessValueCodec.version(request.expectedVersion),
        }, (value) => IdentityAccessMfaClient.policyRecord(value), signal);
    }
    async listAuthenticators(context, userIdValue, signal) {
        const userId = IdentityAccessValueCodec.uuid(userIdValue);
        return this.#admin.get(`${this.#base(context)}/users/${userId}/authenticators`, context, (value) => IdentityAccessValueCodec.array(value, IdentityAccessMfaClient.authenticatorRecord), signal);
    }
    async getUserSecurityState(context, userIdValue, signal) {
        const userId = IdentityAccessValueCodec.uuid(userIdValue);
        return this.#admin.get(`${this.#base(context)}/users/${userId}/state`, context, (value) => IdentityAccessMfaClient.userSecurityState(value), signal);
    }
    async revokeAuthenticator(context, userIdValue, authenticatorIdValue, expectedVersionValue, signal) {
        const userId = IdentityAccessValueCodec.uuid(userIdValue);
        const authenticatorId = IdentityAccessValueCodec.uuid(authenticatorIdValue);
        const expectedVersion = IdentityAccessValueCodec.version(expectedVersionValue);
        return this.#admin.deleteJson(`${this.#base(context)}/users/${userId}/authenticators/${authenticatorId}?expectedVersion=${expectedVersion}`, context, (value) => IdentityAccessMfaClient.authenticatorRecord(value), signal);
    }
    async revokeAuthenticatorForRecovery(context, userIdValue, authenticatorIdValue, expectedVersionValue, signal) {
        const userId = IdentityAccessValueCodec.uuid(userIdValue);
        const authenticatorId = IdentityAccessValueCodec.uuid(authenticatorIdValue);
        const expectedVersion = IdentityAccessValueCodec.version(expectedVersionValue);
        return this.#admin.postAction(`${this.#base(context)}/users/${userId}/authenticators/${authenticatorId}/recovery-revoke?expectedVersion=${expectedVersion}`, context, (value) => IdentityAccessMfaClient.authenticatorRecord(value), signal);
    }
    #base(context) {
        return `${IdentityAccessPathBuilder.administrationBasePath(context)}/mfa`;
    }
    static providerRecord(value) {
        const data = IdentityAccessValueCodec.object(value);
        if (!Array.isArray(data.capabilities))
            throw new IdentityAccessClientError("protocol");
        return {
            key: IdentityAccessValueCodec.slug(IdentityAccessValueCodec.text(data.key)),
            displayName: IdentityAccessValueCodec.text(data.displayName),
            capabilities: data.capabilities.map(IdentityAccessMfaClient.providerCapability),
        };
    }
    static providerCapability(value) {
        const capability = IdentityAccessValueCodec.text(value);
        if (capability !== "enrollment" && capability !== "verification" && capability !== "recovery") {
            throw new IdentityAccessClientError("protocol");
        }
        return capability;
    }
    static policyRecord(value) {
        const data = IdentityAccessValueCodec.object(value);
        if (!Array.isArray(data.allowedProviders))
            throw new IdentityAccessClientError("protocol");
        return {
            mode: IdentityAccessMfaClient.policyMode(data.mode),
            allowedProviders: data.allowedProviders.map((item) => IdentityAccessValueCodec.slug(IdentityAccessValueCodec.text(item))),
            version: IdentityAccessValueCodec.version(data.version),
        };
    }
    static userSecurityState(value) {
        const data = IdentityAccessValueCodec.object(value);
        const policyMode = data.policyMode === null || data.policyMode === undefined
            ? undefined
            : IdentityAccessMfaClient.policyMode(data.policyMode);
        const providers = (input) => IdentityAccessValueCodec.array(input, (item) => IdentityAccessValueCodec.slug(IdentityAccessValueCodec.text(item)));
        return {
            policyConfigured: IdentityAccessValueCodec.boolean(data.policyConfigured),
            ...(policyMode === undefined ? {} : { policyMode }),
            mfaRequired: IdentityAccessValueCodec.boolean(data.mfaRequired),
            hasActiveVerificationFactor: IdentityAccessValueCodec.boolean(data.hasActiveVerificationFactor),
            hasActivePrimaryFactor: IdentityAccessValueCodec.boolean(data.hasActivePrimaryFactor),
            hasActiveRecoveryFactor: IdentityAccessValueCodec.boolean(data.hasActiveRecoveryFactor),
            satisfiesCurrentPolicy: IdentityAccessValueCodec.boolean(data.satisfiesCurrentPolicy),
            activeVerificationProviders: providers(data.activeVerificationProviders),
            activePrimaryProviders: providers(data.activePrimaryProviders),
            activeRecoveryProviders: providers(data.activeRecoveryProviders),
        };
    }
    static authenticatorRecord(value) {
        const data = IdentityAccessValueCodec.object(value);
        const confirmedAt = IdentityAccessMfaClient.optionalTimestamp(data.confirmedAt);
        const lastUsedAt = IdentityAccessMfaClient.optionalTimestamp(data.lastUsedAt);
        const revokedAt = IdentityAccessMfaClient.optionalTimestamp(data.revokedAt);
        return {
            authenticatorId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.authenticatorId)),
            userId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.userId)),
            providerKey: IdentityAccessValueCodec.slug(IdentityAccessValueCodec.text(data.providerKey)),
            displayName: IdentityAccessValueCodec.text(data.displayName),
            status: IdentityAccessMfaClient.authenticatorStatus(data.status),
            createdAt: IdentityAccessValueCodec.timestamp(data.createdAt),
            ...(confirmedAt === undefined ? {} : { confirmedAt }),
            ...(lastUsedAt === undefined ? {} : { lastUsedAt }),
            ...(revokedAt === undefined ? {} : { revokedAt }),
            version: IdentityAccessValueCodec.version(data.version),
        };
    }
    static optionalTimestamp(value) {
        return value === null || value === undefined ? undefined : IdentityAccessValueCodec.timestamp(value);
    }
    static policyMode(value) {
        if (value !== 1 && value !== 2 && value !== 3)
            throw new IdentityAccessClientError("protocol");
        return value;
    }
    static authenticatorStatus(value) {
        if (value !== 1 && value !== 2 && value !== 3)
            throw new IdentityAccessClientError("protocol");
        return value;
    }
}
