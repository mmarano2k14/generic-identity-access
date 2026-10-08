import type { ComponentProps } from "react";
import {
  OrganizationDetailsPage,
  OrganizationMembershipsPage,
  OrganizationsPage,
  OrganizationTreePage,
  OrganizationForm,
  OrganizationMembershipForm,
  OrganizationResourceScopeLinkForm,
} from "@generic-identity/next/organizations";

type OrganizationsProps = ComponentProps<typeof OrganizationsPage>;
type OrganizationDetailsProps = ComponentProps<typeof OrganizationDetailsPage>;
type OrganizationMembershipsProps = ComponentProps<typeof OrganizationMembershipsPage>;
type OrganizationTreeProps = ComponentProps<typeof OrganizationTreePage>;
type OrganizationFormProps = ComponentProps<typeof OrganizationForm>;
type OrganizationMembershipFormProps = ComponentProps<typeof OrganizationMembershipForm>;
type OrganizationResourceScopeLinkFormProps = ComponentProps<typeof OrganizationResourceScopeLinkForm>;

export type NextOrganizationConsumerTypes = Readonly<{
  organizations: OrganizationsProps;
  details: OrganizationDetailsProps;
  memberships: OrganizationMembershipsProps;
  tree: OrganizationTreeProps;
  form: OrganizationFormProps;
  membershipForm: OrganizationMembershipFormProps;
  resourceScopeLinkForm: OrganizationResourceScopeLinkFormProps;
}>;
