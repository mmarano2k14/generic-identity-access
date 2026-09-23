import "server-only";
import { IdentityAccessClient } from "@identity-access/client";

/** Server-only holder for the class-based Identity Access client used by the runnable administration host. */
export class IdentityAccessServerConnector {
  readonly #client: IdentityAccessClient;

  public constructor() {
    const baseUrl = process.env.IDENTITY_ACCESS_API_BASE_URL;
    if (!baseUrl) throw new Error("IDENTITY_ACCESS_API_BASE_URL is required.");
    this.#client = new IdentityAccessClient({ baseUrl });
  }

  public get client(): IdentityAccessClient {
    return this.#client;
  }
}
