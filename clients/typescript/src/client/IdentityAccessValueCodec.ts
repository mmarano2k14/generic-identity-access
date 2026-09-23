import { IdentityAccessClientError } from "../errors.js";

export type IdentityJsonObject = Record<string, unknown>;

/** Central scalar validation/normalization used by specialized client classes. */
export class IdentityAccessValueCodec {
  public static object(value: unknown): IdentityJsonObject {
    if (typeof value !== "object" || value === null || Array.isArray(value)) {
      throw new IdentityAccessClientError("protocol");
    }
    return value as IdentityJsonObject;
  }

  public static text(value: unknown): string {
    if (typeof value !== "string" || value.trim().length === 0) {
      throw new IdentityAccessClientError("protocol");
    }
    return value;
  }

  public static flag(value: unknown): boolean {
    if (typeof value !== "boolean") {
      throw new IdentityAccessClientError("protocol");
    }
    return value;
  }

  public static positiveInteger(value: unknown): number {
    if (typeof value !== "number" || !Number.isSafeInteger(value) || value < 1) {
      throw new IdentityAccessClientError("protocol");
    }
    return value;
  }

  public static timestamp(value: unknown): string {
    const text = IdentityAccessValueCodec.text(value);
    if (!Number.isFinite(Date.parse(text))) {
      throw new IdentityAccessClientError("protocol");
    }
    return text;
  }

  public static nonEmpty(value: string): string {
    if (typeof value !== "string" || value.length === 0 || value !== value.trim()) {
      throw new IdentityAccessClientError("configuration");
    }
    return value;
  }

  public static nonEmptySecret(value: string): string {
    if (typeof value !== "string" || value.length === 0) {
      throw new IdentityAccessClientError("configuration");
    }
    return value;
  }

  public static token(value: string): string {
    const token = IdentityAccessValueCodec.nonEmpty(value);
    if (/\s/u.test(token)) {
      throw new IdentityAccessClientError("configuration");
    }
    return token;
  }

  public static opaqueToken43(value: string): string {
    const token = IdentityAccessValueCodec.token(value);
    if (!/^[A-Za-z0-9_-]{43}$/u.test(token)) {
      throw new IdentityAccessClientError("protocol");
    }
    return token;
  }

  public static uuid(value: string): string {
    const normalized = IdentityAccessValueCodec.nonEmpty(value).toLowerCase();
    if (!/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/u.test(normalized)) {
      throw new IdentityAccessClientError("configuration");
    }
    return normalized;
  }

  public static clientId(value: string): string {
    const normalized = IdentityAccessValueCodec.nonEmpty(value);
    if (!/^[a-z][a-z0-9_-]{0,127}$/u.test(normalized)) {
      throw new IdentityAccessClientError("configuration");
    }
    return normalized;
  }

  public static capabilityPatternSegment(value: string): string {
    const normalized = IdentityAccessValueCodec.nonEmpty(value).toLowerCase();
    if (normalized === "*") return normalized;
    if (!/^[a-z][a-z0-9-]{0,63}$/u.test(normalized)) {
      throw new IdentityAccessClientError("configuration");
    }
    return normalized;
  }

  public static slug(value: string): string {
    const normalized = IdentityAccessValueCodec.nonEmpty(value).toLowerCase();
    if (!/^[a-z][a-z0-9-]{0,63}$/u.test(normalized)) {
      throw new IdentityAccessClientError("configuration");
    }
    return normalized;
  }

  public static redirectUri(value: string): string {
    const uri = IdentityAccessValueCodec.nonEmpty(value);
    if (uri.includes("*")) {
      throw new IdentityAccessClientError("configuration");
    }

    let parsed: URL;
    try {
      parsed = new URL(uri);
    } catch {
      throw new IdentityAccessClientError("configuration");
    }

    const loopback = ["localhost", "127.0.0.1", "[::1]"].includes(parsed.hostname);
    if (
      parsed.username ||
      parsed.password ||
      parsed.hash ||
      !(parsed.protocol === "https:" || (parsed.protocol === "http:" && loopback))
    ) {
      throw new IdentityAccessClientError("configuration");
    }

    return uri;
  }

  public static opaqueState(value: string): string {
    const state = IdentityAccessValueCodec.nonEmpty(value);
    if (state.length < 8 || state.length > 512 || /[\u0000-\u001f\u007f]/u.test(state)) {
      throw new IdentityAccessClientError("configuration");
    }
    return state;
  }

  public static nonce(value: string): string {
    const nonce = IdentityAccessValueCodec.nonEmpty(value);
    if (nonce.length < 8 || nonce.length > 256 || /[\u0000-\u001f\u007f]/u.test(nonce)) {
      throw new IdentityAccessClientError("configuration");
    }
    return nonce;
  }

  public static pkceVerifier(value: string): string {
    const verifier = IdentityAccessValueCodec.nonEmpty(value);
    if (!/^[A-Za-z0-9\-._~]{43,128}$/u.test(verifier)) {
      throw new IdentityAccessClientError("configuration");
    }
    return verifier;
  }

  public static optionalUuidOrEmpty(value: string | undefined): string {
    return value === undefined ? "00000000-0000-0000-0000-000000000000" : IdentityAccessValueCodec.uuid(value);
  }

  public static lifecycleStatus(value: unknown): 1 | 2 {
    if (value !== 1 && value !== 2) throw new IdentityAccessClientError("configuration");
    return value;
  }

  public static version(value: unknown): number {
    if (typeof value !== "number" || !Number.isSafeInteger(value) || value < 1) {
      throw new IdentityAccessClientError("configuration");
    }
    return value;
  }

  public static boolean(value: unknown): boolean {
    if (typeof value !== "boolean") throw new IdentityAccessClientError("configuration");
    return value;
  }

  public static nullableUuid(value: unknown): string | undefined {
    if (value === null || value === undefined) return undefined;
    return IdentityAccessValueCodec.uuid(IdentityAccessValueCodec.text(value));
  }

  public static array<T>(value: unknown, decode: (entry: unknown) => T): readonly T[] {
    if (!Array.isArray(value)) throw new IdentityAccessClientError("protocol");
    return value.map(decode);
  }
}
