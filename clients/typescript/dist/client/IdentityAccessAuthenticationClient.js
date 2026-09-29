import { IdentityAccessClientError } from "../errors.js";
import { IdentityAccessValueCodec } from "./IdentityAccessValueCodec.js";
/** Local password/session authentication lifecycle and session-bound MFA step-up. */
export class IdentityAccessAuthenticationClient {
    #transport;
    constructor(transport) {
        this.#transport = transport;
    }
    async passwordLogin(request, signal) {
        const clientId = IdentityAccessValueCodec.clientId(request.clientId);
        const redirectUri = IdentityAccessValueCodec.redirectUri(request.redirectUri);
        const loginIdentifier = IdentityAccessValueCodec.nonEmpty(request.loginIdentifier);
        const password = IdentityAccessValueCodec.nonEmptySecret(request.password);
        return this.#transport.requestJson(`api/v1/authentication/clients/${encodeURIComponent(clientId)}/password-login`, {
            method: "POST",
            acceptedStatuses: [200],
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ loginIdentifier, password, redirectUri }),
            ...(signal === undefined ? {} : { signal }),
        }, (value) => {
            const data = IdentityAccessValueCodec.object(value);
            const returnedRedirectUri = IdentityAccessValueCodec.redirectUri(IdentityAccessValueCodec.text(data.redirectUri));
            if (returnedRedirectUri !== redirectUri) {
                throw new IdentityAccessClientError("protocol");
            }
            return {
                kind: "session",
                clientId,
                userId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.userId)),
                sessionId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.sessionId)),
                sessionToken: IdentityAccessValueCodec.token(IdentityAccessValueCodec.text(data.sessionToken)),
                expiresAt: IdentityAccessValueCodec.timestamp(data.expiresAt),
                redirectUri: returnedRedirectUri,
                assurance: IdentityAccessAuthenticationClient.decodeAssurance(data.assurance),
            };
        });
    }
    async changePassword(request, signal) {
        const normalized = IdentityAccessAuthenticationClient.normalizeCredential(request.credential);
        const currentPassword = IdentityAccessValueCodec.nonEmptySecret(request.currentPassword);
        const newPassword = IdentityAccessValueCodec.nonEmptySecret(request.newPassword);
        await this.#transport.perform(`api/v1/authentication/clients/${encodeURIComponent(normalized.clientId)}/credentials/password/change`, {
            method: "POST",
            acceptedStatuses: [204],
            headers: IdentityAccessAuthenticationClient.sessionHeaders(normalized, true),
            body: JSON.stringify({ currentPassword, newPassword }),
            ...(signal === undefined ? {} : { signal }),
        }, async () => undefined);
    }
    async recoverPasswordWithCode(request, signal) {
        const clientId = IdentityAccessValueCodec.clientId(request.clientId);
        const loginIdentifier = IdentityAccessValueCodec.nonEmpty(request.loginIdentifier);
        const recoveryCode = IdentityAccessValueCodec.nonEmptySecret(request.recoveryCode);
        const newPassword = IdentityAccessValueCodec.nonEmptySecret(request.newPassword);
        await this.#transport.perform(`api/v1/authentication/clients/${encodeURIComponent(clientId)}/recovery/password`, {
            method: "POST",
            acceptedStatuses: [204],
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ loginIdentifier, recoveryCode, newPassword }),
            ...(signal === undefined ? {} : { signal }),
        }, async () => undefined);
    }
    async validateSession(credential, signal) {
        const clientId = IdentityAccessValueCodec.clientId(credential.clientId);
        const sessionId = IdentityAccessValueCodec.uuid(credential.sessionId);
        const sessionToken = IdentityAccessValueCodec.token(credential.sessionToken);
        return this.#transport.requestJson(`api/v1/authentication/clients/${encodeURIComponent(clientId)}/sessions/validate`, {
            method: "POST",
            acceptedStatuses: [200],
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ sessionId, sessionToken }),
            ...(signal === undefined ? {} : { signal }),
        }, (value) => {
            const data = IdentityAccessValueCodec.object(value);
            const returnedSessionId = IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.sessionId));
            if (returnedSessionId !== sessionId) {
                throw new IdentityAccessClientError("protocol");
            }
            return {
                userId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.userId)),
                sessionId: returnedSessionId,
                expiresAt: IdentityAccessValueCodec.timestamp(data.expiresAt),
                assurance: IdentityAccessAuthenticationClient.decodeAssurance(data.assurance),
            };
        });
    }
    async verifyTotp(credential, authenticatorId, code, signal) {
        return this.verifyFactor(credential, `totp/${encodeURIComponent(IdentityAccessValueCodec.uuid(authenticatorId))}/verify`, { code: IdentityAccessValueCodec.nonEmptySecret(code) }, signal);
    }
    async verifyRecoveryCode(credential, authenticatorId, code, signal) {
        return this.verifyFactor(credential, `recovery/${encodeURIComponent(IdentityAccessValueCodec.uuid(authenticatorId))}/verify`, { code: IdentityAccessValueCodec.nonEmptySecret(code) }, signal);
    }
    async beginWebAuthnStepUp(credential, signal) {
        const normalized = IdentityAccessAuthenticationClient.normalizeCredential(credential);
        return this.#transport.requestJson(`api/v1/authentication/clients/${encodeURIComponent(normalized.clientId)}/mfa/webauthn/options`, {
            method: "POST",
            acceptedStatuses: [200],
            headers: IdentityAccessAuthenticationClient.sessionHeaders(normalized, false),
            ...(signal === undefined ? {} : { signal }),
        }, (value) => {
            const data = IdentityAccessValueCodec.object(value);
            const userVerification = IdentityAccessValueCodec.text(data.userVerification);
            if (userVerification !== "required")
                throw new IdentityAccessClientError("protocol");
            return {
                challengeId: IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(data.challengeId)),
                challenge: IdentityAccessValueCodec.token(IdentityAccessValueCodec.text(data.challenge)),
                relyingPartyId: IdentityAccessValueCodec.nonEmpty(IdentityAccessValueCodec.text(data.relyingPartyId)),
                timeoutMilliseconds: IdentityAccessValueCodec.positiveInteger(data.timeoutMilliseconds),
                allowCredentialIds: IdentityAccessValueCodec.array(data.allowCredentialIds, (entry) => IdentityAccessValueCodec.token(IdentityAccessValueCodec.text(entry))),
                userVerification,
            };
        });
    }
    async completeWebAuthnStepUp(credential, response, signal) {
        const normalized = IdentityAccessAuthenticationClient.normalizeCredential(credential);
        const challengeId = IdentityAccessValueCodec.uuid(response.challengeId);
        const credentialId = IdentityAccessValueCodec.token(response.credentialId);
        const clientDataJson = IdentityAccessValueCodec.token(response.clientDataJson);
        const authenticatorData = IdentityAccessValueCodec.token(response.authenticatorData);
        const signature = IdentityAccessValueCodec.token(response.signature);
        const userHandle = response.userHandle === undefined
            ? undefined
            : IdentityAccessValueCodec.token(response.userHandle);
        return this.#transport.requestJson(`api/v1/authentication/clients/${encodeURIComponent(normalized.clientId)}/mfa/webauthn/complete`, {
            method: "POST",
            acceptedStatuses: [200],
            headers: IdentityAccessAuthenticationClient.sessionHeaders(normalized, true),
            body: JSON.stringify({
                challengeId,
                credentialId,
                clientDataJson,
                authenticatorData,
                signature,
                ...(userHandle === undefined ? {} : { userHandle }),
            }),
            ...(signal === undefined ? {} : { signal }),
        }, (value) => IdentityAccessAuthenticationClient.decodeAssurance(value));
    }
    async logout(credential, postLogoutRedirectUri, signal) {
        const clientId = IdentityAccessValueCodec.clientId(credential.clientId);
        const sessionId = IdentityAccessValueCodec.uuid(credential.sessionId);
        const sessionToken = IdentityAccessValueCodec.token(credential.sessionToken);
        const redirect = postLogoutRedirectUri === undefined
            ? undefined
            : IdentityAccessValueCodec.redirectUri(postLogoutRedirectUri);
        return this.#transport.requestJson(`api/v1/authentication/clients/${encodeURIComponent(clientId)}/logout`, {
            method: "POST",
            acceptedStatuses: [200],
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                sessionId,
                sessionToken,
                ...(redirect === undefined ? {} : { postLogoutRedirectUri: redirect }),
            }),
            ...(signal === undefined ? {} : { signal }),
        }, (value) => {
            const data = IdentityAccessValueCodec.object(value);
            if (data.postLogoutRedirectUri === null || data.postLogoutRedirectUri === undefined) {
                return {};
            }
            const returned = IdentityAccessValueCodec.redirectUri(IdentityAccessValueCodec.text(data.postLogoutRedirectUri));
            if (redirect === undefined || returned !== redirect) {
                throw new IdentityAccessClientError("protocol");
            }
            return { postLogoutRedirectUri: returned };
        });
    }
    async verifyFactor(credential, relativePath, payload, signal) {
        const normalized = IdentityAccessAuthenticationClient.normalizeCredential(credential);
        return this.#transport.requestJson(`api/v1/authentication/clients/${encodeURIComponent(normalized.clientId)}/mfa/${relativePath}`, {
            method: "POST",
            acceptedStatuses: [200],
            headers: IdentityAccessAuthenticationClient.sessionHeaders(normalized, true),
            body: JSON.stringify(payload),
            ...(signal === undefined ? {} : { signal }),
        }, (value) => IdentityAccessAuthenticationClient.decodeAssurance(value));
    }
    static normalizeCredential(credential) {
        return {
            kind: "session",
            clientId: IdentityAccessValueCodec.clientId(credential.clientId),
            sessionId: IdentityAccessValueCodec.uuid(credential.sessionId),
            sessionToken: IdentityAccessValueCodec.token(credential.sessionToken),
        };
    }
    static sessionHeaders(credential, json) {
        return {
            ...(json ? { "Content-Type": "application/json" } : {}),
            Authorization: `IdentitySession ${credential.sessionToken}`,
            "X-Identity-Access-Session": credential.sessionId,
        };
    }
    static decodeAssurance(value) {
        const data = IdentityAccessValueCodec.object(value);
        const level = IdentityAccessValueCodec.text(data.level);
        if (level !== "password" && level !== "mfa")
            throw new IdentityAccessClientError("protocol");
        const methods = IdentityAccessValueCodec.array(data.methods, (entry) => IdentityAccessValueCodec.text(entry));
        if (!methods.includes("pwd") || (level === "mfa" && (!methods.includes("mfa") || methods.length < 3))) {
            throw new IdentityAccessClientError("protocol");
        }
        if (level === "password" && (methods.length !== 1 || methods[0] !== "pwd")) {
            throw new IdentityAccessClientError("protocol");
        }
        const acr = IdentityAccessValueCodec.text(data.acr);
        const expectedAcr = level === "mfa"
            ? "urn:generic-identity-access:acr:mfa"
            : "urn:generic-identity-access:acr:password";
        if (acr !== expectedAcr)
            throw new IdentityAccessClientError("protocol");
        return {
            level,
            methods,
            verifiedAt: IdentityAccessValueCodec.timestamp(data.verifiedAt),
            acr,
        };
    }
}
