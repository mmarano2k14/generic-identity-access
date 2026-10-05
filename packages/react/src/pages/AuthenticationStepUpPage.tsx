import type { ReactNode } from "react";
import { IdentityPageFrame, IdentityPanel } from "../components/index";

export interface AuthenticationStepUpPageProps {
  readonly totp?: ReactNode;
  readonly recovery?: ReactNode;
  readonly webAuthn?: ReactNode;
  readonly message?: ReactNode;
}

/** Shared presentation shell for supported session step-up mechanisms. */
export function AuthenticationStepUpPage({ totp, recovery, webAuthn, message }: AuthenticationStepUpPageProps) {
  return (
    <IdentityPageFrame title="Verify your identity" description="Complete an approved additional authentication factor.">
      {message ? <IdentityPanel>{message}</IdentityPanel> : null}
      {totp ? <IdentityPanel title="Authenticator code">{totp}</IdentityPanel> : null}
      {webAuthn ? <IdentityPanel title="Passkey / security key">{webAuthn}</IdentityPanel> : null}
      {recovery ? <IdentityPanel title="Recovery code">{recovery}</IdentityPanel> : null}
    </IdentityPageFrame>
  );
}
