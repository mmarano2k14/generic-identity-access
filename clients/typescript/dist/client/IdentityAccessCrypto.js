import { IdentityAccessClientError } from "../errors.js";
import { IdentityAccessValueCodec } from "./IdentityAccessValueCodec.js";
/** Owns only cryptographic random material and PKCE S256 computation. */
export class IdentityAccessCrypto {
    static async computeS256Challenge(codeVerifier) {
        const verifier = IdentityAccessValueCodec.pkceVerifier(codeVerifier);
        const crypto = globalThis.crypto;
        if (crypto === undefined || crypto.subtle === undefined) {
            throw new IdentityAccessClientError("configuration");
        }
        const digest = await crypto.subtle.digest("SHA-256", new TextEncoder().encode(verifier));
        return IdentityAccessCrypto.base64Url(new Uint8Array(digest));
    }
    static randomOpaque(byteLength) {
        if (!Number.isSafeInteger(byteLength) || byteLength < 16 || byteLength > 64) {
            throw new IdentityAccessClientError("configuration");
        }
        const crypto = globalThis.crypto;
        if (crypto === undefined || typeof crypto.getRandomValues !== "function") {
            throw new IdentityAccessClientError("configuration");
        }
        const bytes = new Uint8Array(byteLength);
        crypto.getRandomValues(bytes);
        return IdentityAccessCrypto.base64Url(bytes);
    }
    static base64Url(bytes) {
        const alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
        let output = "";
        for (let index = 0; index < bytes.length; index += 3) {
            const first = bytes[index] ?? 0;
            const secondPresent = index + 1 < bytes.length;
            const thirdPresent = index + 2 < bytes.length;
            const second = bytes[index + 1] ?? 0;
            const third = bytes[index + 2] ?? 0;
            const value = (first << 16) | (second << 8) | third;
            output += alphabet[(value >> 18) & 63] ?? "";
            output += alphabet[(value >> 12) & 63] ?? "";
            output += secondPresent ? (alphabet[(value >> 6) & 63] ?? "") : "=";
            output += thirdPresent ? (alphabet[value & 63] ?? "") : "=";
        }
        return output.replace(/=+$/u, "").replace(/\+/gu, "-").replace(/\//gu, "_");
    }
}
