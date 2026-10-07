// Types matching the DTOs returned by the backend (see docs/API.md)

export type Role = 'Admin' | 'Manager' | 'SalesExecutive';

export interface User {
  id: number;
  name: string;
  email: string;
  role: Role;
  roleId: number;
  managerId: number | null;
  managerName: string | null;
  isActive: boolean;
  isLockedOut: boolean;
  lockoutEnd: string | null;
  failedLoginCount: number;
  lastLoginAt: string | null;
  createdAt: string;
}

export interface UserLookup {
  id: number;
  name: string;
  role: Role;
}

export interface Pagination {
  page: number;
  pageSize: number;
  total: number;
  totalPages: number;
}

export interface PagedResult<T> {
  data: T[];
  pagination: Pagination;
}

export interface FieldError {
  field: string;
  message: string;
}

export interface Customer {
  id: number;
  customerCode: string;
  customerName: string;
  email: string;
  phone: string;
  companyName: string | null;
  address: string | null;
  city: string | null;
  state: string | null;
  status: string;
  notes: string | null;
  assignedTo: number;
  assignedToName: string | null;
  createdBy: number;
  createdByName: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface Lead {
  id: number;
  leadCode: string;
  leadName: string;
  email: string | null;
  phone: string | null;
  companyName: string | null;
  source: string;
  status: string;
  priority: string;
  expectedValue: number;
  notes: string | null;
  assignedTo: number;
  assignedToName: string | null;
  createdBy: number;
  convertedCustomerId: number | null;
  convertedCustomerName: string | null;
  convertedAt: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface Opportunity {
  id: number;
  opportunityName: string;
  customerId: number;
  customerName: string | null;
  leadId: number | null;
  leadName: string | null;
  amount: number;
  stage: string;
  probability: number;
  weightedAmount: number;
  expectedCloseDate: string;
  source: string | null;
  status: 'Open' | 'Won' | 'Lost';
  closedAt: string | null;
  notes: string | null;
  assignedTo: number;
  assignedToName: string | null;
  createdBy: number;
  createdAt: string;
  updatedAt: string;
}

export interface FollowUp {
  id: number;
  subject: string;
  followUpType: string;
  followUpDate: string;
  status: string;
  isOverdue: boolean;
  remarks: string | null;
  customerId: number | null;
  customerName: string | null;
  leadId: number | null;
  leadName: string | null;
  opportunityId: number | null;
  opportunityName: string | null;
  assignedTo: number;
  assignedToName: string | null;
  createdBy: number;
  completedAt: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface Activity {
  id: number;
  activityType: string;
  subject: string;
  description: string | null;
  activityDate: string;
  status: string;
  customerId: number | null;
  customerName: string | null;
  leadId: number | null;
  leadName: string | null;
  assignedTo: number;
  assignedToName: string | null;
  createdBy: number;
  createdAt: string;
  updatedAt: string;
}

export interface AuditLog {
  id: number;
  userId: number | null;
  userName: string | null;
  action: string;
  entityName: string;
  recordId: string | null;
  oldValue: string | null;
  newValue: string | null;
  result: string;
  details: string | null;
  ipAddress: string | null;
  createdAt: string;
}
