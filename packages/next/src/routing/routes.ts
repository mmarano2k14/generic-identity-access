export interface IdentityRouteMap {
  readonly signIn: string;
  readonly recovery: string;
  readonly account: string;
  readonly users: string;
  readonly groups: string;
  readonly policies: string;
  readonly sessions: string;
  readonly mfa: string;
  readonly security: string;
}

export type IdentityRouteKey = keyof IdentityRouteMap;

/**
 * Validates consumer-owned application-local routes without imposing a public
 * URL scheme on the consuming application. Routes remain owned by that consumer.
 */
export function defineIdentityRoutes(routes: IdentityRouteMap): Readonly<IdentityRouteMap> {
  const normalized: IdentityRouteMap = {
    signIn: localPath(routes.signIn, "signIn"),
    recovery: localPath(routes.recovery, "recovery"),
    account: localPath(routes.account, "account"),
    users: localPath(routes.users, "users"),
    groups: localPath(routes.groups, "groups"),
    policies: localPath(routes.policies, "policies"),
    sessions: localPath(routes.sessions, "sessions"),
    mfa: localPath(routes.mfa, "mfa"),
    security: localPath(routes.security, "security"),
  };

  return Object.freeze(normalized);
}

export function identityRoute(routes: IdentityRouteMap, key: IdentityRouteKey): string {
  return routes[key];
}

export function isIdentityRoute(pathname: string, routes: IdentityRouteMap): boolean {
  return Object.values(routes).some((route) => pathname === route || pathname.startsWith(`${route}/`));
}

function localPath(value: string, key: IdentityRouteKey): string {
  if (
    typeof value !== "string" ||
    value !== value.trim() ||
    !value.startsWith("/") ||
    value.startsWith("//") ||
    value.includes("?") ||
    value.includes("#") ||
    value.includes("\\")
  ) {
    throw new Error(`Invalid application-local Identity route: ${key}.`);
  }

  return value.length > 1 && value.endsWith("/") ? value.slice(0, -1) : value;
}
