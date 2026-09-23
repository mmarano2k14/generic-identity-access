import { redirect } from "next/navigation";

export default function AuthenticationCallbackPage() {
  // The host performs OIDC authorization with redirect:"manual" server-side and normally never navigates here.
  redirect("/identity");
}
