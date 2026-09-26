import Link from "next/link";
import { AdminField, AdminSelectField } from "../../../components/AdminField";
import { AdminEntityAutocomplete } from "../../../components/AdminEntityAutocomplete";
import { AdminIcon } from "../../../components/AdminIcon";
import { AdminMetricCard } from "../../../components/AdminMetricCard";
import { AdminMutationDialog } from "../../../components/AdminMutationDialog";
import { AdminPageHeader } from "../../../components/AdminPageHeader";
import { AdminSecurityBanner } from "../../../components/AdminSecurityBanner";
import { AdminSessionSecurityTimeline } from "../../../components/AdminSessionSecurityTimeline";
import { IdentityAccessAdminRequest } from "../../../server/IdentityAccessAdminRequest";
import { IdentityAccessAdminSessionPresentation } from "../../../server/IdentityAccessAdminSessionPresentation";
import { IdentityAccessAdminSessionQuery, type IdentityAccessAdminSessionSearchParams } from "../../../server/IdentityAccessAdminSessionQuery";
import { IdentityAccessAdminSessionService } from "../../../server/IdentityAccessAdminSessionService";
import { revokeClientSessionsAction, revokeUserSessionsAction } from "./actions";

export default async function SessionsPage({ searchParams }: { readonly searchParams: Promise<IdentityAccessAdminSessionSearchParams> }) {
  const request = await IdentityAccessAdminRequest.fromCurrentRequest();
  const query = IdentityAccessAdminSessionQuery.fromSearchParams(await searchParams);
  const activity = await new IdentityAccessAdminSessionService(request).listActivity(query);
  const summary = IdentityAccessAdminSessionPresentation.summary(activity.records);

  return (
    <section className="ia-page">
      <AdminPageHeader
        eyebrow="Security"
        badge="Session operations"
        title="Sessions"
        description="Investigate bounded session-security evidence and contain active sessions through the existing server-authorized revocation contracts."
      />

      <AdminSecurityBanner
        title="This is not an inferred active-session inventory."
        description="The current administration API does not expose a session-list contract. This workspace therefore shows secret-safe security evidence and real containment operations without guessing whether an unseen session is active, expired, or revoked."
      />

      <section className="ia-card ia-session-filter-card">
        <div className="ia-card-heading">
          <div>
            <p className="ia-card-kicker">Investigation context</p>
            <h2>Filter session security activity</h2>
            <p>User and outcome filters are applied by the existing security-audit API. Client filtering is applied server-side in this host after authorized audit records are returned, so it remains bounded by those source windows.</p>
          </div>
        </div>
        <form className="ia-session-filter-grid" method="get">
          <AdminEntityAutocomplete label="User" name="userId" kind="user" defaultValue={query.userId ?? ""} emptyLabel="All users" hint="Optional. Type at least 3 characters of the user display name, or enter the full ID." />
          <AdminField label="Client ID" name="clientId" defaultValue={query.clientId ?? ""} autoComplete="off" maxLength={128} />
          <AdminSelectField label="Outcome" name="outcome" defaultValue={query.outcome ?? ""}>
            <option value="">All outcomes</option>
            <option value="Succeeded">Succeeded</option>
            <option value="Denied">Denied</option>
            <option value="Failed">Failed</option>
          </AdminSelectField>
          <AdminSelectField label="Window" name="limit" defaultValue={String(query.limit)}>
            <option value="25">25 events</option>
            <option value="50">50 events</option>
            <option value="100">100 events</option>
          </AdminSelectField>
          <div className="ia-session-filter-actions">
            <button className="ia-button ia-button-primary" type="submit"><AdminIcon name="search" />Apply filters</button>
            {query.hasFilters ? <Link className="ia-button ia-button-secondary" href="/identity/sessions">Clear</Link> : null}
          </div>
        </form>
        {query.validationMessage ? <p className="ia-form-error" role="alert">{query.validationMessage}</p> : null}
        {query.userId ? (
          <div className="ia-session-context-links">
            <Link className="ia-button ia-button-secondary ia-button-compact" href={`/identity/users?userId=${encodeURIComponent(query.userId)}`}><AdminIcon name="users" />User</Link>
            <Link className="ia-button ia-button-secondary ia-button-compact" href={`/identity/mfa?userId=${encodeURIComponent(query.userId)}`}><AdminIcon name="mfa" />MFA</Link>
            <Link className="ia-button ia-button-secondary ia-button-compact" href={`/identity/security-audit?userId=${encodeURIComponent(query.userId)}`}><AdminIcon name="audit" />Security audit</Link>
          </div>
        ) : null}
      </section>

      {activity.evidenceState === "forbidden" ? (
        <AdminSecurityBanner
          title="Session audit evidence is not available to this administrator."
          description="The current session page remains usable for separately authorized containment operations, but security-audit metadata requires its own read capability and is not inferred or bypassed here."
        />
      ) : activity.evidenceState === "unavailable" ? (
        <AdminSecurityBanner
          title={activity.failure?.title ?? "Session audit evidence is temporarily unavailable."}
          description={activity.failure?.message ?? "The bounded evidence read failed. No session state is inferred from the missing response, and containment remains a separately authorized operation."}
        />
      ) : (
        <>
          <div className="ia-metric-grid">
            <AdminMetricCard icon="sessions" label="Visible events" value={String(summary.total)} description={`Bounded to the newest ${query.limit} matching session-security events.`} tone="accent" />
            <AdminMetricCard icon="key" label="Session issuance" value={String(summary.issued)} description="Successful password logins that recorded a new session reference." tone="success" />
            <AdminMetricCard icon="lock" label="Revocation activity" value={String(summary.revocationEvents)} description="Exact, user-wide, client-wide, or refresh-family revocation evidence." />
            <AdminMetricCard icon="shield" label="Continuity alerts" value={String(summary.continuityAlerts)} description="Refresh-token reuse detections visible in the current window." tone="warning" />
          </div>
          <AdminSessionSecurityTimeline records={activity.records} />
        </>
      )}

      <section className="ia-session-operations">
        <div className="ia-section-heading">
          <div>
            <p className="ia-card-kicker">Containment</p>
            <h2>Server-confirmed revocation operations</h2>
            <p>These actions use only the revocation contracts already exposed by the API. No session-delete or refresh-token administration endpoint is synthesized by this host.</p>
          </div>
        </div>
        <div className="ia-security-action-grid">
          <section className="ia-security-action-card">
            <span className="ia-security-action-icon"><AdminIcon name="users" /></span>
            <div className="ia-security-action-copy">
              <p className="ia-card-kicker">User containment</p>
              <h2>Revoke by user</h2>
              <p>Invalidate every active local session owned by one user without changing the identity record itself.</p>
            </div>
            <AdminMutationDialog title="Revoke user sessions" description="This affects sessions linked to the selected user. Type REVOKE to confirm." triggerLabel="Revoke user sessions" submitLabel="Revoke sessions" action={revokeUserSessionsAction} dangerous>
              <AdminEntityAutocomplete label="User" name="userId" kind="user" defaultValue={query.userId ?? ""} required hint="Type at least 3 characters of the user display name, or enter the full ID." />
              <AdminField label="Confirmation" name="confirmation" required autoComplete="off" placeholder="REVOKE" />
            </AdminMutationDialog>
          </section>
          <section className="ia-security-action-card ia-security-action-card-danger">
            <span className="ia-security-action-icon"><AdminIcon name="lock" /></span>
            <div className="ia-security-action-copy">
              <p className="ia-card-kicker">Client containment</p>
              <h2>Revoke by client</h2>
              <p>Invalidate active sessions issued to one registered authentication client. This may sign out many users at once.</p>
            </div>
            <AdminMutationDialog title="Revoke client sessions" description="This can sign out many users at once. Type REVOKE to confirm." triggerLabel="Revoke client sessions" submitLabel="Revoke sessions" action={revokeClientSessionsAction} dangerous>
              <AdminField label="Client ID" name="clientId" defaultValue={query.clientId ?? ""} required autoComplete="off" />
              <AdminField label="Confirmation" name="confirmation" required autoComplete="off" placeholder="REVOKE" />
            </AdminMutationDialog>
          </section>
        </div>
      </section>
    </section>
  );
}
