/** Public JSON and passive configuration contracts for the TypeScript client. */
export interface LivenessResponse {
  readonly status: "alive";
}

export interface ReadinessResponse {
  readonly ready: boolean;
  readonly stage: string;
  readonly blockingCapabilities: readonly string[];
}

export interface ServiceInfoResponse {
  readonly service: "identity-access";
  readonly apiVersion: "v1";
  readonly moduleVersion: string;
  readonly stage: string;
  readonly storageProvider: "postgresql";
  readonly databaseRoutingConfigured: boolean;
  readonly storageConfigured: boolean;
  readonly authenticationConfigured: boolean;
  readonly authorizationConfigured: boolean;
}

export interface IdentityCapabilityRequirement {
  readonly resource: string;
  readonly feature: string;
  readonly action: string;
}

export interface IdentityAuthorizationBoundary {
  readonly identityScopeId: string;
  readonly applicationKey: string;
  readonly tenantId?: string;
  readonly resourceScopeId?: string;
}

export interface IdentityBearerCredential {
  readonly kind: "bearer";
  readonly accessToken: string;
}

export interface IdentitySessionCredential {
  readonly kind: "session";
  readonly clientId: string;
  readonly sessionId: string;
  readonly sessionToken: string;
}

export type IdentityAccessCredential = IdentityBearerCredential | IdentitySessionCredential;

export interface IdentityPasswordLoginRequest {
  readonly clientId: string;
  readonly loginIdentifier: string;
  readonly password: string;
  readonly redirectUri: string;
}

export interface IdentityLocalSession extends IdentitySessionCredential {
  readonly userId: string;
  readonly expiresAt: string;
  readonly redirectUri: string;
}

export interface IdentitySessionValidationResult {
  readonly userId: string;
  readonly sessionId: string;
  readonly expiresAt: string;
}

export interface IdentityLogoutResult {
  readonly postLogoutRedirectUri?: string;
}

export interface IdentityOidcAuthorizationOptions {
  readonly clientId: string;
  readonly redirectUri: string;
  readonly state?: string;
  readonly nonce?: string;
}

export interface IdentityOidcAuthorizationCode {
  readonly clientId: string;
  readonly redirectUri: string;
  readonly code: string;
  readonly state: string;
  readonly nonce: string;
  readonly codeVerifier: string;
}

export interface IdentityOidcTokenSet {
  readonly accessToken: string;
  readonly tokenType: "Bearer";
  readonly expiresIn: number;
  readonly idToken?: string;
  readonly refreshToken: string;
  readonly scope: "openid";
}

export interface AuthorizationEvaluationResponse {
  readonly allowed: boolean;
}

export type IdentityAccessAdminUiSection =
  | "users"
  | "tenants"
  | "memberships"
  | "groups"
  | "policies"
  | "resource-scopes"
  | "mfa"
  | "sessions"
  | "scope-authority";

export interface IdentityAccessAdminUiEntry {
  readonly section: IdentityAccessAdminUiSection;
  readonly requirement: IdentityCapabilityRequirement;
  readonly href: string;
  readonly label: string;
  readonly description: string;
  readonly tenantScoped: boolean;
}

export interface IdentityAccessAdminUiDefinition {
  readonly basePath: string;
  readonly entries: readonly IdentityAccessAdminUiEntry[];
}

export interface IdentityAccessAdminUiBuilderOptions {
  readonly basePath?: string;
}
