import { IdentityEntityAutocomplete } from "../components/IdentityEntityAutocomplete";
import { IdentityPanel } from "../components/IdentityPanel";

export interface SecurityAuditFilterValues {
  readonly userId?: string;
  readonly tenantId?: string;
  readonly outcome?: "Succeeded" | "Denied" | "Failed";
  readonly correlationId?: string;
  readonly limit: 25 | 50 | 100 | 200;
}

export interface SecurityAuditFilterFormProps {
  readonly actionPath: string;
  readonly values: SecurityAuditFilterValues;
  readonly validationMessage?: string;
  readonly allowUserLookup?: boolean;
  readonly allowTenantLookup?: boolean;
}

/** Reusable bounded GET filters for application-scoped Security Audit evidence. */
export function SecurityAuditFilterForm({
  actionPath,
  values,
  validationMessage,
  allowUserLookup = true,
  allowTenantLookup = true,
}: SecurityAuditFilterFormProps) {
  return (
    <IdentityPanel
      title="Filter security evidence"
      description="Filters are exact and server-applied. They never widen authorization or change database placement."
    >
      <form className="gi-form gi-audit-filter-grid" method="get" action={actionPath}>
        {allowUserLookup ? (
          <IdentityEntityAutocomplete
            label="User"
            name="userId"
            kind="user"
            defaultValue={values.userId ?? ""}
            emptyLabel="All users"
            hint="Optional. Type at least 3 characters of the display name, or enter the full stable ID."
          />
        ) : null}

        {allowTenantLookup ? (
          <IdentityEntityAutocomplete
            label="Tenant"
            name="tenantId"
            kind="tenant"
            defaultValue={values.tenantId ?? ""}
            emptyLabel="All tenants"
            hint="Optional. Type at least 3 characters of the display name, or enter the full stable ID."
          />
        ) : null}

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
          <span className="gi-field-label">Correlation ID</span>
          <input
            className="gi-input"
            name="correlationId"
            defaultValue={values.correlationId ?? ""}
            autoComplete="off"
            maxLength={64}
          />
        </label>

        <label className="gi-field">
          <span className="gi-field-label">Window</span>
          <select className="gi-input" name="limit" defaultValue={String(values.limit)}>
            <option value="25">25 events</option>
            <option value="50">50 events</option>
            <option value="100">100 events</option>
            <option value="200">200 events</option>
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
