"use client";

import { useMemo, useState, type ReactNode } from "react";
import type { IdentityGroupRecord, IdentityGroupTemplateResourceScopeRequirement, IdentityResourceScopeRecord } from "@generic-identity/contracts/access-control";
import { IdentityButton } from "../components/index";

type GroupTemplateFormAction = string | ((formData: FormData) => void | Promise<void>);

export interface ReusableGroupTemplateOption {
  readonly group: IdentityGroupRecord;
  readonly requirements: readonly IdentityGroupTemplateResourceScopeRequirement[];
}

export interface GroupFromTemplateFormProps {
  readonly tenantId: string;
  readonly templates: readonly ReusableGroupTemplateOption[];
  readonly targetResourceScopes: readonly IdentityResourceScopeRecord[];
  readonly formAction?: GroupTemplateFormAction;
  readonly error?: ReactNode;
}

/** Clone only reusable group policy definition. Memberships are never copied. */
export function GroupFromTemplateForm({ tenantId, templates, targetResourceScopes, formAction, error }: GroupFromTemplateFormProps) {
  const [sourceGroup, setSourceGroup] = useState("");
  const selected = useMemo(
    () => templates.find(({ group }) => `${group.tenantId}:${group.groupId}` === sourceGroup),
    [sourceGroup, templates],
  );
  const requirements = selected?.requirements ?? [];

  return <form className="gi-form" method={typeof formAction === "function" ? undefined : "post"} action={formAction} data-gi-component="group-from-template-form">
    <input type="hidden" name="tenantId" value={tenantId} />
    <label className="gi-field"><span className="gi-field-label">Reusable group</span><select className="gi-input" name="sourceGroup" required value={sourceGroup} onChange={(event) => setSourceGroup(event.currentTarget.value)}><option value="" disabled>Select a template</option>{templates.map(({ group }) => <option key={`${group.tenantId}:${group.groupId}`} value={`${group.tenantId}:${group.groupId}`}>{group.displayName}</option>)}</select></label>
    {requirements.map((requirement) => {
      const compatible = targetResourceScopes.filter((scope) => scope.status === 1 && scope.modelVersion === requirement.modelVersion && scope.scopeType === requirement.scopeType);
      return <label className="gi-field" key={requirement.sourceResourceScopeId}><span className="gi-field-label">Target scope for {requirement.displayName}</span><select className="gi-input" name={`resourceScopeMapping:${requirement.sourceResourceScopeId}`} defaultValue="" required><option value="" disabled>Select a compatible target scope</option>{compatible.map((scope) => <option key={scope.resourceScopeId} value={scope.resourceScopeId}>{scope.displayName}</option>)}</select><small className="gi-field-hint">Source scope type: {requirement.scopeType}; model version: {requirement.modelVersion}.</small></label>;
    })}
    {sourceGroup && requirements.length === 0 ? <small className="gi-field-hint">This template has no resource-scoped bindings. No scope mapping is required.</small> : null}
    {error ? <div className="gi-form-error" role="alert">{error}</div> : null}
    <IdentityButton variant="primary" type="submit" disabled={!sourceGroup}>Create group</IdentityButton>
  </form>;
}
