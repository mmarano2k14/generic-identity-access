import "server-only";
import { IdentityAccessClientError, type IdentityPasswordCredentialMetadataRecord } from "@identity-access/client";
import type { AdminActionFailure } from "../contracts/AdminActionState";
import { IdentityAccessAdminFailurePresentation } from "./IdentityAccessAdminFailurePresentation";
import { IdentityAccessAdminRequest } from "./IdentityAccessAdminRequest";

export interface IdentityAccessAdminCredentialReadResult {
  readonly credential: IdentityPasswordCredentialMetadataRecord | null;
  readonly failure?: AdminActionFailure;
}

/** Loads secret-free password-credential metadata for one selected identity. */
export class IdentityAccessAdminCredentialReadService {
  readonly #request: IdentityAccessAdminRequest;

  public constructor(request: IdentityAccessAdminRequest) {
    this.#request = request;
  }

  public async load(userId: string): Promise<IdentityAccessAdminCredentialReadResult> {
    try {
      const credential = await this.#request.client.administration.credentials.get(
        this.#request.administrationContext,
        userId,
      );
      return { credential };
    } catch (error) {
      if (error instanceof IdentityAccessClientError &&
        (error.code === "unauthenticated" || error.code === "cancelled")) {
        throw error;
      }

      return {
        credential: null,
        failure: IdentityAccessAdminFailurePresentation.fromRead(error),
      };
    }
  }
}
