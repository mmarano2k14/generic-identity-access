import { redirect } from "next/navigation";
import { IdentityAccessHostSessionService } from "../server/IdentityAccessHostSessionService";

export default async function HomePage() {
  const session = await IdentityAccessHostSessionService.fromCurrentRequest();
  if (session.hasBearerCredential()) redirect("/identity");
  redirect(session.hasRefreshCredential() ? "/login?session=refresh" : "/login");
}
