import { IdentityAccessClientError } from "@identity-access/client";
import type { AdminEntityReferenceKind } from "../../../../contracts/AdminEntityReferenceKind";
import { IdentityAccessAdminEntityReferenceSearchService } from "../../../../server/IdentityAccessAdminEntityReferenceSearchService";
import { IdentityAccessAdminRequest } from "../../../../server/IdentityAccessAdminRequest";

const kinds = new Set<AdminEntityReferenceKind>([
  "user",
  "tenant",
  "tenant-membership",
  "managed-policy",
  "resource-scope",
  "authority-group",
  "authority-policy",
]);

export async function GET(request: Request): Promise<Response> {
  try {
    const url = new URL(request.url);
    const kindValue = url.searchParams.get("kind") ?? "";
    if (!kinds.has(kindValue as AdminEntityReferenceKind)) {
      return Response.json({ message: "Unknown reference kind." }, { status: 400 });
    }

    const adminRequest = await IdentityAccessAdminRequest.fromCurrentRequest();
    const service = new IdentityAccessAdminEntityReferenceSearchService(adminRequest);
    const options = await service.search(
      kindValue as AdminEntityReferenceKind,
      url.searchParams.get("q") ?? "",
      url.searchParams.get("tenantId") ?? undefined,
    );
    return Response.json(options, { status: 200 });
  } catch (error) {
    if (error instanceof RangeError) return Response.json({ message: error.message }, { status: 400 });
    if (error instanceof IdentityAccessClientError) {
      const status = error.httpStatus === 401 || error.httpStatus === 403 || error.httpStatus === 503
        ? error.httpStatus
        : 502;
      return Response.json({ message: error.message }, { status });
    }
    return Response.json({ message: "Reference lookup failed." }, { status: 500 });
  }
}
