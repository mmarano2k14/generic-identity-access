"use client";

import Link from "next/link";
import { useActionState, useState } from "react";
import { AdminIcon } from "../../components/AdminIcon";
import { initialRecoveryActionState } from "../../contracts/RecoveryActionState";
import { recoverPasswordAction } from "./actions";

export function RecoveryForm() {
  const [state, action, pending] = useActionState(recoverPasswordAction, initialRecoveryActionState);
  const [showPasswords, setShowPasswords] = useState(false);

  return (
    <form action={action} className="ia-login-form ia-recovery-form" aria-busy={pending}>
      <label className="ia-field">
        <span className="ia-field-label">Login identifier</span>
        <input className="ia-input" name="loginIdentifier" type="text" autoComplete="username" autoFocus required maxLength={320} placeholder="name@example.com" />
      </label>
      <label className="ia-field">
        <span className="ia-field-label">Recovery code</span>
        <input className="ia-input ia-input-mono" name="recoveryCode" type="text" autoComplete="one-time-code" autoCapitalize="none" spellCheck={false} required maxLength={128} placeholder="XXXX-XXXX-XXXX-XXXX" aria-describedby="recovery-code-hint" />
        <small className="ia-field-hint" id="recovery-code-hint">One unused recovery code is consumed when the password replacement succeeds.</small>
      </label>
      <div className="ia-recovery-password-grid">
        <label className="ia-field">
          <span className="ia-field-label">New password</span>
          <span className="ia-password-field">
            <input className="ia-input" name="newPassword" type={showPasswords ? "text" : "password"} autoComplete="new-password" required minLength={12} maxLength={256} placeholder="At least 12 characters" />
            <button className="ia-password-toggle" type="button" aria-label={showPasswords ? "Hide passwords" : "Show passwords"} aria-pressed={showPasswords} disabled={pending} onClick={() => setShowPasswords((value) => !value)}>
              <AdminIcon name={showPasswords ? "eye-off" : "eye"} />
            </button>
          </span>
        </label>
        <label className="ia-field">
          <span className="ia-field-label">Confirm new password</span>
          <input className="ia-input" name="confirmPassword" type={showPasswords ? "text" : "password"} autoComplete="new-password" required minLength={12} maxLength={256} placeholder="Repeat the new password" />
        </label>
      </div>
      {state.status === "error" ? <p className="ia-feedback ia-feedback-error ia-login-feedback" role="alert">{state.message}</p> : null}
      <button className="ia-button ia-button-primary ia-login-submit" type="submit" disabled={pending}>
        {pending ? "Replacing password…" : "Replace password"}
      </button>
      <Link className="ia-auth-back-link" href="/login"><AdminIcon name="back" />Back to sign in</Link>
    </form>
  );
}
