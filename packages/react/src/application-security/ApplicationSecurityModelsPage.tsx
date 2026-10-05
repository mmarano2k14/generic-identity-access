import type { ReactNode } from "react";
import type { IdentityApplicationSecurityModelSummaryRecord } from "@generic-identity/contracts/application-security";
import { IdentityEmptyState, IdentityPageFrame, IdentityTable } from "../components/index";

export interface ApplicationSecurityModelsPageProps {
  readonly models: readonly IdentityApplicationSecurityModelSummaryRecord[];
  readonly actions?: ReactNode;
  readonly renderModelActions?: (model: IdentityApplicationSecurityModelSummaryRecord) => ReactNode;
}

export function ApplicationSecurityModelsPage({ models, actions, renderModelActions }: ApplicationSecurityModelsPageProps) {
  return (
    <IdentityPageFrame
      title="Application security models"
      description="Immutable, versioned application capability models registered with Generic Identity."
      actions={actions}
    >
      {models.length === 0 ? (
        <IdentityEmptyState title="No security models" description="No application security model has been registered for this context." />
      ) : (
        <IdentityTable caption="Application security models">
          <thead><tr><th scope="col">Version</th><th scope="col">Application</th><th scope="col">RBAC project</th><th scope="col">Namespaces</th><th scope="col">Capabilities</th><th scope="col">Manifest fingerprint</th>{renderModelActions ? <th scope="col">Actions</th> : null}</tr></thead>
          <tbody>{models.map((model) => (
            <tr key={`${model.applicationKey}:${model.modelVersion}`}>
              <td>{model.modelVersion}</td><td><code>{model.applicationKey}</code></td><td><code>{model.rbacProject}</code></td><td>{model.rbacNamespaces.join(", ")}</td><td>{model.capabilityCount}</td><td><code>{model.manifestSha256}</code></td>{renderModelActions ? <td>{renderModelActions(model)}</td> : null}
            </tr>
          ))}</tbody>
        </IdentityTable>
      )}
    </IdentityPageFrame>
  );
}
