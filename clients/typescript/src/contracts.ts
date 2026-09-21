/** Public JSON contracts. IDs are UUID strings; none of these values proves authorization. */
export interface UserProfileResponse {
  readonly identityScopeId: string;
  readonly userId: string;
  readonly displayName: string;
  readonly status: "active" | "suspended";
}

export interface TenantProfileResponse {
  readonly identityScopeId: string;
  readonly tenantId: string;
  readonly displayName: string;
  readonly status: "active" | "suspended";
}

export interface GroupProfileResponse {
  readonly identityScopeId: string;
  readonly tenantId: string;
  readonly applicationKey: string;
  readonly groupId: string;
  readonly displayName: string;
  readonly status: "active" | "suspended";
}

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
  readonly storageConfigured: boolean;
  readonly authenticationConfigured: boolean;
  readonly authorizationConfigured: boolean;
}
