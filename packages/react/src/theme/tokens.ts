/** Public CSS custom-property names supported by the Generic Identity theme contract. */
export const identityThemeTokenNames = [
  "--gi-background",
  "--gi-surface",
  "--gi-surface-muted",
  "--gi-text",
  "--gi-muted",
  "--gi-border",
  "--gi-focus",
  "--gi-positive",
  "--gi-negative",
  "--gi-warning",
  "--gi-primary",
  "--gi-primary-contrast",
  "--gi-danger",
  "--gi-danger-contrast",
  "--gi-radius-sm",
  "--gi-radius-md",
  "--gi-space-xs",
  "--gi-space-sm",
  "--gi-space-md",
  "--gi-space-lg",
] as const;

export type IdentityThemeTokenName = (typeof identityThemeTokenNames)[number];
