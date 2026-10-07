// Client side validation helpers.
// Every rule returns an error message or '' when the value is valid.
// The server repeats all of these checks, this is only for quick feedback.

export const PHONE_REGEX = /^[6-9]\d{9}$/;
const EMAIL_REGEX = /^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/;

export type Errors = Record<string, string>;

export function required(value: unknown, label: string): string {
  if (value === null || value === undefined || String(value).trim() === '') return `${label} is required.`;
  return '';
}

export function maxLength(value: string | null | undefined, max: number, label: string): string {
  if (value && value.trim().length > max) return `${label} cannot be more than ${max} characters.`;
  return '';
}

export function minLength(value: string | null | undefined, min: number, label: string): string {
  if (value && value.trim().length < min) return `${label} must be at least ${min} characters.`;
  return '';
}

export function email(value: string | null | undefined): string {
  if (value && !EMAIL_REGEX.test(value.trim())) return 'Enter a valid email address.';
  return '';
}

export function phone(value: string | null | undefined): string {
  if (value && !PHONE_REGEX.test(value.trim())) return 'Enter a valid phone number.';
  return '';
}

export function numberRange(value: unknown, min: number, max: number, message: string): string {
  if (value === '' || value === null || value === undefined) return '';
  const num = Number(value);
  if (Number.isNaN(num) || num < min || num > max) return message;
  return '';
}

export function integer(value: unknown, label: string): string {
  if (value === '' || value === null || value === undefined) return '';
  if (!Number.isInteger(Number(value))) return `${label} must be a whole number.`;
  return '';
}

export function validDate(value: string | null | undefined, label: string): string {
  if (value && Number.isNaN(new Date(value).getTime())) return `Enter a valid ${label.toLowerCase()}.`;
  return '';
}

// today's date as YYYY-MM-DD in local time (used for min attribute of date inputs too)
export function todayString(): string {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}

export function notPastDate(value: string | null | undefined, message: string): string {
  if (value && value < todayString()) return message;
  return '';
}

export function passwordPolicy(value: string): string {
  if (!value) return 'Password is required.';
  if (value.length < 8) return 'Password must be at least 8 characters.';
  if (value.length > 64) return 'Password cannot be more than 64 characters.';
  if (!/[A-Z]/.test(value)) return 'Password must contain at least one uppercase letter.';
  if (!/[a-z]/.test(value)) return 'Password must contain at least one lowercase letter.';
  if (!/\d/.test(value)) return 'Password must contain at least one number.';
  if (!/[^A-Za-z0-9]/.test(value)) return 'Password must contain at least one special character.';
  return '';
}

// Takes { field: [rule results] } and keeps the first error of every field
export function collectErrors(checks: Record<string, string[]>): Errors {
  const errors: Errors = {};
  for (const [field, results] of Object.entries(checks)) {
    const first = results.find((r) => r);
    if (first) errors[field] = first;
  }
  return errors;
}
