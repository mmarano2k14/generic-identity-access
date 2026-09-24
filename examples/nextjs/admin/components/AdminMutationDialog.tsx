"use client";

import { useActionState, useEffect, useId, useRef, type ReactNode } from "react";
import { INITIAL_ADMIN_ACTION_STATE, type AdminActionState } from "../contracts/AdminActionState";
import { AdminIcon, type AdminIconName } from "./AdminIcon";

export type AdminServerAction = (state: AdminActionState, formData: FormData) => Promise<AdminActionState>;

export interface AdminMutationDialogProps {
  readonly title: string;
  readonly description: string;
  readonly triggerLabel: string;
  readonly submitLabel: string;
  readonly action: AdminServerAction;
  readonly children: ReactNode;
  readonly dangerous?: boolean;
  readonly triggerVariant?: "primary" | "secondary" | "danger";
  readonly triggerIcon?: AdminIconName;
  readonly compact?: boolean;
}

/** Client Component for controlled administration mutations; credentials and API calls remain server-side. */
export function AdminMutationDialog({
  title,
  description,
  triggerLabel,
  submitLabel,
  action,
  children,
  dangerous = false,
  triggerVariant,
  triggerIcon,
  compact = false,
}: AdminMutationDialogProps) {
  const dialog = useRef<HTMLDialogElement>(null);
  const titleId = useId();
  const descriptionId = useId();
  const [state, formAction, pending] = useActionState(action, INITIAL_ADMIN_ACTION_STATE);
  const variant = triggerVariant ?? (dangerous ? "danger" : "primary");
  const icon = triggerIcon ?? (dangerous ? "trash" : "plus");

  useEffect(() => {
    if (state.status === "success") dialog.current?.close();
  }, [state]);

  return (
    <div className="ia-admin-action">
      <button className={`ia-button ia-button-${variant}${compact ? " ia-button-compact" : ""}`} type="button" onClick={() => dialog.current?.showModal()}>
        <AdminIcon name={icon} />
        {triggerLabel}
      </button>
      {state.status === "success" && state.message ? <p className="ia-feedback ia-feedback-success" role="status">{state.message}</p> : null}
      <dialog
        className={`ia-dialog ${dangerous ? "ia-dialog-danger" : ""}`}
        ref={dialog}
        aria-labelledby={titleId}
        aria-describedby={descriptionId}
        onCancel={() => dialog.current?.close()}
      >
        <form action={formAction} className="ia-form" aria-busy={pending}>
          <header className="ia-dialog-header">
            <span className={`ia-dialog-icon ${dangerous ? "ia-dialog-icon-danger" : ""}`}><AdminIcon name={dangerous ? "lock" : triggerIcon ?? "spark"} /></span>
            <div className="ia-dialog-title">
              <p className="ia-eyebrow">{dangerous ? "Security-sensitive change" : "Identity Access"}</p>
              <h2 id={titleId}>{title}</h2>
              <p id={descriptionId}>{description}</p>
            </div>
            <button className="ia-icon-button" type="button" aria-label="Close dialog" onClick={() => dialog.current?.close()} disabled={pending}>×</button>
          </header>
          <div className="ia-form-body">
            {dangerous ? (
              <div className="ia-destructive-notice" role="note">
                <AdminIcon name="shield" />
                <div><strong>Confirm the exact relationship or session change.</strong><span>This action is submitted to the server and authorization is evaluated again before any mutation is accepted.</span></div>
              </div>
            ) : null}
            {children}
          </div>
          {state.status === "error" && state.message ? <p className="ia-feedback ia-feedback-error" role="alert">{state.message}</p> : null}
          <footer className="ia-dialog-footer">
            <span className="ia-dialog-security"><AdminIcon name="shield" />Server authorization is re-evaluated on submit.</span>
            <div className="ia-dialog-actions">
              <button className="ia-button ia-button-secondary" type="button" onClick={() => dialog.current?.close()} disabled={pending}>Cancel</button>
              <button className={dangerous ? "ia-button ia-button-danger" : "ia-button ia-button-primary"} type="submit" disabled={pending}>
                <AdminIcon name={dangerous ? "trash" : "check"} />
                {pending ? "Working…" : submitLabel}
              </button>
            </div>
          </footer>
        </form>
      </dialog>
    </div>
  );
}
