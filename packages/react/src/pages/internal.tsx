import type { ReactNode } from "react";
import { IdentityStatus } from "../components/IdentityStatus";

export function activeStatus(status: number): ReactNode {
  return status === 1 ? (
    <IdentityStatus label="Active" tone="positive" />
  ) : (
    <IdentityStatus label="Inactive" tone="negative" />
  );
}

export function mfaModeLabel(mode: number | undefined): string {
  if (mode === 1) return "Disabled";
  if (mode === 2) return "Optional";
  if (mode === 3) return "Required";
  return "Not configured";
}

export function authenticatorStatus(status: number): ReactNode {
  if (status === 1) return <IdentityStatus label="Pending" tone="warning" />;
  if (status === 2) return <IdentityStatus label="Active" tone="positive" />;
  return <IdentityStatus label="Revoked" tone="negative" />;
}
