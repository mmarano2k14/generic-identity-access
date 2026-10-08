import type { ComponentProps } from "react";
import {
  DelegatedAuthorityPage,
  DelegatedAuthorityGroupForm,
  DelegatedAuthorityMemberForm,
  DelegatedAuthorityPolicyForm,
  DelegatedAuthorityPolicyStatementForm,
  DelegatedAuthorityPolicyBindingForm,
  GroupAccessPage,
  ResourceScopesPage,
  ResourceScopeDetailsPage,
  ResourceScopeForm,
  ResourceScopeTypeFields,
} from "@generic-identity/next/access-control";

export type NextAccessControlConsumerTypes = Readonly<{
  group: ComponentProps<typeof GroupAccessPage>;
  scopes: ComponentProps<typeof ResourceScopesPage>;
  scopeDetails: ComponentProps<typeof ResourceScopeDetailsPage>;
  scopeForm: ComponentProps<typeof ResourceScopeForm>;
  scopeTypeFields: ComponentProps<typeof ResourceScopeTypeFields>;
  delegatedAuthority: ComponentProps<typeof DelegatedAuthorityPage>;
  authorityGroupForm: ComponentProps<typeof DelegatedAuthorityGroupForm>;
  authorityMemberForm: ComponentProps<typeof DelegatedAuthorityMemberForm>;
  authorityPolicyForm: ComponentProps<typeof DelegatedAuthorityPolicyForm>;
  authorityStatementForm: ComponentProps<typeof DelegatedAuthorityPolicyStatementForm>;
  authorityBindingForm: ComponentProps<typeof DelegatedAuthorityPolicyBindingForm>;
}>;
