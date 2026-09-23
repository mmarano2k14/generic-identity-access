"use client";

import { useMemo, useState } from "react";
import { AdminEmptyState } from "./AdminEmptyState";

export interface AdminEntityTableRow {
  readonly id: string;
  readonly name: string;
  readonly status: string;
  readonly version?: number;
}

/** Client Component limited to presentation filtering; authorization and data loading stay server-side. */
export function AdminEntityTable({ rows }: { readonly rows: readonly AdminEntityTableRow[] }) {
  const [query, setQuery] = useState("");
  const filtered = useMemo(() => {
    const normalized = query.trim().toLowerCase();
    if (!normalized) return rows;
    return rows.filter((row) => `${row.id} ${row.name} ${row.status}`.toLowerCase().includes(normalized));
  }, [query, rows]);

  return (
    <section className="ia-table-card">
      <div className="ia-table-toolbar">
        <label className="ia-search-field">
          <span className="ia-sr-only">Filter this page</span>
          <input className="ia-input" value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Search this page" />
        </label>
        <span className="ia-result-count">{filtered.length} of {rows.length}</span>
      </div>
      {filtered.length === 0 ? (
        <AdminEmptyState
          title={rows.length === 0 ? "Nothing here yet" : "No matching records"}
          description={rows.length === 0 ? "Create the first record when you are ready." : "Try a different search term."}
        />
      ) : (
        <div className="ia-table-scroll">
          <table className="ia-table">
            <thead><tr><th>Name</th><th>ID</th><th>Status</th><th>Version</th></tr></thead>
            <tbody>
              {filtered.map((row) => (
                <tr key={row.id}>
                  <td><strong>{row.name}</strong></td>
                  <td><code>{row.id}</code></td>
                  <td><span className={`ia-status ${row.status === "Active" ? "ia-status-active" : "ia-status-inactive"}`}>{row.status}</span></td>
                  <td>{row.version ?? "-"}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}
