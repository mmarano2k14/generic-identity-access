"use client";

const refreshLockName = "identity-access-admin-session-refresh";
let inFlight: Promise<Response> | undefined;

type BrowserLockManager = {
  request<T>(name: string, callback: () => Promise<T>): Promise<T>;
};

async function issueRefreshRequest(): Promise<Response> {
  return fetch("/api/auth/session", {
    method: "POST",
    credentials: "same-origin",
    cache: "no-store",
    headers: {
      "X-Identity-Access-Session-Refresh": "1",
    },
  });
}

async function issueSerializedRefreshRequest(): Promise<Response> {
  const navigatorWithLocks = navigator as Navigator & { readonly locks?: BrowserLockManager };
  if (navigatorWithLocks.locks !== undefined) {
    return navigatorWithLocks.locks.request(refreshLockName, issueRefreshRequest);
  }
  return issueRefreshRequest();
}

/**
 * Refreshes the server-owned OIDC session without exposing any token to browser code.
 * A Web Lock serializes rotation across tabs because refresh-token replay revokes the family.
 */
export function refreshIdentityAccessSession(): Promise<Response> {
  if (inFlight !== undefined) return inFlight;

  const current = issueSerializedRefreshRequest().finally(() => {
    if (inFlight === current) inFlight = undefined;
  });
  inFlight = current;
  return current;
}
