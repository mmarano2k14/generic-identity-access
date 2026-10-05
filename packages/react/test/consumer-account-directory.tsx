import type { ComponentProps } from "react";
import {
  AuthenticationStepUpPage,
  MembershipCandidatePanel,
  MembershipsPage,
  PasswordPage,
  TenantDetailsPage,
  TenantsPage,
} from "@generic-identity/react/pages";

type PasswordProps = ComponentProps<typeof PasswordPage>;
type TenantsProps = ComponentProps<typeof TenantsPage>;
type TenantDetailsProps = ComponentProps<typeof TenantDetailsPage>;
type MembershipsProps = ComponentProps<typeof MembershipsPage>;
type CandidateProps = ComponentProps<typeof MembershipCandidatePanel>;
type StepUpProps = ComponentProps<typeof AuthenticationStepUpPage>;

export type AccountDirectoryReactConsumerTypes = Readonly<{
  password: PasswordProps;
  tenants: TenantsProps;
  tenantDetails: TenantDetailsProps;
  memberships: MembershipsProps;
  candidate: CandidateProps;
  stepUp: StepUpProps;
}>;
