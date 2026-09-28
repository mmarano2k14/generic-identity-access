import { IdentityAccessClientError } from "@identity-access/client";
import { IdentityAccessAdminRequest } from "../../../../server/IdentityAccessAdminRequest";

/** Exact-login membership candidate lookup. This route never exposes partial/global directory search. */
export async function GET(request: Request): Promise<Response> {
  try {
    const url = new URL(request.url);
    const tenantId = url.searchParams.get("tenantId")?.trim() ?? "";
    const loginIdentifier = url.searchParams.get("loginIdentifier")?.trim() ?? "";
    if (!tenantId || !loginIdentifier || loginIdentifier.length > 320) {
      return Response.json({ message: "Tenant and exact login identifier are required." }, { status: 400 });
    }

    const adminRequest = await IdentityAccessAdminRequest.fromCurrentRequest();
    const candidate = await adminRequest.client.administration.membershipCandidates.findByLogin(
      adminRequest.tenantContextFor(tenantId),
      loginIdentifier,
    );
    if (candidate === null) {
      return Response.json({ message: "No eligible account found." }, { status: 404 });
    }
    return Response.json(candidate, { status: 200 });
  } catch (error) {
    if (error instanceof IdentityAccessClientError) {
      const status = error.httpStatus === 401 || error.httpStatus === 403 || error.httpStatus === 503
        ? error.httpStatus
        : 502;
      return Response.json({ message: error.message }, { status });
    }
    return Response.json({ message: "Membership candidate lookup failed." }, { status: 500 });
  }
}
