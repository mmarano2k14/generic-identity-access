import type { ReactNode } from "react";
import type { IdentityManagedPolicyRecord, IdentityManagedPolicyStatementRecord, IdentityManagedPolicyVersionRecord } from "@generic-identity/contracts";
import { IdentityEmptyState, IdentityPageFrame, IdentityPanel, IdentityTable } from "../components/index";
import { activeStatus } from "./internal";

export interface PolicyDetailsPageProps {
  readonly policy: IdentityManagedPolicyRecord;
  readonly versions?: readonly IdentityManagedPolicyVersionRecord[];
  readonly statements?: readonly IdentityManagedPolicyStatementRecord[];
  readonly actions?: ReactNode;
}

export function PolicyDetailsPage({ policy, versions = [], statements = [], actions }: PolicyDetailsPageProps) {
  return (
    <IdentityPageFrame title={policy.displayName} description={`Managed policy ${policy.policyKey}`} actions={actions}>
      <IdentityPanel title="Policy">
        <dl className="gi-definition-list"><div><dt>Policy ID</dt><dd><code>{policy.policyId}</code></dd></div><div><dt>Status</dt><dd>{activeStatus(policy.status)}</dd></div><div><dt>Default version</dt><dd>{policy.defaultVersion ?? "—"}</dd></div><div><dt>Record version</dt><dd>{policy.version}</dd></div></dl>
      </IdentityPanel>
      <IdentityPanel title="Published and draft versions">
        {versions.length === 0 ? <IdentityEmptyState title="No versions" /> : <IdentityTable caption="Policy versions"><thead><tr><th scope="col">Policy version</th><th scope="col">Model version</th><th scope="col">Published</th></tr></thead><tbody>{versions.map((version) => <tr key={version.policyVersion}><td>{version.policyVersion}</td><td>{version.modelVersion}</td><td>{version.publishedAt ?? "Draft"}</td></tr>)}</tbody></IdentityTable>}
      </IdentityPanel>
      <IdentityPanel title="Statements">
        {statements.length === 0 ? <IdentityEmptyState title="No statements" /> : <IdentityTable caption="Policy statements"><thead><tr><th scope="col">Version</th><th scope="col">Resource</th><th scope="col">Feature</th><th scope="col">Action</th></tr></thead><tbody>{statements.map((statement) => <tr key={`${statement.policyVersion}:${statement.statementId}`}><td>{statement.policyVersion}</td><td><code>{statement.resource}</code></td><td><code>{statement.feature}</code></td><td><code>{statement.action}</code></td></tr>)}</tbody></IdentityTable>}
      </IdentityPanel>
    </IdentityPageFrame>
  );
}
