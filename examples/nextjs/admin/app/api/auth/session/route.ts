import { IdentityAccessClientError } from "@identity-access/client";
import { IdentityAccessHostSessionService } from "../../../../server/IdentityAccessHostSessionService";

export const dynamic = "force-dynamic";

const noStoreHeaders = {
  "Cache-Control": "private, no-store, max-age=0",
  "Pragma": "no-cache",
};

/**
 * Browser-visible session maintenance endpoint. Tokens remain HTTP-only and server-owned;
 * the response communicates status only.
 */
export async function POST(): Promise<Response> {
  try {
    const session = await IdentityAccessHostSessionService.fromCurrentRequest();
    const result = await session.refreshIfNeeded(120);

    if (result === "expired") {
      return Response.json({ error: "session_expired" }, { status: 401, headers: noStoreHeaders });
    }

    return new Response(null, { status: 204, headers: noStoreHeaders });
  } catch (error) {
    if (error instanceof IdentityAccessClientError) {
      if (error.code === "unavailable" || error.code === "timeout" || error.code === "transport") {
        return Response.json(
          { error: "session_refresh_temporarily_unavailable" },
          { status: 503, headers: { ...noStoreHeaders, "Retry-After": "5" } },
        );
      }
      if (error.code === "configuration") {
        return Response.json({ error: "session_refresh_configuration" }, { status: 500, headers: noStoreHeaders });
      }
    }

    return Response.json({ error: "session_refresh_failed" }, { status: 502, headers: noStoreHeaders });
  }
}
