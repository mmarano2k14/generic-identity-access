import type { InputHTMLAttributes, SelectHTMLAttributes } from "react";

export function AdminTextField({ label, hint, ...input }: InputHTMLAttributes<HTMLInputElement> & { readonly label: string; readonly hint?: string }) {
  return (
    <label className="ia-field">
      <span className="ia-field-label">{label}</span>
      <input className="ia-input" {...input} />
      {hint ? <small className="ia-field-hint">{hint}</small> : null}
    </label>
  );
}

export function AdminStatusField(props: Omit<SelectHTMLAttributes<HTMLSelectElement>, "children">) {
  return (
    <label className="ia-field">
      <span className="ia-field-label">Status</span>
      <select className="ia-select" defaultValue="1" {...props}>
        <option value="1">Active</option>
        <option value="2">Inactive</option>
      </select>
    </label>
  );
}
