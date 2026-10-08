import type { IdentityAdministrationContext, IdentityTenantAdministrationContext } from "@generic-identity/auth";
import type { IdentityEffectiveAdministrationContext } from "@generic-identity/contracts";
import {
  NextIdentityServerSession,
  loadNextMembershipWorkspace,
  createNextTenantMembershipFromForm,
  findNextTenantMembershipCandidateFromForm,
  updateNextTenantMembershipFromForm,
  replaceNextTenantMemberGroupsFromForm,
  replaceNextTenantMemberOrganizationsFromForm,
} from "@generic-identity/next/server";

/** Type-level public SDK consumer probe; not a live API test. */
export function membershipSdkProbe(
  session: NextIdentityServerSession,
  admin: IdentityAdministrationContext,
  tenant: IdentityTenantAdministrationContext,
  effective: IdentityEffectiveAdministrationContext,
  form: FormData,
): void {
  void loadNextMembershipWorkspace(session, admin, effective);
  void createNextTenantMembershipFromForm(session, tenant, effective, form);
  void findNextTenantMembershipCandidateFromForm(session, tenant, form);
  void updateNextTenantMembershipFromForm(session, tenant, form);
  void replaceNextTenantMemberGroupsFromForm(session, tenant, form);
  void replaceNextTenantMemberOrganizationsFromForm(session, tenant, form);
}
