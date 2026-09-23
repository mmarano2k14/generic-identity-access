import Link from "next/link";
import type { AdminIconName } from "./AdminIcon";
import { AdminIcon } from "./AdminIcon";

export interface AdminFeatureCardProps {
  readonly href: string;
  readonly icon: AdminIconName;
  readonly title: string;
  readonly description: string;
  readonly eyebrow?: string;
}

/** Server-renderable feature navigation card for the administration overview. */
export function AdminFeatureCard({ href, icon, title, description, eyebrow = "Open workspace" }: AdminFeatureCardProps) {
  return (
    <Link className="ia-feature-card" href={href}>
      <span className="ia-feature-icon"><AdminIcon name={icon} /></span>
      <span className="ia-feature-content">
        <span className="ia-feature-eyebrow">{eyebrow}</span>
        <strong>{title}</strong>
        <span>{description}</span>
      </span>
      <span className="ia-feature-arrow"><AdminIcon name="arrow" /></span>
    </Link>
  );
}
