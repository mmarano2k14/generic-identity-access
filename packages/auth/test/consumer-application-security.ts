import type { GenericIdentityApplicationSecurityClient } from "@generic-identity/auth/application-security";
import { createApplicationSecurityPermissionReference } from "@generic-identity/auth/application-security";
import type { IdentityApplicationSecurityModelRecord } from "@generic-identity/contracts/application-security";

export function acceptApplicationSecurity(
  client: GenericIdentityApplicationSecurityClient,
  model: IdentityApplicationSecurityModelRecord,
): void {
  void client.models;
  void client.manifests;
  void client.scopeTypes;
  void client.capabilities;
  void client.context;
  void createApplicationSecurityPermissionReference(
    model,
    model.rbacNamespaces[0] ?? "default",
    model.capabilities[0] ?? { resource: "resource", feature: "feature", action: "read", displayName: "Read" },
  );
}
