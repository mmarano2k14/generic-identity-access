import type { ReactNode } from "react";
import {
  IdentityButton,
  IdentityInput,
  IdentityPageFrame,
  IdentityPanel,
} from "../components/index";

export interface RecoveryPageProps {
  readonly formAction?: string;
  readonly error?: ReactNode;
  readonly footer?: ReactNode;
}

export function RecoveryPage({ formAction, error, footer }: RecoveryPageProps) {
  return (
    <IdentityPageFrame title="Account recovery" description="Use an approved recovery code to reset the account password.">
      <IdentityPanel>
        <form className="gi-form" method="post" action={formAction} data-gi-component="recovery-form">
          <label className="gi-field"><span className="gi-field-label">Login</span><IdentityInput name="loginIdentifier" autoComplete="username" required /></label>
          <label className="gi-field"><span className="gi-field-label">Recovery code</span><IdentityInput name="recoveryCode" autoComplete="one-time-code" required /></label>
          <label className="gi-field"><span className="gi-field-label">New password</span><IdentityInput name="newPassword" type="password" autoComplete="new-password" required /></label>
          {error ? <div className="gi-form-error" role="alert">{error}</div> : null}
          <IdentityButton variant="primary" type="submit">Reset password</IdentityButton>
        </form>
        {footer ? <div className="gi-form-footer">{footer}</div> : null}
      </IdentityPanel>
    </IdentityPageFrame>
  );
}
