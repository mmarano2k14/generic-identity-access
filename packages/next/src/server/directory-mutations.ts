import "server-only";

import type { IdentityAdministrationContext } from "@generic-identity/auth";
import type { IdentityTenantRecord } from "@generic-identity/contracts";
import {
  administrationLifecycleStatus,
  positiveAdministrationInteger,
  requiredAdministrationText,
} from "./administration-form";
import { NextIdentityServerSession } from "./session";

/**
 * Creates a tenant from a trusted server-side form submission.
 *
 * Parsing and bounds intentionally mirror the proven Identity Admin behavior so
 * consuming Next.js applications do not reimplement Identity mutation rules.
 */
export async function createNextTenantFromForm(
  session: NextIdentityServerSession,
  context: IdentityAdministrationContext,
  formData: FormData,
): Promise<IdentityTenantRecord> {
  return session.client.directory.tenants.create(context, {
    displayName: requiredAdministrationText(formData, "displayName", 200),
    status: administrationLifecycleStatus(formData, "status"),
  });
}

/**
 * Updates a tenant from a trusted server-side form submission.
 *
 * The stable tenant id and record version are submitted by the reusable tenant
 * form. The version is forwarded unchanged so optimistic-concurrency conflicts
 * remain authoritative server decisions.
 */
export async function updateNextTenantFromForm(
  session: NextIdentityServerSession,
  context: IdentityAdministrationContext,
  formData: FormData,
): Promise<IdentityTenantRecord> {
  return session.client.directory.tenants.update(
    context,
    requiredAdministrationText(formData, "tenantId", 64),
    {
      displayName: requiredAdministrationText(formData, "displayName", 200),
      status: administrationLifecycleStatus(formData, "status"),
      expectedVersion: positiveAdministrationInteger(formData, "expectedVersion"),
    },
  );
}
