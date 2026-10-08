import "server-only";

export function requiredAdministrationText(
  formData: FormData,
  field: string,
  maxLength: number,
): string {
  const value = formData.get(field);
  if (typeof value !== "string") {
    throw new Error(`Invalid administration input: ${field} is required.`);
  }

  const normalized = value.trim();
  if (!normalized || normalized.length > maxLength) {
    throw new Error(
      `Invalid administration input: ${field} must contain between 1 and ${maxLength} characters.`,
    );
  }

  return normalized;
}

export function positiveAdministrationInteger(
  formData: FormData,
  field: string,
): number {
  const text = requiredAdministrationText(formData, field, 16);
  const value = Number(text);

  if (!Number.isSafeInteger(value) || value <= 0) {
    throw new Error(
      `Invalid administration input: ${field} must be a positive integer.`,
    );
  }

  return value;
}

export function administrationLifecycleStatus(
  formData: FormData,
  field: string,
): 1 | 2 {
  const raw = formData.get(field);
  if (raw === null || raw === "" || raw === "1") return 1;
  if (raw === "2") return 2;

  throw new Error(
    `Invalid administration input: ${field} must be Active or Inactive.`,
  );
}
