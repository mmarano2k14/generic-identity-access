import type { ReactNode } from "react";
import type {
  IdentityApplicationSecurityModelRecord,
  IdentityScopeTypeRecord,
} from "@generic-identity/contracts/application-security";
import { IdentityEmptyState, IdentityPageFrame, IdentityPanel, IdentityTable } from "../components/index";

export interface ApplicationSecurityModelDetailsPageProps {
  readonly model: IdentityApplicationSecurityModelRecord;
  readonly scopeTypes?: readonly IdentityScopeTypeRecord[];
  readonly scopeTypesForbidden?: boolean;
  readonly actions?: ReactNode;
}

export function ApplicationSecurityModelDetailsPage({
  model,
  scopeTypes,
  scopeTypesForbidden = false,
  actions,
}: ApplicationSecurityModelDetailsPageProps) {
  return (
    <IdentityPageFrame
      title={`Security model v${model.modelVersion}`}
      description={`Immutable application-owned model for ${model.applicationKey}. Register a new manifest version to change capabilities or RBAC context.`}
      actions={actions}
    >
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

      {scopeTypesForbidden ? (
        <IdentityPanel title="Scope-type catalogue access not granted">
          <p>The current administrator can inspect this security model but cannot access its scope-type catalogue.</p>
        </IdentityPanel>
      ) : scopeTypes !== undefined ? (
        <IdentityPanel title="Registered scope types">
          {scopeTypes.length === 0 ? <IdentityEmptyState title="No scope types registered" /> : (
            <IdentityTable caption="Registered scope types">
              <thead><tr><th scope="col">Key</th><th scope="col">Display name</th><th scope="col">Parent</th><th scope="col">Tenant attachment</th></tr></thead>
              <tbody>{scopeTypes.map((scopeType) => (
                <tr key={scopeType.key}>
                  <td><code>{scopeType.key}</code></td>
                  <td>{scopeType.displayName}</td>
                  <td>{scopeType.parentKey ?? "Root"}</td>
                  <td>{scopeType.canAttachToTenant ? "Allowed" : "Not allowed"}</td>
                </tr>
              ))}</tbody>
            </IdentityTable>
          )}
        </IdentityPanel>
      ) : null}

      <IdentityPanel title="Declared capabilities">
        <p>TRN previews are descriptive metadata only. They never establish an authorization decision.</p>
        {model.capabilities.length === 0 ? <IdentityEmptyState title="No capabilities registered" /> : (
          <IdentityTable caption="Declared capabilities">
            <thead><tr><th scope="col">Resource</th><th scope="col">Feature</th><th scope="col">Action</th><th scope="col">Display name</th><th scope="col">TRN previews</th></tr></thead>
            <tbody>{model.capabilities.map((capability) => (
              <tr key={`${capability.resource}:${capability.feature}:${capability.action}`}>
                <td><code>{capability.resource}</code></td>
                <td><code>{capability.feature}</code></td>
                <td><code>{capability.action}</code></td>
                <td>{capability.displayName}</td>
                <td>{model.rbacNamespaces.map((namespace) => (
                  <div key={namespace}><code>{`trn:${model.rbacProject}:${namespace}:${capability.resource}:${capability.feature}:${capability.action}`}</code></div>
                ))}</td>
              </tr>
            ))}</tbody>
          </IdentityTable>
        )}
      </IdentityPanel>
    </IdentityPageFrame>
  );
}
