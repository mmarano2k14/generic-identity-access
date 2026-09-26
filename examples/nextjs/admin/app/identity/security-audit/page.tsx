import Link from "next/link";
import { AdminField, AdminSelectField } from "../../../components/AdminField";
import { AdminEntityAutocomplete } from "../../../components/AdminEntityAutocomplete";
import { AdminIcon } from "../../../components/AdminIcon";
import { AdminMetricCard } from "../../../components/AdminMetricCard";
import { AdminPageHeader } from "../../../components/AdminPageHeader";
import { AdminSecurityAuditTimeline } from "../../../components/AdminSecurityAuditTimeline";
import { AdminSecurityBanner } from "../../../components/AdminSecurityBanner";
import { IdentityAccessAdminRequest } from "../../../server/IdentityAccessAdminRequest";
import { IdentityAccessAdminSecurityAuditPresentation } from "../../../server/IdentityAccessAdminSecurityAuditPresentation";
import { IdentityAccessAdminSecurityAuditQuery, type IdentityAccessAdminSecurityAuditSearchParams } from "../../../server/IdentityAccessAdminSecurityAuditQuery";
import { IdentityAccessAdminSecurityAuditService } from "../../../server/IdentityAccessAdminSecurityAuditService";

export default async function SecurityAuditPage({ searchParams }: { readonly searchParams: Promise<IdentityAccessAdminSecurityAuditSearchParams> }) {
  const request = await IdentityAccessAdminRequest.fromCurrentRequest();
  const query = IdentityAccessAdminSecurityAuditQuery.fromSearchParams(await searchParams);
  const records = await new IdentityAccessAdminSecurityAuditService(request).list(query);
  const summary = IdentityAccessAdminSecurityAuditPresentation.summary(records);

  return (
    <section className="ia-page">
      <AdminPageHeader
        eyebrow="Security"
        badge="Read-only evidence"
        title="Security audit"
        description="Inspect bounded, newest-first security events recorded by the current Identity Access application without exposing credentials, tokens, secrets, or arbitrary payloads."
      />

      <AdminSecurityBanner
        title="Audit evidence is application-scoped and read-only."
        description="The API resolves the trusted database route server-side, filters by the current identity scope and application, and exposes only the categorical metadata already persisted in security_events."
      />

      <section className="ia-card ia-audit-filter-card">
        <div className="ia-card-heading">
          <div><p className="ia-card-kicker">Query window</p><h2>Filter security evidence</h2><p>Filters are exact and server-applied. Empty fields do not widen authorization or change database placement.</p></div>
        </div>
        <form className="ia-audit-filter-grid" method="get">
          <AdminEntityAutocomplete label="User" name="userId" kind="user" defaultValue={query.userId ?? ""} emptyLabel="All users" hint="Optional. Type at least 3 characters of the user display name, or enter the full ID." />
          <AdminEntityAutocomplete label="Tenant" name="tenantId" kind="tenant" defaultValue={query.tenantId ?? ""} emptyLabel="All tenants" hint="Optional. Type at least 3 characters of the tenant display name, or enter the full ID." />
          <AdminSelectField label="Outcome" name="outcome" defaultValue={query.outcome ?? ""}>
            <option value="">All outcomes</option>
            <option value="Succeeded">Succeeded</option>
            <option value="Denied">Denied</option>
            <option value="Failed">Failed</option>
          </AdminSelectField>
          <AdminField label="Correlation ID" name="correlationId" defaultValue={query.correlationId ?? ""} autoComplete="off" maxLength={64} />
          <AdminSelectField label="Window" name="limit" defaultValue={String(query.limit)}>
            <option value="25">25 events</option>
            <option value="50">50 events</option>
            <option value="100">100 events</option>
            <option value="200">200 events</option>
          </AdminSelectField>
          <div className="ia-audit-filter-actions">
            <button className="ia-button ia-button-primary" type="submit"><AdminIcon name="search" />Apply filters</button>
            {query.hasFilters ? <Link className="ia-button ia-button-secondary" href="/identity/security-audit">Clear</Link> : null}
          </div>
        </form>
        {query.validationMessage ? <p className="ia-form-error" role="alert">{query.validationMessage}</p> : null}
      </section>

      <div className="ia-metric-grid">
        <AdminMetricCard icon="audit" label="Visible events" value={String(summary.total)} description={`Bounded to the newest ${query.limit} matching events.`} tone="accent" />
        <AdminMetricCard icon="check" label="Succeeded" value={String(summary.succeeded)} description="Recorded operations with a successful outcome." tone="success" />
        <AdminMetricCard icon="shield" label="Denied" value={String(summary.denied)} description="Rejected security operations in the current window." tone="warning" />
        <AdminMetricCard icon="sessions" label="Failed" value={String(summary.failed)} description="Technical or non-authorization failures in the current window." />
      </div>

      <AdminSecurityAuditTimeline records={records} />
    </section>
  );
}
