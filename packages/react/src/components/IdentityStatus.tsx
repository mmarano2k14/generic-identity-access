export interface IdentityStatusProps {
  readonly label: string;
  readonly tone?: "positive" | "negative" | "neutral" | "warning";
}

export function IdentityStatus({ label, tone = "neutral" }: IdentityStatusProps) {
  return (
    <span className={`gi-status gi-status-${tone}`} data-gi-component="status" data-gi-tone={tone}>
      {label}
    </span>
  );
}
