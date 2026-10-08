import type { ComponentProps } from "react";
import {
  ManagedPolicyForm,
  ManagedPolicyPublishForm,
  ManagedPolicyStatementForm,
  ManagedPolicyStatementRemoveForm,
  ManagedPolicyVersionForm,
  PoliciesPage,
  PolicyDetailsPage,
} from "@generic-identity/next/access-control";

type CatalogProps = ComponentProps<typeof PoliciesPage>;
type DetailsProps = ComponentProps<typeof PolicyDetailsPage>;
type PolicyFormProps = ComponentProps<typeof ManagedPolicyForm>;
type VersionFormProps = ComponentProps<typeof ManagedPolicyVersionForm>;
type StatementFormProps = ComponentProps<typeof ManagedPolicyStatementForm>;
type PublishFormProps = ComponentProps<typeof ManagedPolicyPublishForm>;
type RemoveStatementFormProps = ComponentProps<typeof ManagedPolicyStatementRemoveForm>;

export type NextManagedPolicyConsumerTypes = Readonly<{
  catalog: CatalogProps;
  details: DetailsProps;
  policyForm: PolicyFormProps;
  versionForm: VersionFormProps;
  statementForm: StatementFormProps;
  publishForm: PublishFormProps;
  removeStatementForm: RemoveStatementFormProps;
}>;
