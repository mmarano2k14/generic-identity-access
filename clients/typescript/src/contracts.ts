/** Public JSON contracts exposed by the diagnostic client. */
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
