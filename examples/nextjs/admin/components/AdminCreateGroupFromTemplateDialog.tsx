"use client";

import { useMemo, useState } from "react";
import type { AdminServerAction } from "./AdminMutationDialog";
import { AdminMutationDialog } from "./AdminMutationDialog";
import { AdminSelectField } from "./AdminField";

export interface AdminGroupTemplateScopeRequirementOption {
  readonly sourceResourceScopeId: string;
  readonly modelVersion: number;
  readonly scopeType: string;
  readonly displayName: string;
}

export interface AdminReusableGroupTemplateOption {
  readonly value: string;
  readonly displayName: string;
  readonly requirements: readonly AdminGroupTemplateScopeRequirementOption[];
}

export interface AdminTargetResourceScopeOption {
  readonly resourceScopeId: string;
  readonly modelVersion: number;
  readonly scopeType: string;
  readonly displayName: string;
  readonly status: number;
}

export interface AdminCreateGroupFromTemplateDialogProps {
  readonly tenantId: string;
  readonly templates: readonly AdminReusableGroupTemplateOption[];
  readonly targetResourceScopes: readonly AdminTargetResourceScopeOption[];
  readonly action: AdminServerAction;
}

/**
 * Renders reusable-group cloning with explicit target-scope mapping.
 * Scoped source bindings are never copied with source-tenant resource identifiers.
 */
export function AdminCreateGroupFromTemplateDialog({
  tenantId,
  templates,
  targetResourceScopes,
  action,
}: AdminCreateGroupFromTemplateDialogProps) {
  const [selectedTemplate, setSelectedTemplate] = useState("");
  const requirements = useMemo(
    () => templates.find((template) => template.value === selectedTemplate)?.requirements ?? [],
    [selectedTemplate, templates],
  );

  return (
    <AdminMutationDialog
      title="Create from template"
      description="Create a new group in this tenant by copying the selected reusable group's managed policy bindings. Members are never copied. Scoped bindings must be mapped to resource scopes owned by this tenant."
      triggerLabel="Create from template"
      submitLabel="Create group"
      action={action}
      triggerVariant="secondary"
    >
      <input type="hidden" name="tenantId" value={tenantId} />
      <AdminSelectField
        label="Reusable group"
        name="sourceGroup"
        required
        value={selectedTemplate}
        onChange={(event) => setSelectedTemplate(event.currentTarget.value)}
      >
        <option value="" disabled>Select a template</option>
        {templates.map((template) => (
          <option key={template.value} value={template.value}>{template.displayName}</option>
        ))}
      </AdminSelectField>

      {requirements.map((requirement) => {
        const compatibleScopes = targetResourceScopes.filter(
          (scope) =>
            scope.status === 1 &&
            scope.modelVersion === requirement.modelVersion &&
            scope.scopeType === requirement.scopeType,
        );

        return (
          <AdminSelectField
            key={requirement.sourceResourceScopeId}
            label={`Target scope for ${requirement.displayName}`}
            name={`resourceScopeMapping:${requirement.sourceResourceScopeId}`}
            required
            defaultValue=""
            hint={`Source scope type: ${requirement.scopeType}; model version: ${requirement.modelVersion}.`}
          >
            <option value="" disabled>Select a compatible target scope</option>
            {compatibleScopes.map((scope) => (
              <option key={scope.resourceScopeId} value={scope.resourceScopeId}>{scope.displayName}</option>
            ))}
          </AdminSelectField>
        );
      })}

      {selectedTemplate && requirements.length === 0 ? (
        <p className="ia-field-hint">This template has no resource-scoped bindings. No scope mapping is required.</p>
      ) : null}
    </AdminMutationDialog>
  );
}
