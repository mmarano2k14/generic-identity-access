import Link from "next/link";
import type { IdentityEffectiveAdministrationContext } from "@identity-access/client";
import { AdminEntityAutocomplete } from "./AdminEntityAutocomplete";

type Props = {
  readonly effectiveContext: IdentityEffectiveAdministrationContext;
  readonly selectedTenantId?: string;
  readonly allTenantsSelected?: boolean;
  readonly actionPath: string;
};

/**
 * Presents requested tenant working context only. `All authorized tenants` is a collection
 * context, never a tenant identity; every mutation still targets one concrete tenant.
 */
export function AdminTenantContextSelector({ effectiveContext, selectedTenantId, allTenantsSelected = false, actionPath }: Props) {
  const allTenantsHref = `${actionPath}?tenantView=all`;

  if (effectiveContext.tenantVisibility === "scope-wide") {
    return (
      <section className="ia-card ia-lookup-card">
        <div className="ia-card-heading">
          <div>
            <p className="ia-card-kicker">Administration scope</p>
            <h2>Identity Scope Administrator</h2>
            <p>Scope-wide RBAC authority is present. Choose one tenant for a working context or aggregate authorized tenant collections. New records and row mutations still target one concrete tenant.</p>
          </div>
          <span className="ia-context-mode-badge">Scope wide</span>
        </div>
        <div className="ia-tenant-context-mode-row">
          <Link className={`ia-button ${allTenantsSelected ? "ia-button-primary" : "ia-button-secondary"}`} href={allTenantsHref}>All authorized tenants</Link>
          {allTenantsSelected || selectedTenantId ? <Link className="ia-button ia-button-secondary" href={actionPath}>Clear context</Link> : null}
        </div>
        <form className="ia-inline-form" method="get" action={actionPath}>
          <AdminEntityAutocomplete
            label="Tenant"
            name="tenantId"
            kind="tenant"
            defaultValue={selectedTenantId ?? ""}
            required
            hint="Search is server-backed. The selected tenant is re-authorized by every protected API operation."
          />
          <button className="ia-button ia-button-secondary" type="submit">Use tenant</button>
        </form>
        {allTenantsSelected ? <p className="ia-field-hint">Aggregate mode is a collection context only. New records require one explicit tenant target, and row mutations retain the row tenant ownership.</p> : null}
      </section>
    );
  }

  const memberships = effectiveContext.activeTenantMemberships;
  if (memberships.length === 0) {
    return (
      <section className="ia-card ia-lookup-card">
        <div className="ia-card-heading">
          <div>
            <p className="ia-card-kicker">Administration scope</p>
            <h2>Tenant-scoped subject</h2>
            <p>No active tenant membership is available, so no tenant-scoped administration context can be established.</p>
          </div>
          <span className="ia-context-mode-badge">Membership limited</span>
        </div>
      </section>
    );
  }

  if (memberships.length === 1) {
    const tenantId = memberships[0]!.tenantId;
    return (
      <section className="ia-card ia-lookup-card">
        <div className="ia-card-heading">
          <div>
            <p className="ia-card-kicker">Administration scope</p>
            <h2>Tenant-scoped subject</h2>
            <p>The tenant is resolved automatically from the authenticated subject&apos;s only active membership.</p>
            <code>{tenantId}</code>
          </div>
          <span className="ia-context-mode-badge">Membership limited</span>
        </div>
      </section>
    );
  }

  return (
    <section className="ia-card ia-lookup-card">
      <div className="ia-card-heading">
        <div>
          <p className="ia-card-kicker">Administration scope</p>
          <h2>Tenant-scoped subject</h2>
          <p>{allTenantsSelected ? "All active membership tenants are included in this read-only collection view." : selectedTenantId ? "An authorized membership tenant is selected." : "Choose one authorized membership tenant or aggregate all authorized tenant collections."}</p>
        </div>
        <span className="ia-context-mode-badge">Membership limited</span>
      </div>
      <div className="ia-action-row">
        <Link className={`ia-button ${allTenantsSelected ? "ia-button-primary" : "ia-button-secondary"}`} href={allTenantsHref}>All authorized tenants</Link>
        {memberships.map((membership) => (
          <Link
            className={`ia-button ${!allTenantsSelected && membership.tenantId === selectedTenantId ? "ia-button-primary" : "ia-button-secondary"}`}
            href={`${actionPath}?tenantId=${encodeURIComponent(membership.tenantId)}`}
            key={membership.membershipId}
          >
            {membership.tenantId}
          </Link>
        ))}
      </div>
      {allTenantsSelected ? <p className="ia-field-hint">Aggregate mode never becomes a tenant owner. Every mutation still requires one concrete tenant target.</p> : null}
    </section>
  );
}
