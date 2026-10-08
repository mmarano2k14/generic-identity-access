import type { ComponentProps, ReactNode } from "react";
import type { IdentityScopeTypeRecord } from "@generic-identity/contracts/application-security";
import { IdentityButton, IdentityInput } from "../components/index";

export interface ApplicationScopeTypeFormProps {
  /** Backward-compatible keys-only input. */
  readonly parentKeys?: readonly string[];
  /** Prefer these when display names are available from the selected model. */
  readonly parentTypes?: readonly Pick<IdentityScopeTypeRecord, "key" | "displayName">[];
  readonly formAction?: ComponentProps<"form">["action"];
  readonly error?: ReactNode;
}

/** Root types attach to a tenant; child types must name a registered parent. */
export function ApplicationScopeTypeForm({
  parentKeys = [],
  parentTypes,
  formAction,
  error,
}: ApplicationScopeTypeFormProps) {
  const candidates = parentTypes ?? parentKeys.map((key) => ({ key, displayName: key }));
  return (
    <form
      className="gi-form"
      method={typeof formAction === "function" ? undefined : "post"}
      action={formAction}
      data-gi-component="application-scope-type-form"
    >
      <label className="gi-field">
        <span className="gi-field-label">Scope type key</span>
        <IdentityInput name="key" required maxLength={64} pattern="[a-z][a-z0-9-]{0,63}" placeholder="ecommerce" />
      </label>
      <label className="gi-field">
        <span className="gi-field-label">Display name</span>
        <IdentityInput name="displayName" required maxLength={200} placeholder="E-commerce" />
      </label>
      <label className="gi-field">
        <span className="gi-field-label">Parent scope type</span>
        <select className="gi-input" name="parentKey" defaultValue="">
          <option value="">Root scope type (tenant attachment)</option>
          {candidates.map(({ key, displayName }) => (
            <option key={key} value={key}>{displayName} ({key})</option>
          ))}
        </select>
      </label>
      <p className="gi-form-hint">Tenant attachment is determined by the hierarchy: roots may attach to tenants; children must use a registered parent in this model version.</p>
      {error ? <div className="gi-form-error" role="alert">{error}</div> : null}
      <IdentityButton variant="primary" type="submit">Register scope type</IdentityButton>
    </form>
  );
}
