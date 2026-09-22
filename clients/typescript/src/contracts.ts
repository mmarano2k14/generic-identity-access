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
  | "sessions"
  | "scope-authority";

export interface IdentityAccessAdminUiEntry {
  readonly section: IdentityAccessAdminUiSection;
  readonly requirement: IdentityCapabilityRequirement;
}

export interface IdentityAccessAdminUiDefinition {
  readonly entries: readonly IdentityAccessAdminUiEntry[];
}
