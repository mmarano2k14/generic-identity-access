import { IdentityEntityAutocomplete } from "../components/IdentityEntityAutocomplete";
import { IdentityPanel } from "../components/IdentityPanel";

export interface SessionSecurityFilterValues {
  readonly userId?: string;
  readonly clientId?: string;
  readonly outcome?: "Succeeded" | "Denied" | "Failed";
  readonly limit: 25 | 50 | 100;
}

export interface SessionSecurityFilterFormProps {
  readonly actionPath: string;
  readonly values: SessionSecurityFilterValues;
  readonly validationMessage?: string;
  readonly allowUserLookup?: boolean;
}

/** Read-only bounded filters for session-security evidence. */
export function SessionSecurityFilterForm({
  actionPath,
  values,
  validationMessage,
  allowUserLookup = true,
}: SessionSecurityFilterFormProps) {
  return (
    <IdentityPanel
      title="Filter session security activity"
      description="User and outcome filters are server-applied. Client filtering remains bounded by the authorized audit windows."
    >
      <form className="gi-form gi-audit-filter-grid" method="get" action={actionPath}>
        {allowUserLookup ? (
          <IdentityEntityAutocomplete
            label="User"
            name="userId"
            kind="user"
            defaultValue={values.userId ?? ""}
            emptyLabel="All users"
            hint="Optional. Type at least 3 characters of the user display name, or enter the full stable ID."
          />
        ) : null}

        <label className="gi-field">
          <span className="gi-field-label">Client ID</span>
          <input
            className="gi-input"
            name="clientId"
            defaultValue={values.clientId ?? ""}
            autoComplete="off"
            maxLength={128}
          />
        </label>

        <label className="gi-field">
          <span className="gi-field-label">Outcome</span>
          <select className="gi-input" name="outcome" defaultValue={values.outcome ?? ""}>
            <option value="">All outcomes</option>
            <option value="Succeeded">Succeeded</option>
            <option value="Denied">Denied</option>
            <option value="Failed">Failed</option>
          </select>
        </label>

        <label className="gi-field">
          <span className="gi-field-label">Window</span>
          <select className="gi-input" name="limit" defaultValue={String(values.limit)}>
            <option value="25">25 events</option>
            <option value="50">50 events</option>
            <option value="100">100 events</option>
          </select>
        </label>

        <div className="gi-audit-filter-actions">
          <button className="gi-button gi-button-primary" type="submit">Apply filters</button>
          <a className="gi-button gi-button-secondary" href={actionPath}>Clear</a>
        </div>
      </form>

      {validationMessage ? <p className="gi-form-error" role="alert">{validationMessage}</p> : null}
    </IdentityPanel>
  );
}
