import api from './client';
import type { Customer, Lead, PagedResult, User, UserLookup } from '../types';

// Small helpers used on many pages
export async function fetchUserLookup(): Promise<UserLookup[]> {
  const { data } = await api.get<UserLookup[]>('/users/lookup');
  return data;
}

export async function fetchMe(): Promise<User> {
  const { data } = await api.get<User>('/auth/me');
  return data;
}

export interface Option {
  id: number;
  name: string;
}

// Dropdown options for filters (first 100 records the user can see)
export async function fetchCustomerOptions(): Promise<Option[]> {
  const { data } = await api.get<PagedResult<Customer>>('/customers', { params: { pageSize: 100, sortBy: 'customerName', sortOrder: 'asc' } });
  return data.data.map((c) => ({ id: c.id, name: c.customerName }));
}

export async function fetchLeadOptions(): Promise<Option[]> {
  const { data } = await api.get<PagedResult<Lead>>('/leads', { params: { pageSize: 100, sortBy: 'leadName', sortOrder: 'asc' } });
  return data.data.map((l) => ({ id: l.id, name: l.leadName }));
}
