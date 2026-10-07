import axios, { AxiosError } from 'axios';
import type { FieldError } from '../types';

// Authentication uses the ASP.NET Core Identity cookie (HttpOnly, sent automatically by the browser).
// For CSRF protection the server puts an anti-forgery token in the XSRF-TOKEN cookie and
// axios sends it back in the X-XSRF-TOKEN header on every request (same origin only).
const api = axios.create({
  baseURL: '/api',
  headers: { 'Content-Type': 'application/json' },
  withCredentials: true,
  xsrfCookieName: 'XSRF-TOKEN',
  xsrfHeaderName: 'X-XSRF-TOKEN',
});

// Gets a fresh anti-forgery token. Must be called on start up and after login / logout,
// because the token is linked to the logged in user.
export async function refreshCsrfToken() {
  await api.get('/auth/csrf');
}

// Session expired -> back to the login page
api.interceptors.response.use(
  (response) => response,
  (error: AxiosError) => {
    const url = error.config?.url || '';
    const isAuthCall = ['/auth/login', '/auth/me', '/auth/logout'].some((path) => url.includes(path));
    if (error.response?.status === 401 && !isAuthCall) {
      const publicPages = ['/login', '/register'];
      if (!publicPages.includes(window.location.pathname)) {
        window.location.href = '/login?expired=1';
      }
    }
    return Promise.reject(error);
  },
);

interface ApiErrorBody {
  message?: string;
  errors?: FieldError[];
}

// Friendly message to show in toast/alert
export function getErrorMessage(error: unknown, fallback = 'Something went wrong. Please try again.'): string {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data as ApiErrorBody | undefined;
    if (data?.message) return data.message;
    if (!error.response) return 'Unable to reach the server. Please check your connection.';
  }
  return fallback;
}

// Field wise errors from the server -> { email: 'Enter a valid email address.' }
export function getFieldErrors(error: unknown): Record<string, string> {
  const result: Record<string, string> = {};
  if (axios.isAxiosError(error)) {
    const data = error.response?.data as ApiErrorBody | undefined;
    data?.errors?.forEach((e) => {
      if (!result[e.field]) result[e.field] = e.message;
    });
  }
  return result;
}

// Downloads a CSV report using the same auth header
export async function downloadFile(url: string, params: Record<string, unknown>, fileName: string) {
  const response = await api.get(url, { params: { ...params, format: 'csv' }, responseType: 'blob' });
  const blobUrl = window.URL.createObjectURL(response.data as Blob);
  const link = document.createElement('a');
  link.href = blobUrl;
  link.download = fileName;
  link.click();
  window.URL.revokeObjectURL(blobUrl);
}

// Removes empty filter values so the query string stays clean
export function cleanParams(params: Record<string, unknown>) {
  const result: Record<string, unknown> = {};
  Object.entries(params).forEach(([key, value]) => {
    if (value !== '' && value !== null && value !== undefined) result[key] = value;
  });
  return result;
}

export default api;
