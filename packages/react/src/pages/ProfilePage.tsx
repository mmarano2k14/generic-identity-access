import type { ReactNode } from "react";
import type { IdentityUserRecord } from "@generic-identity/contracts";
import { IdentityPageFrame, IdentityPanel } from "../components/index";
import { activeStatus } from "./internal";

export interface ProfilePageProps {
  readonly user: IdentityUserRecord;
  readonly actions?: ReactNode;
}

export function ProfilePage({ user, actions }: ProfilePageProps) {
  return (
    <IdentityPageFrame title="Profile" description="Identity profile metadata." actions={actions}>
      <IdentityPanel>
        <dl className="gi-definition-list"><div><dt>Display name</dt><dd>{user.displayName}</dd></div><div><dt>User ID</dt><dd><code>{user.userId}</code></dd></div><div><dt>Status</dt><dd>{activeStatus(user.status)}</dd></div><div><dt>Version</dt><dd>{user.version}</dd></div></dl>
      </IdentityPanel>
    </IdentityPageFrame>
  );
}
