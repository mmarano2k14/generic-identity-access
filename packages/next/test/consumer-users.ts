import type {
  IdentityAdministrationContext,
  IdentityTenantAdministrationContext,
} from "@generic-identity/auth";
import type { IdentityEffectiveAdministrationContext, IdentityUserRecord } from "@generic-identity/contracts";
import {
  NextIdentityServerSession,
  createNextUserFromForm,
  updateNextUserFromForm,
  createNextPasswordCredentialFromForm,
  changeNextPasswordCredentialFromForm,
  loadNextUsersDirectory,
  loadNextUserCredentialMetadata,
  loadNextUserAccessInsight,
} from "@generic-identity/next/server";

/** Type-level cross-package consumer probe. No API calls are executed here. */
export function usersSdkConsumerProbe(
  session: NextIdentityServerSession,
  administration: IdentityAdministrationContext,
  tenant: IdentityTenantAdministrationContext,
  effective: IdentityEffectiveAdministrationContext,
  user: IdentityUserRecord,
  formData: FormData,
): void {
  void createNextUserFromForm(session, administration, formData);
  void updateNextUserFromForm(session, administration, formData);
  void createNextPasswordCredentialFromForm(session, administration, formData);
  void changeNextPasswordCredentialFromForm(session, administration, formData);
  void loadNextUsersDirectory(session, administration, effective, { tenantView: "all" });
  void loadNextUserCredentialMetadata(session, administration, user.userId);
  void loadNextUserAccessInsight(session, administration, tenant, user);
}
