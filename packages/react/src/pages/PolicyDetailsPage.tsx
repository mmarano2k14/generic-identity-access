import type { ReactNode } from "react";
import type {
  IdentityManagedPolicyRecord,
  IdentityManagedPolicyStatementRecord,
  IdentityManagedPolicyVersionRecord,
} from "@generic-identity/contracts";
import { IdentityEmptyState, IdentityPageFrame, IdentityPanel, IdentityTable } from "../components/index";
import { activeStatus } from "./internal";

export interface PolicyDetailsPageProps {
  readonly policy: IdentityManagedPolicyRecord;
  readonly versions?: readonly IdentityManagedPolicyVersionRecord[];
  readonly selectedVersion?: IdentityManagedPolicyVersionRecord | null;
  readonly statements?: readonly IdentityManagedPolicyStatementRecord[];
  readonly actions?: ReactNode;
  readonly versionActions?: ReactNode;
  readonly statementActions?: ReactNode;
  readonly renderVersionActions?: (version: IdentityManagedPolicyVersionRecord) => ReactNode;
  readonly renderStatementActions?: (statement: IdentityManagedPolicyStatementRecord) => ReactNode;
}

export function PolicyDetailsPage({
  policy,
  versions = [],
  selectedVersion = null,
  statements = [],
  actions,
  versionActions,
  statementActions,
  renderVersionActions,
  renderStatementActions,
}: PolicyDetailsPageProps) {
  const selectedIsDefault = selectedVersion !== null && policy.defaultVersion === selectedVersion.policyVersion;
  return (
    <IdentityPageFrame title={policy.displayName} description={`Managed policy ${policy.policyKey}`} actions={actions}>
      <IdentityPanel title="Policy">
        <dl className="gi-definition-list">
          <div><dt>Policy ID</dt><dd><code>{policy.policyId}</code></dd></div>
          <div><dt>Policy key</dt><dd><code>{policy.policyKey}</code></dd></div>
          <div><dt>Status</dt><dd>{activeStatus(policy.status)}</dd></div>
          <div><dt>Default version</dt><dd>{policy.defaultVersion ?? "—"}</dd></div>
          <div><dt>Record version</dt><dd>{policy.version}</dd></div>
          <div><dt>Tenant ownership</dt><dd>None</dd></div>
        </dl>
      </IdentityPanel>

      <IdentityPanel title="Managed policy versions">
        {versionActions ? <div className="gi-page-actions">{versionActions}</div> : null}
        {versions.length === 0 ? <IdentityEmptyState title="No versions" /> : (
          <IdentityTable caption="Policy versions">
            <thead><tr><th scope="col">Policy version</th><th scope="col">Model version</th><th scope="col">Lifecycle</th><th scope="col">Default</th>{renderVersionActions ? <th scope="col">Actions</th> : null}</tr></thead>
            <tbody>{versions.map((version) => <tr key={version.policyVersion} data-gi-record-id={String(version.policyVersion)}>
              <td>{version.policyVersion}</td><td>{version.modelVersion}</td><td>{version.publishedAt ?? "Draft"}</td><td>{policy.defaultVersion === version.policyVersion ? "Yes" : "No"}</td>{renderVersionActions ? <td>{renderVersionActions(version)}</td> : null}
            </tr>)}</tbody>
          </IdentityTable>
        )}
      </IdentityPanel>

      {selectedVersion ? <IdentityPanel title={`Selected version ${selectedVersion.policyVersion}`}>
        {statementActions ? <div className="gi-page-actions">{statementActions}</div> : null}
        <dl className="gi-definition-list">
          <div><dt>Security model</dt><dd>v{selectedVersion.modelVersion}</dd></div>
          <div><dt>Lifecycle</dt><dd>{selectedVersion.publishedAt ?? "Draft and editable"}</dd></div>
          <div><dt>Default</dt><dd>{selectedIsDefault ? "Yes" : "No"}</dd></div>
        </dl>
        {statements.length === 0 ? <IdentityEmptyState title="No statements" /> : (
          <IdentityTable caption={`Statements for policy version ${selectedVersion.policyVersion}`}>
            <thead><tr><th scope="col">Resource</th><th scope="col">Feature</th><th scope="col">Action</th><th scope="col">Model version</th>{renderStatementActions ? <th scope="col">Actions</th> : null}</tr></thead>
            <tbody>{statements.map((statement) => <tr key={statement.statementId} data-gi-record-id={statement.statementId}>
              <td><code>{statement.resource}</code></td><td><code>{statement.feature}</code></td><td><code>{statement.action}</code></td><td>{statement.modelVersion}</td>{renderStatementActions ? <td>{renderStatementActions(statement)}</td> : null}
            </tr>)}</tbody>
          </IdentityTable>
        )}
      </IdentityPanel> : null}
    </IdentityPageFrame>
  );
}
