"use client";

import Link from "next/link";
import { useActionState, useState } from "react";
import { AdminIcon } from "../../components/AdminIcon";
import { initialLoginActionState } from "../../contracts/LoginActionState";
import { loginAction } from "./actions";

export function LoginForm() {
  const [state, action, pending] = useActionState(loginAction, initialLoginActionState);
  const [showPassword, setShowPassword] = useState(false);

  return (
    <form action={action} className="ia-login-form" aria-busy={pending}>
      <label className="ia-field">
        <span className="ia-field-label">Login identifier</span>
        <input
          className="ia-input"
          name="loginIdentifier"
          type="text"
          autoComplete="username"
          autoFocus
          required
          maxLength={320}
          placeholder="name@example.com"
        />
      </label>
      <label className="ia-field">
        <span className="ia-field-row">
          <span className="ia-field-label">Password</span>
          <Link className="ia-auth-inline-link" href="/recovery">Recover access</Link>
        </span>
        <span className="ia-password-field">
          <input
            className="ia-input"
            name="password"
            type={showPassword ? "text" : "password"}
            autoComplete="current-password"
            required
            maxLength={4096}
            placeholder="Enter your password"
          />
          <button
            className="ia-password-toggle"
            type="button"
            aria-label={showPassword ? "Hide password" : "Show password"}
            aria-pressed={showPassword}
            disabled={pending}
            onClick={() => setShowPassword((value) => !value)}
          >
            <AdminIcon name={showPassword ? "eye-off" : "eye"} />
          </button>
        </span>
      </label>
      {state.status === "error" ? (
        <p className="ia-feedback ia-feedback-error ia-login-feedback" role="alert">{state.message}</p>
      ) : null}
      <button className="ia-button ia-button-primary ia-login-submit" type="submit" disabled={pending}>
        {pending ? "Signing in…" : "Sign in securely"}
      </button>
      <div className="ia-auth-form-assurance" aria-label="Sign-in protections">
        <span><AdminIcon name="shield" />OIDC + PKCE</span>
        <span><AdminIcon name="lock" />HTTP-only session</span>
      </div>
    </form>
  );
}
