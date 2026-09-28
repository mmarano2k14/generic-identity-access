import "server-only";
import {
  IdentityAccessAdminUiBuilder,
  IdentityAccessClientError,
  IdentityAuthorizationContext,
  type IdentityAdministrationContext,
  type IdentityBearerCredential,
  type IdentityEffectiveAdministrationContext,
  type IdentityTenantAdministrationContext,
} from "@identity-access/client";
import { IdentityAccessServerConnector } from "./IdentityAccessServerConnector";
import { IdentityAccessHostSessionService } from "./IdentityAccessHostSessionService";

/**
 * Per-request server-only adapter for the reusable administration pages.
 * Tenant visibility comes from the trusted API projection, never from browser input or a fixed env tenant.
 */
export class IdentityAccessAdminRequest {
  readonly #connector: IdentityAccessServerConnector;
  readonly #identityScopeId: string;
  readonly #applicationKey: string;
  readonly #credential: IdentityBearerCredential;
  readonly #effectiveContext: IdentityEffectiveAdministrationContext;

  private constructor(
    connector: IdentityAccessServerConnector,
    identityScopeId: string,
    applicationKey: string,
    credential: IdentityBearerCredential,
    effectiveContext: IdentityEffectiveAdministrationContext,
  ) {
    this.#connector = connector;
    this.#identityScopeId = identityScopeId;
    this.#applicationKey = applicationKey;
    this.#credential = credential;
    this.#effectiveContext = effectiveContext;
  }

  public static async fromCurrentRequest(): Promise<IdentityAccessAdminRequest> {
    const identityScopeId = IdentityAccessAdminRequest.requiredEnvironment("IDENTITY_ACCESS_IDENTITY_SCOPE_ID");
    const applicationKey = IdentityAccessAdminRequest.requiredEnvironment("IDENTITY_ACCESS_APPLICATION_KEY");
    const hostSession = await IdentityAccessHostSessionService.fromCurrentRequest();
    const credential = hostSession.requireBearerCredential();
    const administrationContext: IdentityAdministrationContext = { identityScopeId, applicationKey, credential };
    const effectiveContext = await hostSession.connector.client.administration.context.get(administrationContext);

    if (effectiveContext.identityScopeId !== identityScopeId || effectiveContext.applicationKey !== applicationKey) {
      throw new Error("Identity Access returned an administration context outside the configured host boundary.");
    }

    return new IdentityAccessAdminRequest(
      hostSession.connector,
      identityScopeId,
      applicationKey,
      credential,
      effectiveContext,
    );
  }

  public get client() {
    return this.#connector.client;
  }

  public get administrationContext(): IdentityAdministrationContext {
    return {
      identityScopeId: this.#identityScopeId,
      applicationKey: this.#applicationKey,
      credential: this.#credential,
    };
  }

  public get effectiveContext(): IdentityEffectiveAdministrationContext {
    return this.#effectiveContext;
  }

  public hasTenantContext(): boolean {
    return this.defaultTenantId() !== undefined;
  }

  public tenantContext(): IdentityTenantAdministrationContext {
    const context = this.selectedTenantContext();
    if (context === undefined) {
      throw new Error("A tenant must be selected from the server-authorized administration context for this page.");
    }
    return context;
  }

  public selectedTenantContext(requestedTenantId?: string): IdentityTenantAdministrationContext | undefined {
    const requested = requestedTenantId?.trim();
    if (requested) return this.tenantContextFor(requested);

    const tenantId = this.defaultTenantId();
    return tenantId === undefined ? undefined : this.tenantContextFor(tenantId);
  }

  public tenantContextFor(tenantId: string): IdentityTenantAdministrationContext {
    const value = tenantId.trim().toLowerCase();
    if (!value) throw new Error("Tenant ID is required.");

    if (this.#effectiveContext.tenantVisibility === "membership-limited" &&
        !this.#effectiveContext.activeTenantMemberships.some((membership) => membership.tenantId === value)) {
      throw new IdentityAccessClientError("forbidden", 403, "tenant_context_outside_visibility");
    }

    return {
      ...this.administrationContext,
      tenantId: value,
    };
  }

  public scopeAuthorization(): IdentityAuthorizationContext {
    return new IdentityAuthorizationContext(this.client, this.administrationContext);
  }

  public tenantAuthorization(): IdentityAuthorizationContext | undefined {
    const tenantId = this.defaultTenantId();
    if (tenantId === undefined) return undefined;
    return new IdentityAuthorizationContext(this.client, this.tenantContextFor(tenantId));
  }

  public tenantAuthorizationFor(tenantId: string): IdentityAuthorizationContext {
    return new IdentityAuthorizationContext(this.client, this.tenantContextFor(tenantId));
  }

  public adminUiBuilder(): IdentityAccessAdminUiBuilder {
    const builder = new IdentityAccessAdminUiBuilder(this.scopeAuthorization(), { basePath: "/identity" }).withAll();
    const tenantAuthorizations = this.#effectiveContext.activeTenantMemberships.map((membership) =>
      new IdentityAuthorizationContext(this.client, this.tenantContextFor(membership.tenantId)));
    if (tenantAuthorizations.length > 0) builder.withTenantAuthorizations(tenantAuthorizations);
    return builder;
  }

  private defaultTenantId(): string | undefined {
    if (this.#effectiveContext.activeTenantMemberships.length !== 1) return undefined;
    return this.#effectiveContext.activeTenantMemberships[0]?.tenantId;
  }

  static requiredEnvironment(name: string): string {
    const value = process.env[name]?.trim();
    if (!value) throw new Error(`${name} is required.`);
    return value;
  }
}
