import "server-only";
import { IdentityAccessAdminRequest } from "./IdentityAccessAdminRequest";
import { IdentityAccessAdminSecurityManifestParser } from "./IdentityAccessAdminSecurityManifestParser";

/** Server-only registration of immutable application-owned security manifests and scope-type declarations. */
export class IdentityAccessAdminSecurityModelMutationService {
  readonly #request: IdentityAccessAdminRequest;
  readonly #parser: IdentityAccessAdminSecurityManifestParser;

  public constructor(
    request: IdentityAccessAdminRequest,
    parser = new IdentityAccessAdminSecurityManifestParser(),
  ) {
    this.#request = request;
    this.#parser = parser;
  }

  public async registerManifest(formData: FormData): Promise<string> {
    const manifest = await this.#parser.parse(formData);
    const context = this.#request.administrationContext;
    const configuredApplicationKey = context.applicationKey.trim().toLowerCase();
    if (manifest.applicationKey !== configuredApplicationKey) {
      throw new Error(
        `Invalid administration input: manifest applicationKey must match the configured application context (${configuredApplicationKey}).`,
      );
    }

    const registered = await this.#request.client.administration.securityModels.registerManifest(
      context,
      manifest,
    );

    return `Security model v${registered.modelVersion} registered with ${registered.capabilityCount} capabilities.`;
  }

  public async addScopeType(modelVersion: number, formData: FormData): Promise<string> {
    if (!Number.isSafeInteger(modelVersion) || modelVersion <= 0) {
      throw new Error("Invalid administration input: modelVersion must be a positive integer.");
    }
    const key = IdentityAccessAdminSecurityModelMutationService.scopeTypeKey(formData, "key");
    const displayName = IdentityAccessAdminSecurityModelMutationService.requiredText(formData, "displayName", 200);
    const parentKey = IdentityAccessAdminSecurityModelMutationService.optionalScopeTypeKey(formData, "parentKey");

    if (parentKey === key) {
      throw new Error("Invalid administration input: a scope type cannot be its own parent.");
    }

    const created = await this.#request.client.administration.securityModels.addScopeType(
      this.#request.administrationContext,
      modelVersion,
      {
        key,
        displayName,
        ...(parentKey === undefined ? {} : { parentKey }),
        canAttachToTenant: parentKey === undefined,
      },
    );

    return `Scope type ${created.displayName} (${created.key}) registered for security model v${modelVersion}.`;
  }

  private static requiredText(formData: FormData, name: string, maxLength: number): string {
    const value = formData.get(name);
    if (typeof value !== "string") throw new Error(`Invalid administration input: ${name} is required.`);
    const normalized = value.trim();
    if (normalized.length === 0) throw new Error(`Invalid administration input: ${name} is required.`);
    if (normalized.length > maxLength) throw new Error(`Invalid administration input: ${name} is too long.`);
    return normalized;
  }

  private static scopeTypeKey(formData: FormData, name: string): string {
    const value = IdentityAccessAdminSecurityModelMutationService.requiredText(formData, name, 64).toLowerCase();
    if (!/^[a-z][a-z0-9-]{0,63}$/.test(value)) {
      throw new Error(`Invalid administration input: ${name} must use lower-case letters, digits, and hyphens and must start with a letter.`);
    }
    return value;
  }

  private static optionalScopeTypeKey(formData: FormData, name: string): string | undefined {
    const value = formData.get(name);
    if (value === null || value === undefined || value === "") return undefined;
    if (typeof value !== "string") throw new Error(`Invalid administration input: ${name} is invalid.`);
    const normalized = value.trim().toLowerCase();
    if (normalized.length === 0) return undefined;
    if (normalized.length > 64 || !/^[a-z][a-z0-9-]{0,63}$/.test(normalized)) {
      throw new Error(`Invalid administration input: ${name} must be a registered scope-type key.`);
    }
    return normalized;
  }
}
