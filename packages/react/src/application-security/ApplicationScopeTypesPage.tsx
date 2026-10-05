import type { ReactNode } from "react";
import type { IdentityScopeTypeRecord } from "@generic-identity/contracts/application-security";
import { IdentityEmptyState, IdentityPageFrame, IdentityTable } from "../components/index";

export interface ApplicationScopeTypesPageProps {
  readonly scopeTypes: readonly IdentityScopeTypeRecord[];
  readonly actions?: ReactNode;
}

export function ApplicationScopeTypesPage({ scopeTypes, actions }: ApplicationScopeTypesPageProps) {
  return (
    <IdentityPageFrame title="Application scope types" description="Scope-type definitions registered for the selected application security model." actions={actions}>
      {scopeTypes.length === 0 ? <IdentityEmptyState title="No scope types" /> : (
        <IdentityTable caption="Application scope types">
          <thead><tr><th scope="col">Key</th><th scope="col">Display name</th><th scope="col">Parent</th><th scope="col">Tenant attachment</th></tr></thead>
          <tbody>{scopeTypes.map((scopeType) => <tr key={scopeType.key}><td><code>{scopeType.key}</code></td><td>{scopeType.displayName}</td><td>{scopeType.parentKey ? <code>{scopeType.parentKey}</code> : "Root"}</td><td>{scopeType.canAttachToTenant ? "Allowed" : "Not allowed"}</td></tr>)}</tbody>
        </IdentityTable>
      )}
    </IdentityPageFrame>
  );
}
