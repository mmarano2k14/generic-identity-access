import { IdentityButton, IdentityInput, IdentityPanel } from "../components/index";

export interface SessionRevocationFormProps {
  readonly target: "user" | "client";
  readonly formAction?: string;
  readonly submitLabel?: string;
}

/** Presentation-only form for consumer-owned administrative session revocation actions. */
export function SessionRevocationForm({ target, formAction, submitLabel = "Revoke sessions" }: SessionRevocationFormProps) {
  const isUser = target === "user";
  return (
    <IdentityPanel title={isUser ? "Revoke user sessions" : "Revoke client sessions"}>
      <form className="gi-form" method="post" action={formAction} data-gi-component="session-revocation-form">
        <label className="gi-field">
          <span className="gi-field-label">{isUser ? "User ID" : "Client ID"}</span>
          <IdentityInput name={isUser ? "userId" : "clientId"} required />
        </label>
        <input type="hidden" name="target" value={target} />
        <IdentityButton variant="danger" type="submit">{submitLabel}</IdentityButton>
      </form>
    </IdentityPanel>
  );
}
