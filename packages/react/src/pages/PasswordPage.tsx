import type { ReactNode } from "react";
import { IdentityButton, IdentityInput, IdentityPageFrame, IdentityPanel } from "../components/index";

export interface PasswordPageProps {
  readonly formAction?: string;
  readonly error?: ReactNode;
  readonly footer?: ReactNode;
}

export function PasswordPage({ formAction, error, footer }: PasswordPageProps) {
  return (
    <IdentityPageFrame title="Password" description="Change the password for the current authenticated account.">
      <IdentityPanel>
        <form className="gi-form" method="post" action={formAction} data-gi-component="password-change-form">
          <label className="gi-field"><span className="gi-field-label">Current password</span><IdentityInput name="currentPassword" type="password" autoComplete="current-password" required /></label>
          <label className="gi-field"><span className="gi-field-label">New password</span><IdentityInput name="newPassword" type="password" autoComplete="new-password" required /></label>
          {error ? <div className="gi-form-error" role="alert">{error}</div> : null}
          <IdentityButton variant="primary" type="submit">Change password</IdentityButton>
        </form>
        {footer ? <div className="gi-form-footer">{footer}</div> : null}
      </IdentityPanel>
    </IdentityPageFrame>
  );
}
