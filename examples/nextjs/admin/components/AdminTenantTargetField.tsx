"use client";

import type { IdentityEffectiveAdministrationContext } from "@identity-access/client";
import { AdminEntityAutocomplete } from "./AdminEntityAutocomplete";

type Props = {
  readonly effectiveContext: IdentityEffectiveAdministrationContext;
  readonly selectedTenantId?: string;
  readonly onTenantChange?: (tenantId: string) => void;
};

/**
 * Selects the concrete tenant ownership target for one tenant-scoped mutation.
 * The submitted tenant remains requested context only and is revalidated by the server action/API.
 */
export function AdminTenantTargetField({ effectiveContext, selectedTenantId, onTenantChange }: Props) {
  const selected = selectedTenantId?.trim().toLowerCase();
  if (selected) {
    const membership = effectiveContext.activeTenantMemberships.find((candidate) => candidate.tenantId === selected);
    return (
      <div className="ia-field">
        <span className="ia-field-label">Tenant</span>
        <input type="hidden" name="tenantId" value={selected} />
        <div className="ia-tenant-target-readonly">
          <strong>{membership ? "Authorized membership tenant" : "Selected tenant"}</strong>
          <code>{selected}</code>
        </div>
        <small className="ia-field-hint">This tenant owns the new tenant-scoped record. Server authorization is re-evaluated on submit.</small>
      </div>
    );
  }

  if (effectiveContext.tenantVisibility === "scope-wide") {
    return (
      <AdminEntityAutocomplete
        label="Tenant"
        name="tenantId"
        kind="tenant"
        required
        onSelectedIdChange={onTenantChange}
        hint="Identity Scope Administrators may target a tenant, but the submitted tenant ID is request context only and is re-authorized server-side."
      />
    );
  }

  const memberships = effectiveContext.activeTenantMemberships;
  if (memberships.length === 0) {
    return (
      <div className="ia-field">
        <span className="ia-field-label">Tenant</span>
        <div className="ia-tenant-target-readonly">
          <strong>No authorized tenant</strong>
          <span>An active tenant membership is required before a tenant-scoped record can be created.</span>
        </div>
      </div>
    );
  }

  if (memberships.length === 1) {
    const membership = memberships[0]!;
    return (
      <div className="ia-field">
        <span className="ia-field-label">Tenant</span>
        <input type="hidden" name="tenantId" value={membership.tenantId} />
        <div className="ia-tenant-target-readonly">
          <strong>Only authorized tenant</strong>
          <code>{membership.tenantId}</code>
        </div>
        <small className="ia-field-hint">The tenant is resolved from the authenticated subject&apos;s only active membership.</small>
      </div>
    );
  }

  return (
    <fieldset className="ia-tenant-target-options">
      <legend>Tenant</legend>
      <p>Select one of the authenticated subject&apos;s active tenant memberships.</p>
      <div className="ia-tenant-target-option-list">
        {memberships.map((membership) => (
          <label className="ia-tenant-target-option" key={membership.membershipId}>
            <input
              type="radio"
              name="tenantId"
              value={membership.tenantId}
              required
              onChange={(event: { currentTarget: { value: string } }) => onTenantChange?.(event.currentTarget.value)}
            />
            <span>
              <strong>Authorized tenant</strong>
              <code>{membership.tenantId}</code>
            </span>
          </label>
        ))}
      </div>
      <small className="ia-field-hint">Only tenants returned by the trusted effective administration context can be selected. A choice is required when more than one tenant is available.</small>
    </fieldset>
  );
}
