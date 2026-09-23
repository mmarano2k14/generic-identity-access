"use client";

import { useActionState } from "react";
import { initialLoginActionState } from "../../contracts/LoginActionState";
import { loginAction } from "./actions";

export function LoginForm() {
  const [state, action, pending] = useActionState(loginAction, initialLoginActionState);

  return (
    <form action={action} className="ia-login-form">
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
        <span className="ia-field-label">Password</span>
        <input
          className="ia-input"
          name="password"
          type="password"
          autoComplete="current-password"
          required
          maxLength={4096}
          placeholder="Enter your password"
        />
      </label>
      {state.status === "error" ? (
        <p className="ia-feedback ia-feedback-error ia-login-feedback" role="alert">{state.message}</p>
      ) : null}
      <button className="ia-button ia-button-primary ia-login-submit" type="submit" disabled={pending}>
        {pending ? "Signing in…" : "Sign in securely"}
      </button>
    </form>
  );
}
