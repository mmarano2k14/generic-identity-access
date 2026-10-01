import type { GenericIdentityClient } from "@generic-identity/auth";
import {
  GroupsPage,
  NextIdentityProvider,
  defineIdentityRoutes,
  identityRoute,
} from "@generic-identity/next";

const routes = defineIdentityRoutes({
  signIn: "/login",
  recovery: "/recovery",
  account: "/account",
  users: "/identity/users",
  groups: "/identity/groups",
  policies: "/identity/policies",
  sessions: "/identity/sessions",
  mfa: "/identity/mfa",
  security: "/identity/security",
});

export function NextConsumerProbe({ client }: { readonly client: GenericIdentityClient }) {
  return (
    <NextIdentityProvider client={client}>
      <GroupsPage groups={[]} title={identityRoute(routes, "groups")} />
    </NextIdentityProvider>
  );
}
