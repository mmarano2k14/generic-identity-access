import type { ComponentProps } from "react";
import {
  OrganizationDetailsPage,
  OrganizationMembershipsPage,
  OrganizationsPage,
  OrganizationTreePage,
} from "@generic-identity/next/organizations";

type OrganizationsProps = ComponentProps<typeof OrganizationsPage>;
type OrganizationDetailsProps = ComponentProps<typeof OrganizationDetailsPage>;
type OrganizationMembershipsProps = ComponentProps<typeof OrganizationMembershipsPage>;
type OrganizationTreeProps = ComponentProps<typeof OrganizationTreePage>;

export type NextOrganizationConsumerTypes = Readonly<{
  organizations: OrganizationsProps;
  details: OrganizationDetailsProps;
  memberships: OrganizationMembershipsProps;
  tree: OrganizationTreeProps;
}>;
