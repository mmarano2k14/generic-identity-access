"use client";

import { useMemo, useState } from "react";
import { AdminEmptyState } from "./AdminEmptyState";
import { AdminIcon } from "./AdminIcon";

export interface AdminEntityTableRow {
  readonly id: string;
  readonly name: string;
  readonly status: string;
  readonly version?: number;
}

export interface AdminEntityTableProps {
  readonly rows: readonly AdminEntityTableRow[];
  readonly title?: string;
  readonly description?: string;
  readonly entityLabel?: string;
}

/** Client Component limited to presentation filtering; authorization and data loading stay server-side. */
export function AdminEntityTable({ rows, title = "Records", description = "Browse the current administration view.", entityLabel = "records" }: AdminEntityTableProps) {
  const [query, setQuery] = useState("");
  const filtered = useMemo(() => {
    const normalized = query.trim().toLowerCase();
    if (!normalized) return rows;
    return rows.filter((row) => `${row.id} ${row.name} ${row.status}`.toLowerCase().includes(normalized));
  }, [query, rows]);

  return (
    <section className="ia-table-card">
      <div className="ia-table-heading">
        <div>
          <p className="ia-card-kicker">Collection</p>
          <h2>{title}</h2>
          <p>{description}</p>
        </div>
        <span className="ia-count-badge">{rows.length} {entityLabel}</span>
      </div>
      <div className="ia-table-toolbar">
        <label className="ia-search-field">
          <span className="ia-search-icon"><AdminIcon name="search" /></span>
          <span className="ia-sr-only">Filter this page</span>
          <input className="ia-input ia-search-input" value={query} onChange={(event) => setQuery(event.target.value)} placeholder={`Search ${entityLabel}`} />
        </label>
        <span className="ia-result-count">Showing <strong>{filtered.length}</strong> of {rows.length}</span>
      </div>
      {filtered.length === 0 ? (
        <AdminEmptyState
          title={rows.length === 0 ? "Nothing here yet" : "No matching records"}
          description={rows.length === 0 ? "Create the first record when you are ready." : "Try a different name, identifier, or status."}
        />
      ) : (
        <div className="ia-table-scroll">
          <table className="ia-table">
            <thead><tr><th>Name</th><th>Identifier</th><th>Status</th><th>Version</th></tr></thead>
            <tbody>
              {filtered.map((row) => (
                <tr key={row.id}>
                  <td>
                    <div className="ia-entity-name">
                      <span className="ia-entity-avatar" aria-hidden="true">{row.name.trim().slice(0, 1).toUpperCase() || "•"}</span>
                      <strong>{row.name}</strong>
                    </div>
                  </td>
                  <td><code className="ia-id-chip">{row.id}</code></td>
                  <td><span className={`ia-status ${row.status === "Active" ? "ia-status-active" : "ia-status-inactive"}`}><span className="ia-status-dot" aria-hidden="true" />{row.status}</span></td>
                  <td><span className="ia-version-chip">v{row.version ?? "—"}</span></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}
