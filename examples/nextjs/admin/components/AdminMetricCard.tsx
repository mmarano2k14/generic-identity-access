import type { AdminIconName } from "./AdminIcon";
import { AdminIcon } from "./AdminIcon";

export interface AdminMetricCardProps {
  readonly icon: AdminIconName;
  readonly label: string;
  readonly value: string;
  readonly description: string;
  readonly tone?: "default" | "accent" | "success" | "warning";
}

/** Server-renderable overview metric with no authorization semantics. */
export function AdminMetricCard({ icon, label, value, description, tone = "default" }: AdminMetricCardProps) {
  return (
    <article className={`ia-metric-card ia-metric-${tone}`}>
      <div className="ia-metric-icon"><AdminIcon name={icon} /></div>
      <div className="ia-metric-copy">
        <span className="ia-metric-label">{label}</span>
        <strong className="ia-metric-value">{value}</strong>
        <span className="ia-metric-description">{description}</span>
      </div>
    </article>
  );
}
