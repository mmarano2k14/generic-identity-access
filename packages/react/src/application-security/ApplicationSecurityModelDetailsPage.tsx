import type { ReactNode } from "react";
import type { IdentityApplicationSecurityModelRecord } from "@generic-identity/contracts/application-security";
import { IdentityPageFrame, IdentityPanel, IdentityTable } from "../components/index";

export interface ApplicationSecurityModelDetailsPageProps {
  readonly model: IdentityApplicationSecurityModelRecord;
  readonly actions?: ReactNode;
}

export function ApplicationSecurityModelDetailsPage({ model, actions }: ApplicationSecurityModelDetailsPageProps) {
  return (
    <IdentityPageFrame title={`Security model v${model.modelVersion}`} description={`Application ${model.applicationKey}`} actions={actions}>
      <IdentityPanel title="Registration">
        <dl className="gi-definition-list">
          <div><dt>Schema version</dt><dd>{model.schemaVersion}</dd></div>
          <div><dt>Application</dt><dd><code>{model.applicationKey}</code></dd></div>
          <div><dt>Model version</dt><dd>{model.modelVersion}</dd></div>
          <div><dt>RBAC project</dt><dd><code>{model.rbacProject}</code></dd></div>
          <div><dt>RBAC namespaces</dt><dd>{model.rbacNamespaces.map((value) => <code key={value}>{value} </code>)}</dd></div>
          <div><dt>Manifest fingerprint</dt><dd><code>{model.manifestSha256}</code></dd></div>
        </dl>
      </IdentityPanel>
      <IdentityPanel title="Capabilities">
        <IdentityTable caption="Declared capabilities">
          <thead><tr><th scope="col">Resource</th><th scope="col">Feature</th><th scope="col">Action</th><th scope="col">Display name</th></tr></thead>
          <tbody>{model.capabilities.map((capability) => (
            <tr key={`${capability.resource}:${capability.feature}:${capability.action}`}><td><code>{capability.resource}</code></td><td><code>{capability.feature}</code></td><td><code>{capability.action}</code></td><td>{capability.displayName}</td></tr>
          ))}</tbody>
        </IdentityTable>
      </IdentityPanel>
    </IdentityPageFrame>
  );
}
