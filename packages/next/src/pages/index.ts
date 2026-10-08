/**
 * Shared page exports for Next.js route adapters.
 *
 * Routes remain owned by the consuming application. These exports intentionally
 * contain no route names or navigation policy.
 */
export {
  AccountPage,
  GroupDetailsPage,
  GroupsPage,
  MfaPage,
  PoliciesPage,
  PolicyDetailsPage,
  ProfilePage,
  RecoveryPage,
  SecurityPage,
  SessionsPage,
  SignInPage,
  UserDetailsPage,
  UsersPage,
} from "@generic-identity/react/pages";

export type {
  AccountPageProps,
  GroupDetailsPageProps,
  GroupsPageProps,
  MfaPageProps,
  PoliciesPageProps,
  PolicyDetailsPageProps,
  ProfilePageProps,
  RecoveryPageProps,
  SecurityPageProps,
  SessionsPageProps,
  SignInPageProps,
  UserDetailsPageProps,
  UsersPageProps,
} from "@generic-identity/react/pages";

export {
  AuthenticationStepUpPage,
  MembershipCandidatePanel,
  MembershipsPage,
  PasswordPage,
  TenantDetailsPage,
  TenantsPage,
} from "@generic-identity/react/pages";

export type {
  AuthenticationStepUpPageProps,
  MembershipCandidatePanelProps,
  MembershipsPageProps,
  PasswordPageProps,
  TenantDetailsPageProps,
  TenantsPageProps,
} from "@generic-identity/react/pages";
export {
  OrganizationDetailsPage,
  OrganizationMembershipsPage,
  OrganizationsPage,
  OrganizationTreePage,
} from "@generic-identity/react/organizations";

export type {
  OrganizationDetailsPageProps,
  OrganizationMembershipsPageProps,
  OrganizationsPageProps,
  OrganizationTreePageProps,
} from "@generic-identity/react/organizations";

export {
  DelegatedAuthorityPage,
  GroupAccessPage,
  ManagedPolicyBindingsPage,
  ResourceScopesPage,
  ResourceScopeDetailsPage,
} from "@generic-identity/react/access-control";

export type {
  DelegatedAuthorityPageProps,
  GroupAccessPageProps,
  ManagedPolicyBindingsPageProps,
  ResourceScopesPageProps,
  ResourceScopeDetailsPageProps,
} from "@generic-identity/react/access-control";


export { TenantLinkedUsersPage, UserAccessInsightPanel, UserPasswordCredentialPanel } from "@generic-identity/react/pages";
export type { TenantLinkedUsersPageProps, UserAccessInsightPanelProps, UserPasswordCredentialPanelProps } from "@generic-identity/react/pages";
