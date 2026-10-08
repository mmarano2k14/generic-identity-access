import type { ReactNode } from "react";
import type { IdentityResourceScopeRecord } from "@generic-identity/contracts/access-control";
import { IdentityEmptyState, IdentityPageFrame, IdentityTable } from "../components/index";
import { activeStatus } from "./internal";

export interface ResourceScopesPageProps {
  readonly resourceScopes: readonly IdentityResourceScopeRecord[];
  readonly title?: string;
  readonly description?: string;
  readonly actions?: ReactNode;
  readonly selectedId?: string;
  readonly contextHeader?: string;
  readonly renderScopeContext?: (scope: IdentityResourceScopeRecord) => ReactNode;
  readonly renderScopeActions?: (scope: IdentityResourceScopeRecord) => ReactNode;
  readonly parentDisplayNames?: Readonly<Record<string, string>>;
  readonly renderParent?: (scope: IdentityResourceScopeRecord) => ReactNode;
  readonly getScopeKey?: (scope: IdentityResourceScopeRecord) => string;
}

export function ResourceScopesPage({
  resourceScopes,
  title = "Resource scopes",
  description = "Versioned resource hierarchy used to narrow permission grants.",
  actions,
  selectedId,
  contextHeader,
  renderScopeContext,
  renderScopeActions,
  parentDisplayNames = {},
  renderParent,
  getScopeKey,
}: ResourceScopesPageProps) {
  return (
    <IdentityPageFrame title={title} description={description} actions={actions}>
      {resourceScopes.length === 0 ? <IdentityEmptyState title="No resource scopes" /> : (
        <IdentityTable caption="Resource scopes">
          <thead>
            <tr>
              <th scope="col">Name</th>
              {contextHeader && renderScopeContext ? <th scope="col">{contextHeader}</th> : null}
              <th scope="col">Type</th>
              <th scope="col">External ID</th>
              <th scope="col">Parent</th>
              <th scope="col">Model version</th>
              <th scope="col">Status</th>
              <th scope="col">Record version</th>
              {renderScopeActions ? <th scope="col">Actions</th> : null}
            </tr>
          </thead>
          <tbody>
            {resourceScopes.map((scope) => (
              <tr key={getScopeKey?.(scope) ?? scope.resourceScopeId} aria-current={scope.resourceScopeId === selectedId ? "true" : undefined}>
                <td>{scope.displayName}</td>
                {contextHeader && renderScopeContext ? <td>{renderScopeContext(scope)}</td> : null}
                <td>{scope.scopeType}</td>
                <td><code>{scope.externalResourceId}</code></td>
                <td>{renderParent
                  ? renderParent(scope)
                  : scope.parentResourceScopeId
                    ? parentDisplayNames[scope.parentResourceScopeId] ?? <code>{scope.parentResourceScopeId}</code>
                    : "Root"}</td>
                <td>{scope.modelVersion}</td>
                <td>{activeStatus(scope.status)}</td>
                <td>{scope.version}</td>
                {renderScopeActions ? <td>{renderScopeActions(scope)}</td> : null}
              </tr>
            ))}
          </tbody>
        </IdentityTable>
      )}
    </IdentityPageFrame>
  );
}
