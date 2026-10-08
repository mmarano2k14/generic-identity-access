import type { ComponentProps, ReactNode } from "react";
import type { IdentityOrganizationResourceScopeLinkRecord } from "@generic-identity/contracts/organizations";
import { IdentityButton, IdentityEntityAutocomplete } from "../components/index";

export interface OrganizationResourceScopeLinkFormProps {
  readonly tenantId: string;
  readonly organizationId: string;
  readonly link?: IdentityOrganizationResourceScopeLinkRecord | null;
  readonly formAction?: ComponentProps<"form">["action"];
  readonly error?: ReactNode;
}

/** Maps one Organization to one existing active ResourceScope. */
export function OrganizationResourceScopeLinkForm({ tenantId, organizationId, link = null, formAction, error }: OrganizationResourceScopeLinkFormProps) {
  return (
    <form className="gi-form" method={typeof formAction === "function" ? undefined : "post"} action={formAction} data-gi-component="organization-resource-scope-link-form">
      <input type="hidden" name="tenantId" value={tenantId} />
      <input type="hidden" name="organizationId" value={organizationId} />
      <IdentityEntityAutocomplete
        label="Resource scope"
        name="resourceScopeId"
        kind="resource-scope"
        tenantId={tenantId}
        defaultValue={link?.resourceScopeId ?? ""}
        required
        hint="Search the protected active ResourceScope catalog. The server revalidates the selected scope before saving."
      />
      {link ? <input type="hidden" name="expectedRowVersion" value={link.rowVersion} /> : null}
      {error ? <div className="gi-form-error" role="alert">{error}</div> : null}
      <IdentityButton variant="primary" type="submit">{link ? "Update scope link" : "Link resource scope"}</IdentityButton>
    </form>
  );
}
