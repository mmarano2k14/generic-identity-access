import type { ReactNode } from "react";
import { IdentityButton, IdentityEntityAutocomplete } from "../components/index";

type DelegatedAuthorityMemberFormAction = string | ((formData: FormData) => void | Promise<void>);

export interface DelegatedAuthorityMemberFormProps {
  readonly groupId: string;
  readonly excludeUserIds?: readonly string[];
  readonly formAction?: DelegatedAuthorityMemberFormAction;
  readonly error?: ReactNode;
}

export function DelegatedAuthorityMemberForm({ groupId, excludeUserIds = [], formAction, error }: DelegatedAuthorityMemberFormProps) {
  return <form className="gi-form" method={typeof formAction === "function" ? undefined : "post"} action={formAction} data-gi-component="delegated-authority-member-form">
    <input type="hidden" name="groupId" value={groupId} />
    <IdentityEntityAutocomplete
      label="User"
      name="userId"
      kind="user"
      excludeIds={excludeUserIds}
      required
      hint="Type at least 3 characters of the user display name, or enter the full ID."
    />
    {error ? <div className="gi-form-error" role="alert">{error}</div> : null}
    <IdentityButton variant="primary" type="submit">Add authority member</IdentityButton>
  </form>;
}
