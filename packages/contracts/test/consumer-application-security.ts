import type {
  IdentityApplicationSecurityModelRecord,
  IdentityApplicationSecurityPermissionReference,
  IdentityScopeTypeRecord,
} from "@generic-identity/contracts/application-security";

export function acceptApplicationSecurityContracts(
  model: IdentityApplicationSecurityModelRecord,
  scopeType: IdentityScopeTypeRecord,
  permission: IdentityApplicationSecurityPermissionReference,
): void {
  void model;
  void scopeType;
  void permission;
}
