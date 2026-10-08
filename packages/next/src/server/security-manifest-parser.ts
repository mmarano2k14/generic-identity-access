import "server-only";

import type { IdentityApplicationSecurityManifestRequest } from "@generic-identity/contracts/application-security";
import { parseApplicationSecurityManifestFileCore } from "../internal/security-manifest-parser-core";

// The Next.js server adapter owns the upload limit and server-only boundary.
// Pure validation stays internal so it can be tested without a Next.js runtime.
const MAX_SECURITY_MANIFEST_BYTES = 262_144;

export async function parseNextApplicationSecurityManifestFile(
  formData: FormData,
): Promise<IdentityApplicationSecurityManifestRequest> {
  return parseApplicationSecurityManifestFileCore(formData, MAX_SECURITY_MANIFEST_BYTES);
}
