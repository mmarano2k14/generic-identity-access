import type { ButtonHTMLAttributes, InputHTMLAttributes } from "react";
import type { GenericIdentityClient } from "@generic-identity/auth";
import {
  IdentityProvider,
  IdentityThemeRoot,
  SignInPage,
  type IdentityButtonProps,
  type IdentityComponentOverrides,
  type IdentityInputProps,
} from "@generic-identity/react";

function ConsumerButton({ variant: _variant, ...props }: IdentityButtonProps) {
  return <button {...(props as ButtonHTMLAttributes<HTMLButtonElement>)} data-consumer-button="true" />;
}

function ConsumerInput(props: IdentityInputProps) {
  return <input {...(props as InputHTMLAttributes<HTMLInputElement>)} data-consumer-input="true" />;
}

const overrides: IdentityComponentOverrides = {
  Button: ConsumerButton,
  Input: ConsumerInput,
};

export function ThemeConsumerProbe(props: { readonly client: GenericIdentityClient }) {
  return (
    <IdentityProvider client={props.client} components={overrides}>
      <IdentityThemeRoot className="consumer-identity-theme">
        <SignInPage />
      </IdentityThemeRoot>
    </IdentityProvider>
  );
}
