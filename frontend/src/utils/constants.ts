// Same values as backend/src/utils/constants.ts

export const ROLES = {
  ADMIN: 'Admin',
  MANAGER: 'Manager',
  SALES_EXECUTIVE: 'SalesExecutive',
} as const;

export const ROLE_LABELS: Record<string, string> = {
  Admin: 'Admin',
  Manager: 'Manager',
  SalesExecutive: 'Sales Executive',
};

export const CUSTOMER_STATUSES = ['Active', 'Inactive', 'Prospect'];

export const LEAD_STATUSES = ['New', 'Contacted', 'Qualified', 'Unqualified', 'Converted', 'Lost'];
export const LEAD_SOURCES = ['Website', 'Referral', 'Social Media', 'Email Campaign', 'Cold Call', 'Event', 'Other'];
export const LEAD_PRIORITIES = ['Low', 'Medium', 'High'];
export const LEAD_MAX_EXPECTED_VALUE = 100000000;

export const LEAD_STATUS_TRANSITIONS: Record<string, string[]> = {
  New: ['Contacted', 'Unqualified', 'Lost'],
  Contacted: ['Qualified', 'Unqualified', 'Lost'],
  Qualified: ['Lost'],
  Unqualified: ['Contacted', 'Lost'],
  Converted: [],
  Lost: [],
};

export const OPPORTUNITY_STAGES = ['Qualification', 'Proposal', 'Negotiation', 'Won', 'Lost'];
export const OPEN_STAGES = ['Qualification', 'Proposal', 'Negotiation'];

export const FOLLOWUP_TYPES = ['Call', 'Meeting', 'Email', 'Visit', 'Other'];
export const FOLLOWUP_STATUSES = ['Planned', 'Completed', 'Missed', 'Cancelled'];

export const ACTIVITY_TYPES = ['Call', 'Meeting', 'Email', 'Task'];
export const ACTIVITY_STATUSES = ['Planned', 'Completed', 'Cancelled'];

export const PAGE_SIZES = [10, 25, 50];
