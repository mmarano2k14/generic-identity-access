import type { IdentityMfaPolicyMode, IdentityMfaProviderRecord } from "@generic-identity/contracts/security-operations";
import { IdentityButton, IdentityPanel } from "../components/index";

export interface MfaPolicyFormProps {
  readonly providers: readonly IdentityMfaProviderRecord[];
  readonly mode?: IdentityMfaPolicyMode;
  readonly allowedProviders?: readonly string[];
  readonly expectedVersion?: number;
  readonly formAction?: string;
  readonly submitLabel?: string;
}

/** Presentation-only form for consumer-owned MFA policy server actions. */
export function MfaPolicyForm({
  providers,
  mode = 1,
  allowedProviders = [],
  expectedVersion,
  formAction,
  submitLabel = expectedVersion === undefined ? "Create policy" : "Update policy",
}: MfaPolicyFormProps) {
  return (
    <IdentityPanel title="MFA policy">
      <form className="gi-form" method="post" action={formAction} data-gi-component="mfa-policy-form">
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
          {providers.map((provider) => (
            <label key={provider.key} className="gi-checkbox">
              <input
                type="checkbox"
                name="allowedProviders"
                value={provider.key}
                defaultChecked={allowedProviders.includes(provider.key)}
              />
              <span>{provider.displayName}</span>
            </label>
          ))}
        </fieldset>
        {expectedVersion === undefined ? null : (
          <input type="hidden" name="expectedVersion" value={String(expectedVersion)} />
        )}
        <IdentityButton variant="primary" type="submit">{submitLabel}</IdentityButton>
      </form>
    </IdentityPanel>
  );
}
