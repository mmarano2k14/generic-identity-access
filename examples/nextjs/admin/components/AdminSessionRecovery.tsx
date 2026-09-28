"use client";

import { useEffect, useState } from "react";
import { AdminIcon } from "./AdminIcon";
import { refreshIdentityAccessSession } from "./IdentityAccessSessionRefreshClient";

export function AdminSessionRecovery({ returnTo = "/identity" }: { readonly returnTo?: string }) {
  const [failed, setFailed] = useState(false);

  useEffect(() => {
    let cancelled = false;

    const recover = async () => {
      try {
        const response = await refreshIdentityAccessSession();
        if (cancelled) return;

        if (response.ok) {
          window.location.replace(returnTo);
          return;
        }

        if (response.status === 401) {
          window.location.replace("/login?session=expired");
          return;
        }

        setFailed(true);
      } catch {
        if (!cancelled) setFailed(true);
      }
    };

    void recover();
    return () => {
      cancelled = true;
    };
  }, [returnTo]);

  if (failed) {
    return (
      <div className="ia-auth-notice" role="status">
        <AdminIcon name="shield" />
        <span>Session renewal is temporarily unavailable. Reload this page to retry or sign in again.</span>
      </div>
    );
  }

  return (
    <div className="ia-auth-notice" role="status">
      <AdminIcon name="shield" />
      <span>Restoring your secure administrative session…</span>
    </div>
  );
}
