/**
 * The `errors` of a `400` validation problem, keyed in camelCase. FluentValidation names properties in PascalCase
 * (`Slug`, `Fields[0].Label`) while the supporting services use camelCase (`values.fld_title`), so the keys are
 * lowered at the start of every segment and forms can look them up one way.
 */
export function validationErrors(problem: unknown): Record<string, string[]> {
  const errors = (problem as { errors?: Record<string, string[]> } | undefined)?.errors ?? {}

  return Object.fromEntries(Object.entries(errors).map(([key, messages]) => [camelCaseKey(key), messages]))
}

function camelCaseKey(key: string) {
  return key.replace(/(^|\.)([A-Z])/g, (_, dot: string, c: string) => dot + c.toLowerCase())
}
