import type { ReactNode } from "react";
import { IdentityButton, IdentityEntityAutocomplete, IdentityInput, IdentityPanel } from "../components/index";

export interface SessionRevocationFormProps {
  readonly target: "user" | "client";
  readonly formAction?: string | ((formData: FormData) => void | Promise<void>);
  readonly submitLabel?: string;
  readonly defaultUserId?: string;
  readonly defaultClientId?: string;
  readonly error?: string;
  readonly userLookupHint?: ReactNode;
}

/** Presentation-only form for server-authorized session containment actions. */
export function SessionRevocationForm({
  target,
  formAction,
  submitLabel = "Revoke sessions",
  defaultUserId = "",
  defaultClientId = "",
  error,
  userLookupHint,
}: SessionRevocationFormProps) {
  const isUser = target === "user";
  return (
    <IdentityPanel
      title={isUser ? "Revoke user sessions" : "Revoke client sessions"}
      description={
        isUser
          ? "Invalidate active local sessions owned by one user without changing the identity record."
          : "Invalidate active sessions issued to one registered authentication client. This may sign out many users."
      }
    >
      <form
        className="gi-form"
        method={typeof formAction === "function" ? undefined : "post"}
        action={formAction}
        data-gi-component="session-revocation-form"
      >
        {isUser ? (
          <IdentityEntityAutocomplete
            label="User"
            name="userId"
            kind="user"
            defaultValue={defaultUserId}
            required
            hint={
              typeof userLookupHint === "string"
                ? userLookupHint
                : "Search is server-backed. Select the stable user identity to contain."
            }
          />
        ) : (
          <label className="gi-field">
            <span className="gi-field-label">Client ID</span>
            <IdentityInput name="clientId" defaultValue={defaultClientId} required maxLength={128} autoComplete="off" />
          </label>
        )}

        <label className="gi-field">
          <span className="gi-field-label">Type REVOKE to confirm</span>
          <IdentityInput name="confirmation" required autoComplete="off" placeholder="REVOKE" />
        </label>

        {error ? <p className="gi-form-error" role="alert">{error}</p> : null}
        <IdentityButton variant="danger" type="submit">{submitLabel}</IdentityButton>
      </form>
    </IdentityPanel>
  );
}
