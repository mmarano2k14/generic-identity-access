import "server-only";
import { cookies } from "next/headers";
import {
  IdentityAccessAdminUiBuilder,
  IdentityAuthorizationContext,
  type IdentityAdministrationContext,
  type IdentityBearerCredential,
  type IdentityTenantAdministrationContext,
} from "@identity-access/client";
import { IdentityAccessServerConnector } from "../../identity-access";

/**
 * Per-request server-only adapter for the reusable administration pages.
 * It owns no global current-user state and never exposes the bearer credential to Client Components.
 */
export class IdentityAccessAdminRequest {
  readonly #connector: IdentityAccessServerConnector;
  readonly #identityScopeId: string;
  readonly #applicationKey: string;
  readonly #tenantId: string | undefined;
  readonly #credential: IdentityBearerCredential;

  private constructor(
    connector: IdentityAccessServerConnector,
    identityScopeId: string,
    applicationKey: string,
    tenantId: string | undefined,
    credential: IdentityBearerCredential,
  ) {
    this.#connector = connector;
    this.#identityScopeId = identityScopeId;
    this.#applicationKey = applicationKey;
    this.#tenantId = tenantId;
    this.#credential = credential;
  }

  public static async fromCurrentRequest(): Promise<IdentityAccessAdminRequest> {
    const identityScopeId = IdentityAccessAdminRequest.requiredEnvironment("IDENTITY_ACCESS_IDENTITY_SCOPE_ID");
    const applicationKey = IdentityAccessAdminRequest.requiredEnvironment("IDENTITY_ACCESS_APPLICATION_KEY");
    const cookieName = IdentityAccessAdminRequest.requiredEnvironment("IDENTITY_ACCESS_BEARER_COOKIE_NAME");
    const tenantId = process.env.IDENTITY_ACCESS_TENANT_ID?.trim() || undefined;
    const cookieStore = await cookies();
    const accessToken = cookieStore.get(cookieName)?.value;
    if (!accessToken) throw new Error("An authenticated Identity Access bearer session is required.");

    return new IdentityAccessAdminRequest(
      new IdentityAccessServerConnector(),
      identityScopeId,
      applicationKey,
      tenantId,
      { kind: "bearer", accessToken },
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

  public tenantContext(): IdentityTenantAdministrationContext {
    if (!this.#tenantId) throw new Error("IDENTITY_ACCESS_TENANT_ID is required for this administration page.");
    return {
      ...this.administrationContext,
      tenantId: this.#tenantId,
    };
  }

  public scopeAuthorization(): IdentityAuthorizationContext {
    return new IdentityAuthorizationContext(this.client, this.administrationContext);
  }

  public tenantAuthorization(): IdentityAuthorizationContext | undefined {
    if (!this.#tenantId) return undefined;
    return new IdentityAuthorizationContext(this.client, this.tenantContext());
  }

  public adminUiBuilder(): IdentityAccessAdminUiBuilder {
    const builder = new IdentityAccessAdminUiBuilder(this.scopeAuthorization(), { basePath: "/identity" }).withAll();
    const tenantAuthorization = this.tenantAuthorization();
    if (tenantAuthorization !== undefined) builder.withTenantAuthorization(tenantAuthorization);
    return builder;
  }

  static requiredEnvironment(name: string): string {
    const value = process.env[name]?.trim();
    if (!value) throw new Error(`${name} is required.`);
    return value;
  }
}
