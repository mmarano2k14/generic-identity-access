import type { ComponentProps, ReactNode } from "react";
import { IdentityButton } from "../components/index";

export interface ApplicationSecurityManifestUploadFormProps {
  /** Trusted application key shown to the administrator, not submitted as authority. */
  readonly applicationKey: string;
  readonly formAction?: ComponentProps<"form">["action"];
  readonly error?: ReactNode;
}

/**
 * Matches the GOLDEN security-model registration UI: upload a project-owned
 * JSON manifest. No individual capability can be authored by this form.
 */
export function ApplicationSecurityManifestUploadForm({
  applicationKey,
  formAction,
  error,
}: ApplicationSecurityManifestUploadFormProps) {
  const serverAction = typeof formAction === "function";
  return (
    <form
      className="gi-form"
      method={serverAction ? undefined : "post"}
      encType={serverAction ? undefined : "multipart/form-data"}
      action={formAction}
      data-gi-component="application-security-manifest-upload-form"
    >
      <p className="gi-form-hint">Upload a project-owned JSON manifest for application <code>{applicationKey}</code>. Its applicationKey must match the trusted server context.</p>
      <label className="gi-field">
        <span className="gi-field-label">Security manifest JSON</span>
        <input className="gi-input" type="file" name="manifestFile" accept="application/json,.json" required />
      </label>
      <p className="gi-form-hint">Only .json files up to 256 KiB are accepted. Capabilities remain immutable and application-owned.</p>
      {error ? <div className="gi-form-error" role="alert">{error}</div> : null}
      <IdentityButton variant="primary" type="submit">Register security model</IdentityButton>
    </form>
  );
}
