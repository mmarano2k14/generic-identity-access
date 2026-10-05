import type { ReactNode } from "react";
import type { IdentityUserRecord } from "@generic-identity/contracts";
import { IdentityEmptyState, IdentityPageFrame, IdentityTable } from "../components/index";
import { activeStatus } from "./internal";

export interface UsersPageProps {
  readonly users: readonly IdentityUserRecord[];
  readonly title?: string;
  readonly description?: string;
  readonly actions?: ReactNode;
  readonly renderUserActions?: (user: IdentityUserRecord) => ReactNode;
}

export function UsersPage({
  users,
  title = "Users",
  description = "Identity accounts visible in the current administration boundary.",
  actions,
  renderUserActions,
}: UsersPageProps) {
  return (
    <IdentityPageFrame title={title} description={description} actions={actions}>
      {users.length === 0 ? (
        <IdentityEmptyState title="No users" description="No user is visible in the current boundary." />
      ) : (
        <IdentityTable caption="Users">
          <thead><tr><th scope="col">Display name</th><th scope="col">User ID</th><th scope="col">Status</th><th scope="col">Version</th>{renderUserActions ? <th scope="col">Actions</th> : null}</tr></thead>
          <tbody>
            {users.map((user) => (
              <tr key={user.userId} data-gi-record-id={user.userId}>
                <td>{user.displayName}</td><td><code>{user.userId}</code></td><td>{activeStatus(user.status)}</td><td>{user.version}</td>{renderUserActions ? <td>{renderUserActions(user)}</td> : null}
              </tr>
            ))}
          </tbody>
        </IdentityTable>
      )}
    </IdentityPageFrame>
  );
}
