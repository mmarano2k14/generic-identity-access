import { IdentityButton, IdentityPanel } from "../components/index";

export interface AuthenticatorRevocationFormProps {
  readonly userId: string;
  readonly authenticatorId: string;
  readonly expectedVersion: number;
  readonly recovery?: boolean;
  readonly formAction?: string;
  readonly submitLabel?: string;
}

/** Presentation-only form for consumer-owned authenticator revocation server actions. */
export function AuthenticatorRevocationForm({
  userId,
  authenticatorId,
  expectedVersion,
  recovery = false,
  formAction,
  submitLabel = recovery ? "Revoke for recovery" : "Revoke authenticator",
}: AuthenticatorRevocationFormProps) {
  return (
    <IdentityPanel title={recovery ? "Recovery revocation" : "Authenticator revocation"}>
      <form className="gi-form" method="post" action={formAction} data-gi-component="authenticator-revocation-form">
        <input type="hidden" name="userId" value={userId} />
        <input type="hidden" name="authenticatorId" value={authenticatorId} />
        <input type="hidden" name="expectedVersion" value={String(expectedVersion)} />
        <input type="hidden" name="recovery" value={recovery ? "true" : "false"} />
        <IdentityButton variant="danger" type="submit">{submitLabel}</IdentityButton>
      </form>
    </IdentityPanel>
  );
}
