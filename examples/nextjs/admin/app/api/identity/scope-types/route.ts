import { IdentityAccessClientError } from "@identity-access/client";
import { IdentityAccessAdminRequest } from "../../../../server/IdentityAccessAdminRequest";

export async function GET(request: Request): Promise<Response> {
  try {
    const url = new URL(request.url);
    const rawModelVersion = url.searchParams.get("modelVersion") ?? "";
    if (!/^\d+$/.test(rawModelVersion)) {
      return Response.json({ message: "A positive security-model version is required." }, { status: 400 });
    }

    const modelVersion = Number(rawModelVersion);
    if (!Number.isSafeInteger(modelVersion) || modelVersion <= 0) {
      return Response.json({ message: "A positive security-model version is required." }, { status: 400 });
    }

    const adminRequest = await IdentityAccessAdminRequest.fromCurrentRequest();
    const scopeTypes = await adminRequest.client.administration.securityModels.listScopeTypes(
      adminRequest.administrationContext,
      modelVersion,
    );

    return Response.json(scopeTypes, { status: 200 });
  } catch (error) {
    if (error instanceof IdentityAccessClientError) {
      const status = error.httpStatus === 401 || error.httpStatus === 403 || error.httpStatus === 404 || error.httpStatus === 503
        ? error.httpStatus
        : 502;
      return Response.json({ message: error.message }, { status });
    }
    return Response.json({ message: "Scope-type lookup failed." }, { status: 500 });
  }
}
