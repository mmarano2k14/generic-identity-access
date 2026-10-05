import type { ComponentProps } from "react";
import {
  DelegatedAuthorityPage,
  GroupAccessPage,
  ManagedPolicyBindingsPage,
  ResourceScopesPage,
} from "@generic-identity/react/access-control";

export type AccessControlConsumerTypes = Readonly<{
  group: ComponentProps<typeof GroupAccessPage>;
  managedBindings: ComponentProps<typeof ManagedPolicyBindingsPage>;
  scopes: ComponentProps<typeof ResourceScopesPage>;
  delegatedAuthority: ComponentProps<typeof DelegatedAuthorityPage>;
}>;
