"use client";

import { useEffect, useRef } from "react";
import { refreshIdentityAccessSession } from "./IdentityAccessSessionRefreshClient";

const refreshCheckIntervalMs = 60_000;

/**
 * Keeps the server-owned admin session alive while the protected workspace is open.
 * The endpoint only rotates when the access token is near expiry, so the one-minute
 * browser check does not imply one-minute refresh-token rotation.
 */
export function AdminSessionRefresh() {
  const stopped = useRef(false);

  useEffect(() => {
    stopped.current = false;

    const check = async () => {
      if (stopped.current || document.visibilityState === "hidden") return;

      try {
        const response = await refreshIdentityAccessSession();
        if (stopped.current) return;

        if (response.status === 401) {
          window.location.replace("/login?session=expired");
        }
      } catch {
        // Keep the current session state on transient browser/network failure.
        // The next interval/focus event retries without destroying the refresh token.
      }
    };

    const onVisible = () => {
      if (document.visibilityState === "visible") void check();
    };
    const onFocus = () => void check();
    const onPageShow = () => void check();

    void check();
    const interval = window.setInterval(() => void check(), refreshCheckIntervalMs);
    document.addEventListener("visibilitychange", onVisible);
    window.addEventListener("focus", onFocus);
    window.addEventListener("pageshow", onPageShow);

    return () => {
      stopped.current = true;
      window.clearInterval(interval);
      document.removeEventListener("visibilitychange", onVisible);
      window.removeEventListener("focus", onFocus);
      window.removeEventListener("pageshow", onPageShow);
    };
  }, []);

  return null;
}
