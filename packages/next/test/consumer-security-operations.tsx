import type { ComponentType } from "react";
import type {
  AuthenticatorRevocationFormProps,
  MfaPageProps,
  MfaPolicyFormProps,
  SecurityAuditFilterFormProps,
  SecurityAuditPageProps,
  SessionSecurityFilterFormProps,
  SessionSecurityPageProps,
  SessionRevocationFormProps,
} from "@generic-identity/next/security-operations";
import {
  AuthenticatorRevocationForm,
  MfaPage,
  MfaPolicyForm,
  SecurityAuditFilterForm,
  SecurityAuditPage,
  SessionSecurityFilterForm,
  SessionSecurityPage,
  SessionRevocationForm,
} from "@generic-identity/next/security-operations";

export function acceptNextSecurityOperations(): {
  readonly audit: ComponentType<SecurityAuditPageProps>;
  readonly auditFilter: ComponentType<SecurityAuditFilterFormProps>;
  readonly sessionSecurity: ComponentType<SessionSecurityPageProps>;
  readonly sessionFilter: ComponentType<SessionSecurityFilterFormProps>;
  readonly sessionRevoke: ComponentType<SessionRevocationFormProps>;
  readonly mfa: ComponentType<MfaPageProps>;
  readonly policy: ComponentType<MfaPolicyFormProps>;
  readonly revoke: ComponentType<AuthenticatorRevocationFormProps>;
} {
  return {
    audit: SecurityAuditPage,
    auditFilter: SecurityAuditFilterForm,
    sessionSecurity: SessionSecurityPage,
    sessionFilter: SessionSecurityFilterForm,
    sessionRevoke: SessionRevocationForm,
    mfa: MfaPage,
    policy: MfaPolicyForm,
    revoke: AuthenticatorRevocationForm,
  };
}
