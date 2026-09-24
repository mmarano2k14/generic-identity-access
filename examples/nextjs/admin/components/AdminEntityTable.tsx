"use client";

import { useId, useMemo, useState, type ReactNode } from "react";
import { AdminEmptyState } from "./AdminEmptyState";
import { AdminIcon } from "./AdminIcon";

export interface AdminEntityTableRow {
  readonly id: string;
  readonly name: string;
  readonly status: string;
  readonly version?: number;
  readonly actions?: ReactNode;
}

export interface AdminEntityTableProps {
  readonly rows: readonly AdminEntityTableRow[];
  readonly title?: string;
  readonly description?: string;
  readonly entityLabel?: string;
}

type AdminEntityTableSort = "default" | "name" | "status" | "version";

/** Client Component limited to presentation filtering/sorting; authorization and data loading stay server-side. */
export function AdminEntityTable({ rows, title = "Records", description = "Browse the current administration view.", entityLabel = "records" }: AdminEntityTableProps) {
  const tableId = useId();
  const [query, setQuery] = useState("");
  const [status, setStatus] = useState("all");
  const [sort, setSort] = useState<AdminEntityTableSort>("default");
  const hasActions = rows.some((row) => row.actions !== undefined);
  const statuses = useMemo(() => [...new Set(rows.map((row) => row.status))].sort((left, right) => left.localeCompare(right)), [rows]);
  const filtered = useMemo(() => {
    const normalized = query.trim().toLowerCase();
    const matches = rows.filter((row) => {
      const matchesQuery = !normalized || `${row.id} ${row.name} ${row.status}`.toLowerCase().includes(normalized);
      const matchesStatus = status === "all" || row.status === status;
      return matchesQuery && matchesStatus;
    });

    if (sort === "name") return [...matches].sort((left, right) => left.name.localeCompare(right.name));
    if (sort === "status") return [...matches].sort((left, right) => left.status.localeCompare(right.status) || left.name.localeCompare(right.name));
    if (sort === "version") return [...matches].sort((left, right) => (right.version ?? -1) - (left.version ?? -1) || left.name.localeCompare(right.name));
    return matches;
  }, [query, rows, sort, status]);
  const hasActiveFilters = query.trim().length > 0 || status !== "all" || sort !== "default";

  const clearFilters = () => {
    setQuery("");
    setStatus("all");
    setSort("default");
  };

  return (
    <section className="ia-table-card" aria-labelledby={`${tableId}-title`}>
      <div className="ia-table-heading">
        <div>
          <p className="ia-card-kicker">Collection</p>
          <h2 id={`${tableId}-title`}>{title}</h2>
          <p>{description}</p>
        </div>
        <span className="ia-count-badge">{rows.length} {entityLabel}</span>
      </div>
      <div className="ia-table-toolbar">
        <div className="ia-table-toolbar-controls">
          <label className="ia-search-field">
            <span className="ia-search-icon"><AdminIcon name="search" /></span>
            <span className="ia-sr-only">Filter this page</span>
            <input
              className="ia-input ia-search-input"
              value={query}
              onChange={(event) => setQuery(event.target.value)}
              placeholder={`Search ${entityLabel}`}
              aria-controls={tableId}
            />
          </label>
          <label className="ia-table-filter-field">
            <span className="ia-sr-only">Filter by status</span>
            <span className="ia-select-wrap">
              <select className="ia-select ia-table-filter" value={status} onChange={(event) => setStatus(event.target.value)} aria-controls={tableId}>
                <option value="all">All statuses</option>
                {statuses.map((item) => <option key={item} value={item}>{item}</option>)}
              </select>
            </span>
          </label>
          <label className="ia-table-filter-field">
            <span className="ia-sr-only">Sort records</span>
            <span className="ia-select-wrap">
              <select className="ia-select ia-table-filter" value={sort} onChange={(event) => setSort(event.target.value as AdminEntityTableSort)} aria-controls={tableId}>
                <option value="default">Default order</option>
                <option value="name">Name A-Z</option>
                <option value="status">Status</option>
                <option value="version">Newest version</option>
              </select>
            </span>
          </label>
          {hasActiveFilters ? <button className="ia-button ia-button-secondary ia-button-compact ia-table-clear" type="button" onClick={clearFilters}>Clear</button> : null}
        </div>
        <span className="ia-result-count" aria-live="polite">Showing <strong>{filtered.length}</strong> of {rows.length}</span>
      </div>
      {filtered.length === 0 ? (
        <AdminEmptyState
          title={rows.length === 0 ? "Nothing here yet" : "No matching records"}
          description={rows.length === 0 ? "Create the first record when you are ready." : "Clear the filters or try a different name, identifier, or status."}
        />
      ) : (
        <div className="ia-table-scroll">
          <table className="ia-table" id={tableId}>
            <caption className="ia-sr-only">{title}. {filtered.length} visible {entityLabel}.</caption>
            <thead><tr><th scope="col">Name</th><th scope="col">Identifier</th><th scope="col">Status</th><th scope="col">Version</th>{hasActions ? <th scope="col" className="ia-actions-heading">Actions</th> : null}</tr></thead>
            <tbody>
              {filtered.map((row) => (
                <tr key={row.id}>
                  <td data-label="Name" className="ia-table-cell-name">
                    <div className="ia-entity-name">
                      <span className="ia-entity-avatar" aria-hidden="true">{row.name.trim().slice(0, 1).toUpperCase() || "•"}</span>
                      <strong>{row.name}</strong>
                    </div>
                  </td>
                  <td data-label="Identifier"><code className="ia-id-chip" title={row.id}>{row.id}</code></td>
                  <td data-label="Status"><span className={`ia-status ${row.status === "Active" ? "ia-status-active" : row.status === "Inactive" || row.status === "Revoked" ? "ia-status-inactive" : "ia-status-neutral"}`}><span className="ia-status-dot" aria-hidden="true" />{row.status}</span></td>
                  <td data-label="Version"><span className="ia-version-chip">v{row.version ?? "—"}</span></td>
                  {hasActions ? <td data-label="Actions" className="ia-table-cell-actions"><div className="ia-row-actions">{row.actions}</div></td> : null}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}
