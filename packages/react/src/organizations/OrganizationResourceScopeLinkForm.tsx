import type { ReactNode } from "react";
import type { IdentityOrganizationResourceScopeLinkRecord } from "@generic-identity/contracts/organizations";
import { IdentityButton, IdentityInput } from "../components/index";

export interface OrganizationResourceScopeLinkFormProps {
  readonly organizationId: string;
  readonly link?: IdentityOrganizationResourceScopeLinkRecord | null;
  readonly formAction?: string;
  readonly error?: ReactNode;
}

export function OrganizationResourceScopeLinkForm({ organizationId, link = null, formAction, error }: OrganizationResourceScopeLinkFormProps) {
  return (
    <form className="gi-form" method="post" action={formAction} data-gi-component="organization-resource-scope-link-form">
      <input type="hidden" name="organizationId" value={organizationId} />
      <label className="gi-field"><span className="gi-field-label">Resource scope ID</span><IdentityInput name="resourceScopeId" defaultValue={link?.resourceScopeId ?? ""} required /></label>
      {link ? <input type="hidden" name="expectedRowVersion" value={link.rowVersion} /> : null}
      {error ? <div className="gi-form-error" role="alert">{error}</div> : null}
      <IdentityButton variant="primary" type="submit">{link ? "Update scope link" : "Link resource scope"}</IdentityButton>
    </form>
  );
}
