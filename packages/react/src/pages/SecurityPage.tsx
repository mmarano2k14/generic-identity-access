import type { ReactNode } from "react";
import { IdentityPageFrame } from "../components/index";

export interface SecurityPageProps {
  readonly children: ReactNode;
  readonly actions?: ReactNode;
}

/** Composition shell for consumer-selected session, MFA and recovery panels. */
export function SecurityPage({ children, actions }: SecurityPageProps) {
  return (
    <IdentityPageFrame title="Security" description="Account security, sessions and authentication factors." actions={actions}>
      <div className="gi-security-sections" data-gi-component="security-sections">{children}</div>
    </IdentityPageFrame>
  );
}
