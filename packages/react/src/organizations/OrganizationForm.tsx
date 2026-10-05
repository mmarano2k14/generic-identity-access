import type { ReactNode } from "react";
import type { IdentityOrganizationRecord } from "@generic-identity/contracts/organizations";
import { IdentityButton, IdentityInput } from "../components/index";

export interface OrganizationFormProps {
  readonly organization?: IdentityOrganizationRecord | null;
  readonly parentOrganizations?: readonly IdentityOrganizationRecord[];
  readonly formAction?: string;
  readonly error?: ReactNode;
}

export function OrganizationForm({ organization = null, parentOrganizations = [], formAction, error }: OrganizationFormProps) {
  return (
    <form className="gi-form" method="post" action={formAction} data-gi-component="organization-form">
      {!organization ? <label className="gi-field"><span className="gi-field-label">Organization key</span><IdentityInput name="organizationKey" required /></label> : null}
      <label className="gi-field"><span className="gi-field-label">Display name</span><IdentityInput name="displayName" defaultValue={organization?.displayName ?? ""} required /></label>
      <label className="gi-field"><span className="gi-field-label">Organization type</span><IdentityInput name="organizationType" defaultValue={organization?.organizationType ?? ""} required /></label>
      <label className="gi-field"><span className="gi-field-label">Parent organization</span><select className="gi-input" name="parentOrganizationId" defaultValue={organization?.parentOrganizationId ?? ""}><option value="">None</option>{parentOrganizations.filter((candidate) => candidate.organizationId !== organization?.organizationId).map((candidate) => <option key={candidate.organizationId} value={candidate.organizationId}>{candidate.displayName}</option>)}</select></label>
      {organization ? <input type="hidden" name="expectedRowVersion" value={organization.rowVersion} /> : null}
      {error ? <div className="gi-form-error" role="alert">{error}</div> : null}
      <IdentityButton variant="primary" type="submit">{organization ? "Save organization" : "Create organization"}</IdentityButton>
    </form>
  );
}
