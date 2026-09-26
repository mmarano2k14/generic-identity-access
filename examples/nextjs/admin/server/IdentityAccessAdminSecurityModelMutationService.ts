import "server-only";
import { IdentityAccessAdminRequest } from "./IdentityAccessAdminRequest";
import { IdentityAccessAdminSecurityManifestParser } from "./IdentityAccessAdminSecurityManifestParser";

/** Server-only registration of immutable application-owned security manifests. */
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
}
