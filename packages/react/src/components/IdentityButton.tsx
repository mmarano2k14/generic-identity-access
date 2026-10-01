"use client";

import { useIdentityComponents } from "../hooks/useIdentityComponents";
import type { IdentityButtonProps } from "../visual/types";

function joinClasses(...classes: Array<string | undefined | false>): string {
  return classes.filter(Boolean).join(" ");
}

export function IdentityButton({
  variant = "secondary",
  className,
  type = "button",
  ...props
}: IdentityButtonProps) {
  const { Button } = useIdentityComponents();
  const resolvedClassName = joinClasses(
    "gi-button",
    `gi-button-${variant}`,
    className,
  );

  if (Button) {
    return (
      <Button
        {...props}
        type={type}
        variant={variant}
        className={resolvedClassName}
      />
    );
  }

  return (
    <button
      {...props}
      type={type}
      className={resolvedClassName}
      data-gi-component="button"
      data-gi-variant={variant}
    />
  );
}
