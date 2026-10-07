// Maps every status/stage used in the app to a bootstrap colour
const COLORS: Record<string, string> = {
  // customer
  Active: 'success',
  Inactive: 'secondary',
  Prospect: 'info',
  // lead
  New: 'primary',
  Contacted: 'info',
  Qualified: 'success',
  Unqualified: 'secondary',
  Converted: 'dark',
  Lost: 'danger',
  // opportunity
  Qualification: 'primary',
  Proposal: 'info',
  Negotiation: 'warning',
  Won: 'success',
  Open: 'primary',
  // follow-up / activity
  Planned: 'primary',
  Completed: 'success',
  Missed: 'danger',
  Cancelled: 'secondary',
  Overdue: 'danger',
  // priority
  High: 'danger',
  Medium: 'warning',
  Low: 'secondary',
  // audit
  Success: 'success',
  Failure: 'danger',
};

export default function StatusBadge({ status }: { status: string | null | undefined }) {
  if (!status) return <span>-</span>;
  const color = COLORS[status] || 'secondary';
  const textClass = color === 'warning' || color === 'info' ? 'text-dark' : '';
  return <span className={`badge bg-${color} ${textClass}`}>{status === 'SalesExecutive' ? 'Sales Executive' : status}</span>;
}
