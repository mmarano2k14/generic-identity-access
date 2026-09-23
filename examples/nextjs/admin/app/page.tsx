import { redirect } from "next/navigation";
import { IdentityAccessHostSessionService } from "../server/IdentityAccessHostSessionService";

export default async function HomePage() {
  const session = await IdentityAccessHostSessionService.fromCurrentRequest();
  redirect(session.hasBearerCredential() ? "/identity" : "/login");
}
