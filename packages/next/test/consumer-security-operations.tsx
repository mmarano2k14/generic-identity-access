import type { ComponentType } from "react";
import type { SecurityAuditPageProps } from "@generic-identity/next/security-operations";
import { SecurityAuditPage } from "@generic-identity/next/security-operations";

export function acceptNextSecurityOperations(): ComponentType<SecurityAuditPageProps> {
  return SecurityAuditPage;
}
