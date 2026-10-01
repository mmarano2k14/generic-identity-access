"use client";

import { useIdentityComponents } from "../hooks/useIdentityComponents";
import type { IdentityTableVisualProps } from "../visual/types";

export interface IdentityTableProps extends IdentityTableVisualProps {}

export function IdentityTable({ caption, children }: IdentityTableProps) {
  const { Table } = useIdentityComponents();

  if (Table) {
    return <Table caption={caption}>{children}</Table>;
  }

  return (
    <div className="gi-table-shell" data-gi-component="table">
      <table className="gi-table">
        {caption ? <caption className="gi-table-caption">{caption}</caption> : null}
        {children}
      </table>
    </div>
  );
}
