import type { ComponentType } from "react";
import type { ApplicationSecurityModelsPageProps } from "@generic-identity/next/application-security";
import { ApplicationSecurityModelsPage } from "@generic-identity/next/application-security";

export function acceptNextApplicationSecurity(): ComponentType<ApplicationSecurityModelsPageProps> {
  return ApplicationSecurityModelsPage;
}
