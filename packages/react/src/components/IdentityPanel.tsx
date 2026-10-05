"use client";

import { useIdentityComponents } from "../hooks/useIdentityComponents";
import type { IdentityPanelVisualProps } from "../visual/types";

export interface IdentityPanelProps extends IdentityPanelVisualProps {}

export function IdentityPanel({ title, description, children }: IdentityPanelProps) {
  const { Panel } = useIdentityComponents();

  if (Panel) {
    return <Panel title={title} description={description}>{children}</Panel>;
  }

  return (
    <section className="gi-panel" data-gi-component="panel">
      {title || description ? (
        <header className="gi-panel-header">
          {title ? <h2 className="gi-panel-title">{title}</h2> : null}
          {description ? <p className="gi-panel-description">{description}</p> : null}
        </header>
      ) : null}
      <div className="gi-panel-content">{children}</div>
    </section>
  );
}
