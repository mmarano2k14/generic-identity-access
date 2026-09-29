import Link from "next/link";
import type { IdentityOrganizationRecord } from "@identity-access/client";
import { AdminIcon } from "./AdminIcon";
import { AdminOrganizationRowActions } from "./AdminOrganizationRowActions";

type Props = {
  readonly tenantId: string;
  readonly organizations: readonly IdentityOrganizationRecord[];
  readonly selectedOrganizationId?: string;
  readonly canReadScopeLinks: boolean;
  readonly canManageOrganizations: boolean;
};

function hierarchyLabel(
  organization: IdentityOrganizationRecord,
  organizationsById: ReadonlyMap<string, IdentityOrganizationRecord>,
): string {
  const names = [organization.displayName];
  const visited = new Set<string>([organization.organizationId]);
  let parentId = organization.parentOrganizationId;

  while (parentId) {
    if (visited.has(parentId)) {
      return `Cycle / ${names.reverse().join(" / ")}`;
    }

    visited.add(parentId);
    const parent = organizationsById.get(parentId);
    if (!parent) {
      return `Missing parent / ${names.reverse().join(" / ")}`;
    }

    names.push(parent.displayName);
    parentId = parent.parentOrganizationId;
  }

  return names.reverse().join(" / ");
}

/** Renders the Organization hierarchy table; mutation forms live in row-action components. */
export function AdminOrganizationTable({
  tenantId,
  organizations,
  selectedOrganizationId,
  canReadScopeLinks,
  canManageOrganizations,
}: Props) {
  const byId = new Map(
    organizations.map((organization) => [
      organization.organizationId,
      organization,
    ]),
  );

  const sorted = organizations
    .slice()
    .sort((left, right) =>
      hierarchyLabel(left, byId).localeCompare(
        hierarchyLabel(right, byId),
      ),
    );

  return (
    <div className="ia-table-scroll">
      <table className="ia-table">
        <thead>
          <tr>
            <th scope="col">Organization</th>
            <th scope="col">Key</th>
            <th scope="col">Type</th>
            <th scope="col">Hierarchy</th>
            <th scope="col">Status</th>
            <th scope="col" className="ia-actions-heading">
              Actions
            </th>
          </tr>
        </thead>
        <tbody>
          {sorted.map((organization) => (
            <tr
              key={organization.organizationId}
              className={
                selectedOrganizationId === organization.organizationId
                  ? "ia-table-row-selected"
                  : undefined
              }
            >
              <td
                data-label="Organization"
                className="ia-table-cell-name"
              >
                <div className="ia-entity-name">
                  <span
                    className="ia-entity-avatar"
                    aria-hidden="true"
                  >
                    {organization.displayName
                      .trim()
                      .slice(0, 1)
                      .toUpperCase() || "O"}
                  </span>
                  <strong>{organization.displayName}</strong>
                </div>
              </td>
              <td data-label="Key">
                <code className="ia-id-chip">
                  {organization.organizationKey}
                </code>
              </td>
              <td data-label="Type">
                <span className="ia-origin-badge ia-origin-tenant">
                  {organization.organizationType}
                </span>
              </td>
              <td data-label="Hierarchy">
                <span className="ia-muted">
                  {hierarchyLabel(organization, byId)}
                </span>
              </td>
              <td data-label="Status">
                <span
                  className={`ia-status ${
                    organization.status === 1
                      ? "ia-status-active"
                      : "ia-status-inactive"
                  }`}
                >
                  <span
                    className="ia-status-dot"
                    aria-hidden="true"
                  />
                  {organization.status === 1
                    ? "Active"
                    : "Disabled"}
                </span>
              </td>
              <td
                data-label="Actions"
                className="ia-table-cell-actions"
              >
                <div className="ia-row-actions">
                  {canReadScopeLinks ? (
                    <Link
                      className="ia-button ia-button-secondary ia-button-compact"
                      href={`/identity/memberships?tenantId=${encodeURIComponent(
                        tenantId,
                      )}&organizationId=${encodeURIComponent(
                        organization.organizationId,
                      )}`}
                    >
                      <AdminIcon name="resource-scopes" />
                      Scope
                    </Link>
                  ) : null}

                  {canManageOrganizations ? (
                    <AdminOrganizationRowActions
                      tenantId={tenantId}
                      organization={organization}
                      organizations={organizations}
                    />
                  ) : null}
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
