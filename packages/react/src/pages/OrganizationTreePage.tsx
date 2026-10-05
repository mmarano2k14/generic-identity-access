import type { ReactNode } from "react";
import type { IdentityOrganizationTreeNodeRecord } from "@generic-identity/contracts/organizations";
import { IdentityEmptyState, IdentityPageFrame, IdentityPanel } from "../components/index";
import { activeStatus } from "./internal";

export interface OrganizationTreePageProps {
  readonly tree: readonly IdentityOrganizationTreeNodeRecord[];
  readonly actions?: ReactNode;
  readonly renderOrganizationActions?: (node: IdentityOrganizationTreeNodeRecord) => ReactNode;
}

function OrganizationTreeNode({ node, renderOrganizationActions }: { readonly node: IdentityOrganizationTreeNodeRecord; readonly renderOrganizationActions?: (node: IdentityOrganizationTreeNodeRecord) => ReactNode }) {
  return (
    <li data-gi-record-id={node.organization.organizationId}>
      <div className="gi-tree-node"><span>{node.organization.displayName}</span> <code>{node.organization.organizationKey}</code> <span>{activeStatus(node.organization.status)}</span>{renderOrganizationActions ? <span>{renderOrganizationActions(node)}</span> : null}</div>
      {node.children.length > 0 ? <ul>{node.children.map((child) => <OrganizationTreeNode key={child.organization.organizationId} node={child} {...(renderOrganizationActions ? { renderOrganizationActions } : {})} />)}</ul> : null}
    </li>
  );
}

export function OrganizationTreePage({ tree, actions, renderOrganizationActions }: OrganizationTreePageProps) {
  return (
    <IdentityPageFrame title="Organization hierarchy" description="Tenant-local Organization hierarchy." actions={actions}>
      <IdentityPanel title="Hierarchy">
        {tree.length === 0 ? <IdentityEmptyState title="No organizations" /> : <ul className="gi-tree">{tree.map((node) => <OrganizationTreeNode key={node.organization.organizationId} node={node} {...(renderOrganizationActions ? { renderOrganizationActions } : {})} />)}</ul>}
      </IdentityPanel>
    </IdentityPageFrame>
  );
}
