import type { ReactNode } from "react";
import type { IdentityGroupRecord } from "@generic-identity/contracts";
import { IdentityButton } from "../components/index";

export interface MemberGroupAssignmentsFormProps {
  readonly tenantId: string;
  readonly tenantMembershipId: string;
  readonly groups: readonly IdentityGroupRecord[];
  readonly assignedGroupIds: readonly string[];
  readonly formAction?: string | ((formData: FormData) => void | Promise<void>);
  readonly error?: ReactNode;
  readonly disabled?: boolean;
}

/** Group selections only; actual grants continue to come from RBAC policies. */
export function MemberGroupAssignmentsForm({
  tenantId, tenantMembershipId, groups, assignedGroupIds, formAction, error, disabled = false,
}: MemberGroupAssignmentsFormProps) {
  const assigned = new Set(assignedGroupIds);
  return (
    <form className="gi-form" method={typeof formAction === "function" ? undefined : "post"} action={formAction} data-gi-component="member-group-assignments-form">
      <input type="hidden" name="tenantId" value={tenantId} />
      <input type="hidden" name="tenantMembershipId" value={tenantMembershipId} />
      {groups.length === 0 ? <p className="gi-muted">No tenant groups available.</p> : groups.map((group) => (
        <label key={group.groupId} className="gi-field">
          <span className="gi-field-label">
            <input
              type="checkbox"
              name="groupSelection"
              value={`group:${group.groupId}`}
              defaultChecked={assigned.has(group.groupId)}
              disabled={disabled || (group.status !== 1 && !assigned.has(group.groupId))}
            /> {group.displayName}{group.status !== 1 ? " (Inactive)" : ""}
          </span>
        </label>
      ))}
      {error ? <div className="gi-form-error" role="alert">{error}</div> : null}
      <IdentityButton variant="primary" type="submit" disabled={disabled}>Save group assignments</IdentityButton>
    </form>
  );
}
