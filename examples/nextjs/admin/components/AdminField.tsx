import { useId, type InputHTMLAttributes, type ReactNode, type SelectHTMLAttributes } from "react";

function mergeDescribedBy(existing: string | undefined, generated: string | undefined): string | undefined {
  return [existing, generated].filter((value): value is string => Boolean(value)).join(" ") || undefined;
}

export function AdminField({ label, hint, ...input }: InputHTMLAttributes<HTMLInputElement> & { readonly label: string; readonly hint?: string }) {
  const hintId = useId();
  const describedBy = mergeDescribedBy(input["aria-describedby"], hint ? hintId : undefined);

  return (
    <label className="ia-field">
      <span className="ia-field-label">{label}</span>
      <input className="ia-input" {...input} aria-describedby={describedBy} />
      {hint ? <small className="ia-field-hint" id={hintId}>{hint}</small> : null}
    </label>
  );
}

export function AdminSelectField({ label, hint, children, ...select }: SelectHTMLAttributes<HTMLSelectElement> & { readonly label: string; readonly hint?: string; readonly children: ReactNode }) {
  const hintId = useId();
  const describedBy = mergeDescribedBy(select["aria-describedby"], hint ? hintId : undefined);

  return (
    <label className="ia-field">
      <span className="ia-field-label">{label}</span>
      <span className="ia-select-wrap"><select className="ia-select" {...select} aria-describedby={describedBy}>{children}</select></span>
      {hint ? <small className="ia-field-hint" id={hintId}>{hint}</small> : null}
    </label>
  );
}

export function AdminCheckboxField({ label, hint, ...input }: InputHTMLAttributes<HTMLInputElement> & { readonly label: string; readonly hint?: string }) {
  const hintId = useId();
  const describedBy = mergeDescribedBy(input["aria-describedby"], hint ? hintId : undefined);

  return (
    <label className="ia-checkbox-field">
      <input type="checkbox" {...input} aria-describedby={describedBy} />
      <span><strong>{label}</strong>{hint ? <small id={hintId}>{hint}</small> : null}</span>
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
