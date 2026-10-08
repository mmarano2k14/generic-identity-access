import type { ComponentProps, ReactNode } from "react";
import type {
  IdentityMfaPolicyMode,
  IdentityMfaProviderRecord,
} from "@generic-identity/contracts/security-operations";
import { IdentityButton, IdentityPanel } from "../components/index";

export interface MfaPolicyFormProps {
  readonly providers: readonly IdentityMfaProviderRecord[];
  readonly mode?: IdentityMfaPolicyMode;
  readonly allowedProviders?: readonly string[];
  readonly expectedVersion?: number;
  readonly formAction?: ComponentProps<"form">["action"];
  readonly submitLabel?: string;
  readonly error?: ReactNode;
}

/** Presentation-only form for consumer-owned MFA policy server actions. */
export function MfaPolicyForm({
  providers,
  mode = 1,
  allowedProviders = [],
  expectedVersion,
  formAction,
  submitLabel = expectedVersion === undefined ? "Create policy" : "Update policy",
  error,
}: MfaPolicyFormProps) {
  return (
    <IdentityPanel title="MFA policy">
      <form
        className="gi-form"
        method={typeof formAction === "function" ? undefined : "post"}
        action={formAction}
        data-gi-component="mfa-policy-form"
      >
        <label className="gi-field">
          <span className="gi-field-label">Mode</span>
          <select className="gi-input" name="mode" defaultValue={String(mode)} required>
            <option value="1">Disabled</option>
            <option value="2">Optional</option>
            <option value="3">Required</option>
          </select>
        </label>
        <fieldset className="gi-fieldset">
          <legend>Allowed providers</legend>
          {providers.length === 0 ? <p>No MFA providers are installed in this host.</p> : providers.map((provider) => (
            <label key={provider.key} className="gi-checkbox">
              <input
                type="checkbox"
                name="allowedProviders"
                value={provider.key}
                defaultChecked={allowedProviders.includes(provider.key)}
              />
              <span>{provider.displayName} <code>{provider.key}</code></span>
            </label>
          ))}
        </fieldset>
        {expectedVersion === undefined ? null : (
          <input type="hidden" name="expectedVersion" value={String(expectedVersion)} />
        )}
        {error ? <div className="gi-form-error" role="alert">{error}</div> : null}
        <IdentityButton variant="primary" type="submit">{submitLabel}</IdentityButton>
      </form>
    </IdentityPanel>
  );
}
