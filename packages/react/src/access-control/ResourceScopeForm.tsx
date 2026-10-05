import type { ReactNode } from "react";
import type { IdentityResourceScopeRecord } from "@generic-identity/contracts/access-control";
import { IdentityButton, IdentityInput } from "../components/index";
export interface ResourceScopeFormProps { readonly resourceScope?: IdentityResourceScopeRecord | null; readonly parentScopes?: readonly IdentityResourceScopeRecord[]; readonly formAction?: string; readonly error?: ReactNode; }
export function ResourceScopeForm({ resourceScope = null, parentScopes = [], formAction, error }: ResourceScopeFormProps) {
  return <form className="gi-form" method="post" action={formAction} data-gi-component="resource-scope-form">
    <label className="gi-field"><span className="gi-field-label">Model version</span><IdentityInput name="modelVersion" type="number" min={1} defaultValue={resourceScope?.modelVersion ?? 1} required /></label>
    <label className="gi-field"><span className="gi-field-label">Scope type</span><IdentityInput name="scopeType" defaultValue={resourceScope?.scopeType ?? ""} required /></label>
    <label className="gi-field"><span className="gi-field-label">External resource ID</span><IdentityInput name="externalResourceId" defaultValue={resourceScope?.externalResourceId ?? ""} required /></label>
    <label className="gi-field"><span className="gi-field-label">Display name</span><IdentityInput name="displayName" defaultValue={resourceScope?.displayName ?? ""} required /></label>
    <label className="gi-field"><span className="gi-field-label">Parent scope</span><select className="gi-input" name="parentResourceScopeId" defaultValue={resourceScope?.parentResourceScopeId ?? ""}><option value="">None</option>{parentScopes.filter((scope) => scope.resourceScopeId !== resourceScope?.resourceScopeId).map((scope) => <option key={scope.resourceScopeId} value={scope.resourceScopeId}>{scope.displayName}</option>)}</select></label>
    <label className="gi-field"><span className="gi-field-label">Status</span><select className="gi-input" name="status" defaultValue={String(resourceScope?.status ?? 1)}><option value="1">Active</option><option value="2">Inactive</option></select></label>
    {resourceScope ? <input type="hidden" name="expectedVersion" value={resourceScope.version} /> : null}
    {error ? <div className="gi-form-error" role="alert">{error}</div> : null}
    <IdentityButton variant="primary" type="submit">{resourceScope ? "Save resource scope" : "Create resource scope"}</IdentityButton>
  </form>;
}
