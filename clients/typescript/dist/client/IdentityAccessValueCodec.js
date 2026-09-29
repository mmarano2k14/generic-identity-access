import { IdentityAccessClientError } from "../errors.js";
/** Central scalar validation/normalization used by specialized client classes. */
export class IdentityAccessValueCodec {
    static object(value) {
        if (typeof value !== "object" || value === null || Array.isArray(value)) {
            throw new IdentityAccessClientError("protocol");
        }
        return value;
    }
    static text(value) {
        if (typeof value !== "string" || value.trim().length === 0) {
            throw new IdentityAccessClientError("protocol");
        }
        return value;
    }
    static flag(value) {
        if (typeof value !== "boolean") {
            throw new IdentityAccessClientError("protocol");
        }
        return value;
    }
    static positiveInteger(value) {
        if (typeof value !== "number" || !Number.isSafeInteger(value) || value < 1) {
            throw new IdentityAccessClientError("protocol");
        }
        return value;
    }
    static timestamp(value) {
        const text = IdentityAccessValueCodec.text(value);
        if (!Number.isFinite(Date.parse(text))) {
            throw new IdentityAccessClientError("protocol");
        }
        return text;
    }
    static nonEmpty(value) {
        if (typeof value !== "string" || value.length === 0 || value !== value.trim()) {
            throw new IdentityAccessClientError("configuration");
        }
        return value;
    }
    static nonEmptySecret(value) {
        if (typeof value !== "string" || value.length === 0) {
            throw new IdentityAccessClientError("configuration");
        }
        return value;
    }
    static token(value) {
        const token = IdentityAccessValueCodec.nonEmpty(value);
        if (/\s/u.test(token)) {
            throw new IdentityAccessClientError("configuration");
        }
        return token;
    }
    static opaqueToken43(value) {
        const token = IdentityAccessValueCodec.token(value);
        if (!/^[A-Za-z0-9_-]{43}$/u.test(token)) {
            throw new IdentityAccessClientError("protocol");
        }
        return token;
    }
    static uuid(value) {
        const normalized = IdentityAccessValueCodec.nonEmpty(value).toLowerCase();
        if (!/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/u.test(normalized)) {
            throw new IdentityAccessClientError("configuration");
        }
        return normalized;
    }
    static clientId(value) {
        const normalized = IdentityAccessValueCodec.nonEmpty(value);
        if (!/^[a-z][a-z0-9_-]{0,127}$/u.test(normalized)) {
            throw new IdentityAccessClientError("configuration");
        }
        return normalized;
    }
    static capabilityPatternSegment(value) {
        const normalized = IdentityAccessValueCodec.nonEmpty(value).toLowerCase();
        if (normalized === "*")
            return normalized;
        if (!/^[a-z][a-z0-9-]{0,63}$/u.test(normalized)) {
            throw new IdentityAccessClientError("configuration");
        }
        return normalized;
    }
    static managedPolicyKey(value) {
        const normalized = IdentityAccessValueCodec.nonEmpty(value).toLowerCase();
        if (!/^[a-z][a-z0-9-]{0,127}$/u.test(normalized)) {
            throw new IdentityAccessClientError("configuration");
        }
        return normalized;
    }
    static slug(value) {
        const normalized = IdentityAccessValueCodec.nonEmpty(value).toLowerCase();
        if (!/^[a-z][a-z0-9-]{0,63}$/u.test(normalized)) {
            throw new IdentityAccessClientError("configuration");
        }
        return normalized;
    }
    static rbacContextSegment(value) {
        const normalized = IdentityAccessValueCodec.nonEmpty(value.trim()).toLowerCase();
        if (normalized.length > 128 || normalized.includes(":") || normalized.includes("*")) {
            throw new IdentityAccessClientError("configuration");
        }
        return normalized;
    }
    static sha256(value) {
        const normalized = IdentityAccessValueCodec.text(value);
        if (!/^[0-9a-f]{64}$/u.test(normalized)) {
            throw new IdentityAccessClientError("protocol");
        }
        return normalized;
    }
    static redirectUri(value) {
        const uri = IdentityAccessValueCodec.nonEmpty(value);
        if (uri.includes("*")) {
            throw new IdentityAccessClientError("configuration");
        }
        let parsed;
        try {
            parsed = new URL(uri);
        }
        catch {
            throw new IdentityAccessClientError("configuration");
        }
        const loopback = ["localhost", "127.0.0.1", "[::1]"].includes(parsed.hostname);
        if (parsed.username ||
            parsed.password ||
            parsed.hash ||
            !(parsed.protocol === "https:" || (parsed.protocol === "http:" && loopback))) {
            throw new IdentityAccessClientError("configuration");
        }
        return uri;
    }
    static opaqueState(value) {
        const state = IdentityAccessValueCodec.nonEmpty(value);
        if (state.length < 8 || state.length > 512 || /[\u0000-\u001f\u007f]/u.test(state)) {
            throw new IdentityAccessClientError("configuration");
        }
        return state;
    }
    static nonce(value) {
        const nonce = IdentityAccessValueCodec.nonEmpty(value);
        if (nonce.length < 8 || nonce.length > 256 || /[\u0000-\u001f\u007f]/u.test(nonce)) {
            throw new IdentityAccessClientError("configuration");
        }
        return nonce;
    }
    static pkceVerifier(value) {
        const verifier = IdentityAccessValueCodec.nonEmpty(value);
        if (!/^[A-Za-z0-9\-._~]{43,128}$/u.test(verifier)) {
            throw new IdentityAccessClientError("configuration");
        }
        return verifier;
    }
    static optionalUuidOrEmpty(value) {
        return value === undefined ? "00000000-0000-0000-0000-000000000000" : IdentityAccessValueCodec.uuid(value);
    }
    static lifecycleStatus(value) {
        if (value !== 1 && value !== 2)
            throw new IdentityAccessClientError("configuration");
        return value;
    }
    static version(value) {
        if (typeof value !== "number" || !Number.isSafeInteger(value) || value < 1) {
            throw new IdentityAccessClientError("configuration");
        }
        return value;
    }
    static boolean(value) {
        if (typeof value !== "boolean")
            throw new IdentityAccessClientError("configuration");
        return value;
    }
    static nullableUuid(value) {
        if (value === null || value === undefined)
            return undefined;
        return IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(value));
    }
    static array(value, decode) {
        if (!Array.isArray(value))
            throw new IdentityAccessClientError("protocol");
        return value.map(decode);
    }
}
