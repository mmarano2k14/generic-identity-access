import Link from "next/link";
import type { ReactNode } from "react";
import { AdminEmptyState } from "./AdminEmptyState";

export interface AdminTenantGroupCatalogRow {
  readonly groupId: string;
  readonly displayName: string;
  readonly status: string;
  readonly version: number;
  readonly isTemplate: boolean;
  readonly selected?: boolean;
  readonly manageHref: string;
  readonly actions?: ReactNode;
}

export function AdminTenantGroupCatalog({ rows }: { readonly rows: readonly AdminTenantGroupCatalogRow[] }) {
  return (
    <section className="ia-table-card">
      <div className="ia-table-heading">
        <div>
          <p className="ia-card-kicker">Tenant groups</p>
          <h2>Groups in this tenant</h2>
          <p>Every row is a real tenant group. A reusable group is marked Template and can be cloned without copying members.</p>
        </div>
        <span className="ia-count-badge">{rows.length} groups</span>
      </div>
      {rows.length === 0 ? (
        <AdminEmptyState title="No tenant groups" description="Create a group or create one from a reusable group template." />
      ) : (
        <div className="ia-table-scroll">
          <table className="ia-table">
            <thead>
              <tr><th scope="col">Group</th><th scope="col">Template</th><th scope="col">Identifier</th><th scope="col">Status</th><th scope="col">Version</th><th scope="col" className="ia-actions-heading">Actions</th></tr>
            </thead>
            <tbody>
              {rows.map((row) => (
                <tr key={row.groupId} className={row.selected ? "ia-table-row-selected" : undefined}>
                  <td data-label="Group" className="ia-table-cell-name"><strong>{row.displayName}</strong></td>
                  <td data-label="Template"><span className={`ia-origin-badge ${row.isTemplate ? "ia-origin-template" : "ia-origin-tenant"}`}>{row.isTemplate ? "Yes" : "No"}</span></td>
                  <td data-label="Identifier"><code className="ia-id-chip" title={row.groupId}>{row.groupId}</code></td>
                  <td data-label="Status"><span className={`ia-status ${row.status === "Active" ? "ia-status-active" : "ia-status-inactive"}`}><span className="ia-status-dot" aria-hidden="true" />{row.status}</span></td>
                  <td data-label="Version"><span className="ia-version-chip">v{row.version}</span></td>
                  <td data-label="Actions" className="ia-table-cell-actions"><div className="ia-row-actions"><Link className="ia-button ia-button-secondary ia-button-compact" href={row.manageHref}>Manage</Link>{row.actions}</div></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}
