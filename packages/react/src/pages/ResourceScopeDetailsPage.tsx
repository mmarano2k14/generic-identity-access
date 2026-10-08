import type { ReactNode } from "react";
import type { IdentityResourceScopeRecord } from "@generic-identity/contracts/access-control";
import { IdentityPageFrame, IdentityPanel } from "../components/index";
import { activeStatus } from "./internal";

export interface ResourceScopeDetailsPageProps {
  readonly resourceScope: IdentityResourceScopeRecord;
  readonly tenantDisplayName?: string;
  readonly parentDisplayName?: string;
  readonly actions?: ReactNode;
}

export function ResourceScopeDetailsPage({
  resourceScope,
  tenantDisplayName,
  parentDisplayName,
  actions,
}: ResourceScopeDetailsPageProps) {
  const facts = [
    { label: "Tenant", value: tenantDisplayName ?? "—" },
    { label: "Scope type", value: resourceScope.scopeType },
    { label: "External resource", value: resourceScope.externalResourceId, mono: true },
    { label: "Security model", value: `v${resourceScope.modelVersion}` },
    { label: "Parent", value: parentDisplayName ?? resourceScope.parentResourceScopeId ?? "Root" },
    { label: "Status", value: activeStatus(resourceScope.status) },
    { label: "Record version", value: String(resourceScope.version) },
  ] as const;

  return (
    <IdentityPageFrame
      title={resourceScope.displayName}
      description="Selected ResourceScope hierarchy and external-resource metadata."
      actions={actions}
    >
      <IdentityPanel title="Resource scope details">
        <dl className="gi-definition-list">
          <div><dt>Resource scope ID</dt><dd><code>{resourceScope.resourceScopeId}</code></dd></div>
          {facts.map((fact) => (
            <div key={fact.label}>
              <dt>{fact.label}</dt>
              <dd>{"mono" in fact && fact.mono ? <code>{fact.value}</code> : fact.value}</dd>
            </div>
          ))}
        </dl>
      </IdentityPanel>
    </IdentityPageFrame>
  );
}
