import type { ComponentProps, ReactNode } from "react";
import type { IdentityOrganizationRecord } from "@generic-identity/contracts/organizations";
import { IdentityButton, IdentityInput } from "../components/index";

export interface OrganizationFormProps {
  readonly tenantId?: string;
  readonly organization?: IdentityOrganizationRecord | null;
  readonly parentOrganizations?: readonly IdentityOrganizationRecord[];
  readonly formAction?: ComponentProps<"form">["action"];
  readonly error?: ReactNode;
}

/** Generic Organization definition form matching the Identity Admin GOLDEN bounds. */
export function OrganizationForm({ tenantId, organization = null, parentOrganizations = [], formAction, error }: OrganizationFormProps) {
  return (
    <form className="gi-form" method={typeof formAction === "function" ? undefined : "post"} action={formAction} data-gi-component="organization-form">
      {tenantId ? <input type="hidden" name="tenantId" value={tenantId} /> : null}
      {organization ? <><input type="hidden" name="organizationId" value={organization.organizationId} /><input type="hidden" name="expectedRowVersion" value={organization.rowVersion} /></> : null}
      {!organization ? <label className="gi-field"><span className="gi-field-label">Organization key</span><IdentityInput name="organizationKey" required maxLength={64} pattern="[a-z][a-z0-9-]{0,63}" /><small className="gi-field-hint">Stable tenant-local lowercase key, for example urban-flower.</small></label> : null}
      <label className="gi-field"><span className="gi-field-label">Display name</span><IdentityInput name="displayName" defaultValue={organization?.displayName ?? ""} required maxLength={200} /></label>
      <label className="gi-field"><span className="gi-field-label">Organization type</span><IdentityInput name="organizationType" defaultValue={organization?.organizationType ?? "organization"} required maxLength={64} pattern="[a-z][a-z0-9-]{0,63}" /><small className="gi-field-hint">Generic classification such as organization, business, division, department, site, or branch.</small></label>
      <label className="gi-field"><span className="gi-field-label">Parent organization</span><select className="gi-input" name="parentOrganizationId" defaultValue={organization?.parentOrganizationId ?? ""}><option value="">No parent</option>{parentOrganizations.filter((candidate) => candidate.organizationId !== organization?.organizationId).slice().sort((a,b) => a.displayName.localeCompare(b.displayName)).map((candidate) => <option key={candidate.organizationId} value={candidate.organizationId}>{candidate.displayName}</option>)}</select></label>
      {error ? <div className="gi-form-error" role="alert">{error}</div> : null}
      <IdentityButton variant="primary" type="submit">{organization ? "Save organization" : "Create organization"}</IdentityButton>
    </form>
  );
}
