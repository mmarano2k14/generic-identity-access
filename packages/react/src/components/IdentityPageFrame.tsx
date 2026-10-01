import type { ReactNode } from "react";

export interface IdentityPageFrameProps {
  readonly title: string;
  readonly description?: string;
  readonly actions?: ReactNode;
  readonly children: ReactNode;
}

/** Stable semantic shell used by every shared Identity page. */
export function IdentityPageFrame({
  title,
  description,
  actions,
  children,
}: IdentityPageFrameProps) {
  return (
    <section className="gi-page" data-gi-component="page">
      <header className="gi-page-header">
        <div className="gi-page-heading">
          <h1 className="gi-page-title">{title}</h1>
          {description ? <p className="gi-page-description">{description}</p> : null}
        </div>
        {actions ? <div className="gi-page-actions">{actions}</div> : null}
      </header>
      <div className="gi-page-content">{children}</div>
    </section>
  );
}
