import type { InputHTMLAttributes, ReactNode, SelectHTMLAttributes } from "react";

export function AdminField({ label, hint, ...input }: InputHTMLAttributes<HTMLInputElement> & { readonly label: string; readonly hint?: string }) {
  return (
    <label className="ia-field">
      <span className="ia-field-label">{label}</span>
      <input className="ia-input" {...input} />
      {hint ? <small className="ia-field-hint">{hint}</small> : null}
    </label>
  );
}

export function AdminSelectField({ label, hint, children, ...select }: SelectHTMLAttributes<HTMLSelectElement> & { readonly label: string; readonly hint?: string; readonly children: ReactNode }) {
  return (
    <label className="ia-field">
      <span className="ia-field-label">{label}</span>
      <span className="ia-select-wrap"><select className="ia-select" {...select}>{children}</select></span>
      {hint ? <small className="ia-field-hint">{hint}</small> : null}
    </label>
  );
}

export function AdminCheckboxField({ label, hint, ...input }: InputHTMLAttributes<HTMLInputElement> & { readonly label: string; readonly hint?: string }) {
  return (
    <label className="ia-checkbox-field">
      <input type="checkbox" {...input} />
      <span><strong>{label}</strong>{hint ? <small>{hint}</small> : null}</span>
    </label>
  );
}

export function AdminStatusField(props: Omit<SelectHTMLAttributes<HTMLSelectElement>, "children">) {
  return (
    <AdminSelectField label="Status" defaultValue="1" {...props}>
      <option value="1">Active</option>
      <option value="2">Inactive</option>
    </AdminSelectField>
  );
}
