import type { ReactNode } from "react";
import {
  IdentityButton,
  IdentityInput,
  IdentityPageFrame,
  IdentityPanel,
} from "../components/index";

export interface SignInPageProps {
  readonly formAction?: string;
  readonly loginIdentifierName?: string;
  readonly passwordName?: string;
  readonly error?: ReactNode;
  readonly footer?: ReactNode;
}

export function SignInPage({
  formAction,
  loginIdentifierName = "loginIdentifier",
  passwordName = "password",
  error,
  footer,
}: SignInPageProps) {
  return (
    <IdentityPageFrame title="Sign in" description="Authenticate with your Identity account.">
      <IdentityPanel>
        <form className="gi-form" method="post" action={formAction} data-gi-component="sign-in-form">
          <label className="gi-field"><span className="gi-field-label">Login</span><IdentityInput name={loginIdentifierName} autoComplete="username" required /></label>
          <label className="gi-field"><span className="gi-field-label">Password</span><IdentityInput name={passwordName} type="password" autoComplete="current-password" required /></label>
          {error ? <div className="gi-form-error" role="alert">{error}</div> : null}
          <IdentityButton variant="primary" type="submit">Sign in</IdentityButton>
        </form>
        {footer ? <div className="gi-form-footer">{footer}</div> : null}
      </IdentityPanel>
    </IdentityPageFrame>
  );
}
