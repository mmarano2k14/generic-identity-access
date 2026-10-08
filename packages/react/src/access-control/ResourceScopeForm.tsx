"use client";

import { useMemo, useState, type ComponentProps, type ReactNode } from "react";
import type { IdentityResourceScopeRecord } from "@generic-identity/contracts/access-control";
import { IdentityButton, IdentityEntityAutocomplete, IdentityInput } from "../components/index";
import { ResourceScopeTypeFields } from "./ResourceScopeTypeFields";

export interface ResourceScopeTenantOption {
  readonly tenantId: string;
  readonly displayName: string;
}

export interface ResourceScopeFormProps {
  readonly resourceScope?: IdentityResourceScopeRecord | null;
  readonly tenantId?: string;
  readonly scopeWide?: boolean;
  readonly tenantOptions?: readonly ResourceScopeTenantOption[];
  readonly canReadScopeTypes?: boolean;
  readonly formAction?: ComponentProps<"form">["action"];
  readonly error?: ReactNode;
}

/**
 * Shared ResourceScope create/edit form matching the Identity Admin GOLDEN:
 * registered scope types and server-backed parent lookup, never raw relation IDs.
 */
export function ResourceScopeForm({
  resourceScope = null,
  tenantId,
  scopeWide = false,
  tenantOptions = [],
  canReadScopeTypes = true,
  formAction,
  error,
}: ResourceScopeFormProps) {
  const initialTenantId = tenantId ?? (!scopeWide && tenantOptions.length === 1 ? tenantOptions[0]?.tenantId : undefined) ?? "";
  const [selectedTenantId, setSelectedTenantId] = useState(initialTenantId);

  const selectedTenant = useMemo(
    () => tenantOptions.find((candidate) => candidate.tenantId === selectedTenantId),
    [selectedTenantId, tenantOptions],
  );
  const fixedTenant = resourceScope !== null || tenantId !== undefined;

  return (
    <form
      className="gi-form"
      method={typeof formAction === "function" ? undefined : "post"}
      action={formAction}
      data-gi-component="resource-scope-form"
    >
      {fixedTenant ? (
        <>
          <input type="hidden" name="tenantId" value={selectedTenantId} />
          <div className="gi-field">
            <span className="gi-field-label">Tenant</span>
            <strong>{selectedTenant?.displayName ?? selectedTenantId}</strong>
            <code>{selectedTenantId}</code>
            <small className="gi-field-hint">The server re-authorizes this tenant target on submit.</small>
          </div>
        </>
      ) : scopeWide ? (
        <IdentityEntityAutocomplete
          label="Tenant"
          name="tenantId"
          kind="tenant"
          required
          onSelectedIdChange={setSelectedTenantId}
          hint="Choose the concrete tenant that owns this ResourceScope."
        />
      ) : tenantOptions.length > 0 ? (
        <label className="gi-field">
          <span className="gi-field-label">Tenant</span>
          <select
            className="gi-input"
            name="tenantId"
            value={selectedTenantId}
            onChange={(event) => setSelectedTenantId(event.currentTarget.value)}
            required
          >
            <option value="" disabled>Select an authorized tenant</option>
            {tenantOptions.map((tenant) => (
              <option key={tenant.tenantId} value={tenant.tenantId}>{tenant.displayName}</option>
            ))}
          </select>
        </label>
      ) : (
        <div className="gi-form-error" role="alert">No authorized tenant is available for this ResourceScope.</div>
      )}

      {resourceScope ? <input type="hidden" name="resourceScopeId" value={resourceScope.resourceScopeId} /> : null}

      <ResourceScopeTypeFields
        initialModelVersion={resourceScope?.modelVersion}
        initialScopeType={resourceScope?.scopeType}
        disabled={!canReadScopeTypes}
      />

      <label className="gi-field">
        <span className="gi-field-label">External resource ID</span>
        <IdentityInput name="externalResourceId" defaultValue={resourceScope?.externalResourceId ?? ""} required maxLength={256} />
      </label>

      <label className="gi-field">
        <span className="gi-field-label">Display name</span>
        <IdentityInput name="displayName" defaultValue={resourceScope?.displayName ?? ""} required maxLength={200} />
      </label>

      <div key={selectedTenantId || "no-tenant"}>
        <IdentityEntityAutocomplete
          label="Parent resource scope"
          name="parentResourceScopeId"
          kind="resource-scope"
          tenantId={selectedTenantId || undefined}
          defaultValue={resourceScope?.parentResourceScopeId ?? ""}
          disabled={!selectedTenantId}
          excludeIds={resourceScope ? [resourceScope.resourceScopeId] : []}
          emptyLabel="Root scope"
          includeInactive
          hint={selectedTenantId
            ? "Optional. Search is constrained to the concrete owning tenant."
            : "Choose the owning tenant before searching for a parent scope."}
        />
      </div>

      <label className="gi-field">
        <span className="gi-field-label">Status</span>
        <select className="gi-input" name="status" defaultValue={String(resourceScope?.status ?? 1)}>
          <option value="1">Active</option>
          <option value="2">Inactive</option>
        </select>
      </label>

      {resourceScope ? <input type="hidden" name="expectedVersion" value={resourceScope.version} /> : null}
      {error ? <div className="gi-form-error" role="alert">{error}</div> : null}

      <IdentityButton
        variant="primary"
        type="submit"
        disabled={!selectedTenantId || !canReadScopeTypes}
      >
        {resourceScope ? "Save resource scope" : "Create resource scope"}
      </IdentityButton>
    </form>
  );
}
