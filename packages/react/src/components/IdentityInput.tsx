"use client";

import { useIdentityComponents } from "../hooks/useIdentityComponents";
import type { IdentityInputProps } from "../visual/types";

function joinClasses(...classes: Array<string | undefined | false>): string {
  return classes.filter(Boolean).join(" ");
}

export function IdentityInput({ className, ...props }: IdentityInputProps) {
  const { Input } = useIdentityComponents();
  const resolvedClassName = joinClasses("gi-input", className);

  if (Input) {
    return <Input {...props} className={resolvedClassName} />;
  }

  return <input {...props} className={resolvedClassName} data-gi-component="input" />;
}
