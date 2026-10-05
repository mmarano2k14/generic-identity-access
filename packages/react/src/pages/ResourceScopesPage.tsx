import type { ReactNode } from "react";
import type { IdentityResourceScopeRecord } from "@generic-identity/contracts/access-control";
import { IdentityEmptyState, IdentityPageFrame, IdentityTable } from "../components/index";
import { activeStatus } from "./internal";
export interface ResourceScopesPageProps { readonly resourceScopes: readonly IdentityResourceScopeRecord[]; readonly actions?: ReactNode; readonly renderScopeActions?: (scope: IdentityResourceScopeRecord) => ReactNode; }
export function ResourceScopesPage({ resourceScopes, actions, renderScopeActions }: ResourceScopesPageProps) {
  return <IdentityPageFrame title="Resource scopes" description="Versioned resource hierarchy used to narrow permission grants." actions={actions}>{resourceScopes.length === 0 ? <IdentityEmptyState title="No resource scopes" /> : <IdentityTable caption="Resource scopes"><thead><tr><th scope="col">Name</th><th scope="col">Type</th><th scope="col">External ID</th><th scope="col">Parent</th><th scope="col">Model version</th><th scope="col">Status</th>{renderScopeActions ? <th scope="col">Actions</th> : null}</tr></thead><tbody>{resourceScopes.map((scope) => <tr key={scope.resourceScopeId}><td>{scope.displayName}</td><td>{scope.scopeType}</td><td><code>{scope.externalResourceId}</code></td><td>{scope.parentResourceScopeId ? <code>{scope.parentResourceScopeId}</code> : "Root"}</td><td>{scope.modelVersion}</td><td>{activeStatus(scope.status)}</td>{renderScopeActions ? <td>{renderScopeActions(scope)}</td> : null}</tr>)}</tbody></IdentityTable>}</IdentityPageFrame>;
}
