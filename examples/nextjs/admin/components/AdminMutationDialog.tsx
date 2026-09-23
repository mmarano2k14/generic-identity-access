"use client";

import { useActionState, useEffect, useRef, type ReactNode } from "react";
import { INITIAL_ADMIN_ACTION_STATE, type AdminActionState } from "../contracts/AdminActionState";
import { AdminIcon } from "./AdminIcon";

export type AdminServerAction = (state: AdminActionState, formData: FormData) => Promise<AdminActionState>;

export interface AdminMutationDialogProps {
  readonly title: string;
  readonly description: string;
  readonly triggerLabel: string;
  readonly submitLabel: string;
  readonly action: AdminServerAction;
  readonly children: ReactNode;
  readonly dangerous?: boolean;
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
}: AdminMutationDialogProps) {
  const dialog = useRef<HTMLDialogElement>(null);
  const [state, formAction, pending] = useActionState(action, INITIAL_ADMIN_ACTION_STATE);

  useEffect(() => {
    if (state.status === "success") dialog.current?.close();
  }, [state]);

  return (
    <div className="ia-admin-action">
      <button className={dangerous ? "ia-button ia-button-danger" : "ia-button ia-button-primary"} type="button" onClick={() => dialog.current?.showModal()}>
        <AdminIcon name={dangerous ? "lock" : "plus"} />
        {triggerLabel}
      </button>
      {state.status === "success" && state.message ? <p className="ia-feedback ia-feedback-success" role="status">{state.message}</p> : null}
      <dialog className={`ia-dialog ${dangerous ? "ia-dialog-danger" : ""}`} ref={dialog} onCancel={() => dialog.current?.close()}>
        <form action={formAction} className="ia-form">
          <header className="ia-dialog-header">
            <span className={`ia-dialog-icon ${dangerous ? "ia-dialog-icon-danger" : ""}`}><AdminIcon name={dangerous ? "lock" : "spark"} /></span>
            <div className="ia-dialog-title">
              <p className="ia-eyebrow">{dangerous ? "Security-sensitive change" : "Identity Access"}</p>
              <h2>{title}</h2>
              <p>{description}</p>
            </div>
            <button className="ia-icon-button" type="button" aria-label="Close dialog" onClick={() => dialog.current?.close()}>×</button>
          </header>
          <div className="ia-form-body">{children}</div>
          {state.status === "error" && state.message ? <p className="ia-feedback ia-feedback-error" role="alert">{state.message}</p> : null}
          <footer className="ia-dialog-footer">
            <span className="ia-dialog-security"><AdminIcon name="shield" />Server authorization is re-evaluated on submit.</span>
            <div className="ia-dialog-actions">
              <button className="ia-button ia-button-secondary" type="button" onClick={() => dialog.current?.close()} disabled={pending}>Cancel</button>
              <button className={dangerous ? "ia-button ia-button-danger" : "ia-button ia-button-primary"} type="submit" disabled={pending}>
                {pending ? "Working…" : submitLabel}
              </button>
            </div>
          </footer>
        </form>
      </dialog>
    </div>
  );
}
