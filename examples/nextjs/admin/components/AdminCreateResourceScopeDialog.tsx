"use client";

import { useState } from "react";
import type { IdentityEffectiveAdministrationContext } from "@identity-access/client";
import { createResourceScopeAction } from "../app/identity/actions";
import { AdminEntityAutocomplete } from "./AdminEntityAutocomplete";
import { AdminField, AdminStatusField } from "./AdminField";
import { AdminResourceScopeTypeFields } from "./AdminResourceScopeTypeFields";
import { AdminMutationDialog } from "./AdminMutationDialog";
import { AdminTenantTargetField } from "./AdminTenantTargetField";

type Props = {
  readonly effectiveContext: IdentityEffectiveAdministrationContext;
  readonly selectedTenantId?: string;
};

/** Creates one resource scope and constrains optional parent lookup to the selected owning tenant. */
export function AdminCreateResourceScopeDialog({ effectiveContext, selectedTenantId }: Props) {
  const canTargetTenant = effectiveContext.tenantVisibility === "scope-wide" || effectiveContext.activeTenantMemberships.length > 0;
  const [tenantId, setTenantId] = useState(resolveInitialTenantId(effectiveContext, selectedTenantId));
  if (!canTargetTenant) return null;

  return (
    <AdminMutationDialog
      title="Create resource scope"
      description="Register an application-defined resource in one concrete tenant hierarchy. Parent lookup is enabled only after the owning tenant is resolved."
      triggerLabel="Create resource scope"
      submitLabel="Create resource scope"
      action={createResourceScopeAction}
    >
      <AdminTenantTargetField effectiveContext={effectiveContext} selectedTenantId={selectedTenantId} onTenantChange={setTenantId} />
      <AdminResourceScopeTypeFields />
      <AdminField label="External resource ID" name="externalResourceId" required maxLength={256} />
      <AdminField label="Display name" name="displayName" required maxLength={200} />
      <div key={tenantId || "no-tenant"}>
        <AdminEntityAutocomplete
          label="Parent resource scope"
          name="parentResourceScopeId"
          kind="resource-scope"
          tenantId={tenantId || undefined}
          disabled={!tenantId}
          emptyLabel="Root scope"
          hint={tenantId
            ? "Optional. Parent search is constrained to the selected owning tenant."
            : "Choose the owning tenant first. Parent lookup never searches across tenant boundaries."}
        />
      </div>
      <AdminStatusField name="status" />
    </AdminMutationDialog>
  );
}

function resolveInitialTenantId(effectiveContext: IdentityEffectiveAdministrationContext, selectedTenantId?: string): string {
  const selected = selectedTenantId?.trim().toLowerCase();
  if (selected) return selected;
  if (effectiveContext.tenantVisibility !== "membership-limited") return "";
  if (effectiveContext.activeTenantMemberships.length !== 1) return "";
  return effectiveContext.activeTenantMemberships[0]?.tenantId ?? "";
}
