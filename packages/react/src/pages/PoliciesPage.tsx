import type { ReactNode } from "react";
import type { IdentityManagedPolicyRecord } from "@generic-identity/contracts";
import { IdentityEmptyState, IdentityPageFrame, IdentityTable } from "../components/index";
import { activeStatus } from "./internal";

export interface PoliciesPageProps {
  readonly policies: readonly IdentityManagedPolicyRecord[];
  readonly title?: string;
  readonly description?: string;
  readonly actions?: ReactNode;
  readonly renderPolicyActions?: (policy: IdentityManagedPolicyRecord) => ReactNode;
}

export function PoliciesPage({
  policies,
  title = "Policies",
  description = "Managed permission policies available in the current administration boundary.",
  actions,
  renderPolicyActions,
}: PoliciesPageProps) {
  return (
    <IdentityPageFrame title={title} description={description} actions={actions}>
      {policies.length === 0 ? <IdentityEmptyState title="No policies" /> : (
        <IdentityTable caption="Policies">
          <thead><tr><th scope="col">Name</th><th scope="col">Policy key</th><th scope="col">Status</th><th scope="col">Default version</th><th scope="col">Record version</th>{renderPolicyActions ? <th scope="col">Actions</th> : null}</tr></thead>
          <tbody>{policies.map((policy) => <tr key={policy.policyId} data-gi-record-id={policy.policyId}><td>{policy.displayName}</td><td><code>{policy.policyKey}</code></td><td>{activeStatus(policy.status)}</td><td>{policy.defaultVersion ?? "—"}</td><td>{policy.version}</td>{renderPolicyActions ? <td>{renderPolicyActions(policy)}</td> : null}</tr>)}</tbody>
        </IdentityTable>
      )}
    </IdentityPageFrame>
  );
}
