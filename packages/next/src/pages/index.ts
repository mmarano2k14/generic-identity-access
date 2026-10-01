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
