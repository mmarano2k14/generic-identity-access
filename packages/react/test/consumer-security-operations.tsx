import type { ReactElement } from "react";
import {
  AuthenticatorRevocationForm,
  MfaPage,
  MfaPolicyForm,
  SecurityAuditPage,
  SessionRevocationForm,
} from "@generic-identity/react/security-operations";
import type {
  IdentityMfaPolicyRecord,
  IdentityMfaProviderRecord,
  IdentitySecurityAuditRecord,
} from "@generic-identity/contracts/security-operations";

export function renderSecurityOperations(
  providers: readonly IdentityMfaProviderRecord[],
  policy: IdentityMfaPolicyRecord | null,
  events: readonly IdentitySecurityAuditRecord[],
): readonly ReactElement[] {
  return [
    <MfaPage providers={providers} policy={policy} />,
    policy === null
      ? <MfaPolicyForm providers={providers} />
      : <MfaPolicyForm providers={providers} mode={policy.mode} allowedProviders={policy.allowedProviders} expectedVersion={policy.version} />,
    <AuthenticatorRevocationForm userId="00000000-0000-0000-0000-000000000001" authenticatorId="00000000-0000-0000-0000-000000000002" expectedVersion={1} />,
    <SessionRevocationForm target="user" />,
    <SessionRevocationForm target="client" />,
    <SecurityAuditPage events={events} />,
  ];
}
