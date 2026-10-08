import type { ComponentProps } from "react";
import type { IdentityUserAccessInsight, IdentityTenantLinkedUserRow, IdentityUserRecord } from "@generic-identity/contracts";
import { UserForm } from "@generic-identity/react/directory";
import { PasswordCredentialForm } from "@generic-identity/react/account";
import {
  TenantLinkedUsersPage,
  UserAccessInsightPanel,
  UserDetailsPage,
  UserPasswordCredentialPanel,
} from "@generic-identity/react/pages";

/** Type-level composition probe for React 19 function form actions. */
export function usersComponentsProbe(
  user: IdentityUserRecord,
  rows: readonly IdentityTenantLinkedUserRow[],
  insight: IdentityUserAccessInsight,
  action: Exclude<ComponentProps<"form">["action"], string | undefined>,
) {
  return <>
    <UserForm user={user} formAction={action} />
    <PasswordCredentialForm userId={user.userId} formAction={action} />
    <TenantLinkedUsersPage rows={rows} />
    <UserDetailsPage user={user} showMemberships={false} />
    <UserPasswordCredentialPanel credential={null} />
    <UserAccessInsightPanel userId={user.userId} userName={user.displayName} insight={insight} />
  </>;
}
