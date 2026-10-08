import type { ReactNode } from "react";
import type { IdentityOrganizationMembershipRecord, IdentityOrganizationRecord } from "@generic-identity/contracts";
import { IdentityButton } from "../components/index";

export interface MemberOrganizationAssignmentsFormProps {
  readonly tenantId: string;
  readonly tenantMembershipId: string;
  readonly organizations: readonly IdentityOrganizationRecord[];
  readonly memberships: readonly IdentityOrganizationMembershipRecord[];
  readonly formAction?: string | ((formData: FormData) => void | Promise<void>);
  readonly error?: ReactNode;
  readonly disabled?: boolean;
}

/** Organization belonging is separate from group/policy authorization. */
export function MemberOrganizationAssignmentsForm({
  tenantId, tenantMembershipId, organizations, memberships, formAction, error, disabled = false,
}: MemberOrganizationAssignmentsFormProps) {
  const active = new Set(memberships.filter((membership) => membership.status === 1).map((membership) => membership.organizationId));
  return (
    <form className="gi-form" method={typeof formAction === "function" ? undefined : "post"} action={formAction} data-gi-component="member-organization-assignments-form">
      <input type="hidden" name="tenantId" value={tenantId} />
      <input type="hidden" name="tenantMembershipId" value={tenantMembershipId} />
      {organizations.length === 0 ? <p className="gi-muted">No Organizations available.</p> : organizations.map((organization) => (
        <label key={organization.organizationId} className="gi-field">
          <span className="gi-field-label">
            <input
              type="checkbox"
              name="organizationSelection"
              value={`organization:${organization.organizationId}`}
              defaultChecked={active.has(organization.organizationId)}
              disabled={disabled || (organization.status !== 1 && !active.has(organization.organizationId))}
            /> {organization.displayName}{organization.status !== 1 ? " (Inactive)" : ""}
          </span>
        </label>
      ))}
      {error ? <div className="gi-form-error" role="alert">{error}</div> : null}
      <IdentityButton variant="primary" type="submit" disabled={disabled}>Save Organization memberships</IdentityButton>
    </form>
  );
}
