import type { ComponentProps } from "react";
import {
  GroupAccessPage,
  GroupForm,
  GroupFromTemplateForm,
  GroupMemberForm,
  GroupsPage,
  ManagedPolicyBindingForm,
} from "@generic-identity/next/access-control";

type GroupsProps = ComponentProps<typeof GroupsPage>;
type GroupAccessProps = ComponentProps<typeof GroupAccessPage>;
type GroupFormProps = ComponentProps<typeof GroupForm>;
type GroupFromTemplateProps = ComponentProps<typeof GroupFromTemplateForm>;
type GroupMemberProps = ComponentProps<typeof GroupMemberForm>;
type BindingProps = ComponentProps<typeof ManagedPolicyBindingForm>;

export type NextGroupsConsumerTypes = Readonly<{
  groups: GroupsProps;
  access: GroupAccessProps;
  groupForm: GroupFormProps;
  templateForm: GroupFromTemplateProps;
  memberForm: GroupMemberProps;
  bindingForm: BindingProps;
}>;
