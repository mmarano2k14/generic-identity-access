import type {
  ButtonHTMLAttributes,
  ComponentType,
  InputHTMLAttributes,
  ReactNode,
} from "react";

export type IdentityButtonVariant = "primary" | "secondary" | "danger";

export interface IdentityButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  readonly variant?: IdentityButtonVariant;
}

export interface IdentityInputProps extends InputHTMLAttributes<HTMLInputElement> {}

export interface IdentityPanelVisualProps {
  readonly title?: string | undefined;
  readonly description?: string | undefined;
  readonly children: ReactNode;
}

export interface IdentityTableVisualProps {
  readonly caption?: string | undefined;
  readonly children: ReactNode;
}

/**
 * Optional visual implementation overrides.
 *
 * These components may change rendering only. They do not own authentication,
 * authorization, session, MFA, policy or TRN semantics.
 */
export interface IdentityComponentOverrides {
  readonly Button?: ComponentType<IdentityButtonProps>;
  readonly Input?: ComponentType<IdentityInputProps>;
  readonly Panel?: ComponentType<IdentityPanelVisualProps>;
  readonly Table?: ComponentType<IdentityTableVisualProps>;
}

export const emptyIdentityComponentOverrides: IdentityComponentOverrides = Object.freeze({});
