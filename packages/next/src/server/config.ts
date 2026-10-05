import type { GenericIdentityClientOptions } from "@generic-identity/auth";

export interface NextIdentityServerOptions {
  readonly baseUrl: string;
  readonly clientId: string;
  readonly cookiePrefix?: string;
  readonly cookiePath?: string;
  readonly secureCookies?: boolean;
  readonly timeoutMs?: number;
}

export interface NormalizedNextIdentityServerOptions {
  readonly clientOptions: GenericIdentityClientOptions;
  readonly clientId: string;
  readonly cookiePrefix: string;
  readonly cookiePath: string;
  readonly secureCookies: boolean;
}

export function normalizeNextIdentityServerOptions(
  options: NextIdentityServerOptions,
): NormalizedNextIdentityServerOptions {
  const clientId = required(options.clientId, "clientId", 256);
  const cookiePrefix = options.cookiePrefix ?? "gi_identity";
  if (!/^[!#$%&'*+\-.^_`|~0-9A-Za-z]+$/.test(cookiePrefix)) {
    throw new Error("Invalid Generic Identity cookiePrefix.");
  }

  const cookiePath = options.cookiePath ?? "/";
  if (!cookiePath.startsWith("/") || cookiePath.includes(";") || cookiePath.includes("\\")) {
    throw new Error("Invalid Generic Identity cookiePath.");
  }

  const clientOptions: GenericIdentityClientOptions = {
    baseUrl: required(options.baseUrl, "baseUrl", 2048),
    ...(options.timeoutMs === undefined ? {} : { timeoutMs: options.timeoutMs }),
  };

  return {
    clientOptions,
    clientId,
    cookiePrefix,
    cookiePath,
    secureCookies: options.secureCookies ?? process.env.NODE_ENV === "production",
  };
}

function required(value: string, name: string, maxLength: number): string {
  const normalized = value.trim();
  if (!normalized || normalized.length > maxLength) {
    throw new Error(`Generic Identity server option '${name}' is required.`);
  }
  return normalized;
}
