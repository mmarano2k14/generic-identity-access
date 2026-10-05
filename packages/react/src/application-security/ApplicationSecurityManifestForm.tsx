import type { ReactNode } from "react";
import { IdentityButton, IdentityInput } from "../components/index";

export interface ApplicationSecurityManifestFormProps {
  readonly applicationKey: string;
  readonly defaultModelVersion?: number;
  readonly defaultRbacProject?: string;
  readonly defaultNamespaces?: readonly string[];
  readonly defaultResourcesJson?: string;
  readonly formAction?: string;
  readonly error?: ReactNode;
}

export function ApplicationSecurityManifestForm({
  applicationKey,
  defaultModelVersion = 1,
  defaultRbacProject = "",
  defaultNamespaces = [],
  defaultResourcesJson = "[]",
  formAction,
  error,
}: ApplicationSecurityManifestFormProps) {
  return (
    <form className="gi-form" method="post" action={formAction} data-gi-component="application-security-manifest-form">
      <input type="hidden" name="schemaVersion" value="1" />
      <input type="hidden" name="applicationKey" value={applicationKey} />
      <label className="gi-field"><span className="gi-field-label">Application</span><IdentityInput value={applicationKey} readOnly /></label>
      <label className="gi-field"><span className="gi-field-label">Model version</span><IdentityInput name="modelVersion" type="number" min={1} defaultValue={defaultModelVersion} required /></label>
      <label className="gi-field"><span className="gi-field-label">RBAC project</span><IdentityInput name="rbacProject" defaultValue={defaultRbacProject} required /></label>
      <label className="gi-field"><span className="gi-field-label">RBAC namespaces</span><textarea className="gi-input" name="rbacNamespaces" rows={4} defaultValue={defaultNamespaces.join("\n")} required /></label>
      <label className="gi-field"><span className="gi-field-label">Resources JSON</span><textarea className="gi-input" name="resourcesJson" rows={14} defaultValue={defaultResourcesJson} required spellCheck={false} /></label>
      {error ? <div className="gi-form-error" role="alert">{error}</div> : null}
      <IdentityButton variant="primary" type="submit">Register security model</IdentityButton>
    </form>
  );
}
