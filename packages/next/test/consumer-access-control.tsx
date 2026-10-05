import type { ComponentProps } from "react";
import {
  DelegatedAuthorityPage,
  GroupAccessPage,
  ResourceScopesPage,
} from "@generic-identity/next/access-control";

export type NextAccessControlConsumerTypes = Readonly<{
  group: ComponentProps<typeof GroupAccessPage>;
  scopes: ComponentProps<typeof ResourceScopesPage>;
  delegatedAuthority: ComponentProps<typeof DelegatedAuthorityPage>;
}>;
