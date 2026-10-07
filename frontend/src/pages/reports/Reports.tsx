import { useEffect, useState } from 'react';
import { fetchUserLookup } from '../../api/services';
import PageHeader from '../../components/PageHeader';
import StatusBadge from '../../components/StatusBadge';
import { useAuth } from '../../context/AuthContext';
import type { Role, UserLookup } from '../../types';
import { CUSTOMER_STATUSES, FOLLOWUP_STATUSES, LEAD_SOURCES, LEAD_STATUSES, OPPORTUNITY_STAGES, ROLE_LABELS } from '../../utils/constants';
import { formatCurrency, formatDate, formatDateTime } from '../../utils/format';
import ConversionReport from './ConversionReport';
import PipelineReport from './PipelineReport';
import ReportTable, { type ReportFilter } from './ReportTable';

interface Tab {
  key: string;
  label: string;
  icon: string;
  roles?: Role[];
}

const TABS: Tab[] = [
  { key: 'customers', label: 'Customers', icon: 'bi-people' },
  { key: 'leads', label: 'Leads', icon: 'bi-person-plus' },
  { key: 'followups', label: 'Follow-Ups', icon: 'bi-calendar-check' },
  { key: 'opportunities', label: 'Opportunities', icon: 'bi-briefcase' },
  { key: 'pipeline', label: 'Pipeline', icon: 'bi-kanban' },
  { key: 'conversion', label: 'Conversion', icon: 'bi-arrow-left-right' },
  { key: 'user-activity', label: 'User Activity', icon: 'bi-person-lines-fill', roles: ['Admin', 'Manager'] },
  { key: 'audit', label: 'Audit', icon: 'bi-journal-text', roles: ['Admin'] },
];

const toOptions = (values: string[]) => values.map((v) => ({ value: v, label: v }));
const text = (value: unknown) => (value === null || value === undefined || value === '' ? '-' : String(value));

export default function Reports() {
  const { hasRole } = useAuth();
  const [activeTab, setActiveTab] = useState('customers');
  const [users, setUsers] = useState<UserLookup[]>([]);

  useEffect(() => {
    fetchUserLookup().then(setUsers).catch(() => setUsers([]));
  }, []);

  const visibleTabs = TABS.filter((t) => !t.roles || hasRole(...t.roles));

  // owner filter only makes sense when the user can see more than himself
  const ownerFilter: ReportFilter[] =
    users.length > 1 ? [{ name: 'assignedTo', label: 'Owner', options: users.map((u) => ({ value: String(u.id), label: u.name })) }] : [];

  const renderTab = () => {
    switch (activeTab) {
      case 'customers':
        return (
          <ReportTable
            key="customers"
            endpoint="/reports/customers"
            fileName="customer-report.csv"
            filters={[{ name: 'status', label: 'Status', options: toOptions(CUSTOMER_STATUSES) }, ...ownerFilter]}
            columns={[
              { key: 'customerCode', label: 'Code', sortable: true },
              { key: 'customerName', label: 'Customer', sortable: true },
              { key: 'companyName', label: 'Company', sortable: true, render: (r) => text(r.companyName) },
              { key: 'email', label: 'Email' },
              { key: 'phone', label: 'Phone' },
              { key: 'status', label: 'Status', sortable: true, render: (r) => <StatusBadge status={String(r.status)} /> },
              { key: 'assignedToName', label: 'Owner' },
              { key: 'createdAt', label: 'Created', sortable: true, render: (r) => formatDate(String(r.createdAt)) },
            ]}
          />
        );
      case 'leads':
        return (
          <ReportTable
            key="leads"
            endpoint="/reports/leads"
            fileName="lead-report.csv"
            filters={[
              { name: 'status', label: 'Status', options: toOptions(LEAD_STATUSES) },
              { name: 'source', label: 'Source', options: toOptions(LEAD_SOURCES) },
              ...ownerFilter,
            ]}
            columns={[
              { key: 'leadCode', label: 'Code', sortable: true },
              { key: 'leadName', label: 'Lead', sortable: true },
              { key: 'companyName', label: 'Company', render: (r) => text(r.companyName) },
              { key: 'source', label: 'Source', sortable: true },
              { key: 'status', label: 'Status', sortable: true, render: (r) => <StatusBadge status={String(r.status)} /> },
              { key: 'priority', label: 'Priority', render: (r) => <StatusBadge status={String(r.priority)} /> },
              { key: 'expectedValue', label: 'Expected Value', sortable: true, className: 'text-end', render: (r) => formatCurrency(Number(r.expectedValue)) },
              { key: 'assignedToName', label: 'Owner' },
              {
                key: 'converted',
                label: 'Converted',
                render: (r) => (r.converted ? <span className="text-success">Yes ({formatDate(String(r.convertedAt))})</span> : 'No'),
              },
              { key: 'createdAt', label: 'Created', sortable: true, render: (r) => formatDate(String(r.createdAt)) },
            ]}
          />
        );
      case 'followups':
        return (
          <ReportTable
            key="followups"
            endpoint="/reports/followups"
            fileName="followup-report.csv"
            defaultSort="followUpDate"
            dateLabel="Follow-Up"
            filters={[{ name: 'status', label: 'Status', options: toOptions([...FOLLOWUP_STATUSES, 'Overdue']) }, ...ownerFilter]}
            columns={[
              { key: 'followUpDate', label: 'Date', sortable: true, render: (r) => formatDate(String(r.followUpDate)) },
              { key: 'subject', label: 'Subject', sortable: true },
              { key: 'followUpType', label: 'Type' },
              { key: 'relatedTo', label: 'Related To', render: (r) => text(r.relatedTo) },
              {
                key: 'status',
                label: 'Status',
                sortable: true,
                render: (r) => (
                  <>
                    <StatusBadge status={String(r.status)} /> {r.isOverdue ? <StatusBadge status="Overdue" /> : null}
                  </>
                ),
              },
              { key: 'assignedToName', label: 'Assigned To' },
            ]}
          />
        );
      case 'opportunities':
        return (
          <ReportTable
            key="opportunities"
            endpoint="/reports/opportunities"
            fileName="opportunity-report.csv"
            filters={[{ name: 'stage', label: 'Stage', options: toOptions(OPPORTUNITY_STAGES) }, ...ownerFilter]}
            columns={[
              { key: 'opportunityName', label: 'Opportunity', sortable: true },
              { key: 'customerName', label: 'Customer', render: (r) => text(r.customerName) },
              { key: 'stage', label: 'Stage', sortable: true, render: (r) => <StatusBadge status={String(r.stage)} /> },
              { key: 'amount', label: 'Amount', sortable: true, className: 'text-end', render: (r) => formatCurrency(Number(r.amount)) },
              { key: 'probability', label: 'Probability', sortable: true, className: 'text-end', render: (r) => `${r.probability}%` },
              { key: 'weightedAmount', label: 'Weighted', className: 'text-end', render: (r) => formatCurrency(Number(r.weightedAmount)) },
              { key: 'expectedCloseDate', label: 'Expected Close', sortable: true, render: (r) => formatDate(String(r.expectedCloseDate)) },
              { key: 'assignedToName', label: 'Owner' },
            ]}
          />
        );
      case 'pipeline':
        return <PipelineReport />;
      case 'conversion':
        return <ConversionReport />;
      case 'user-activity':
        return (
          <ReportTable
            key="user-activity"
            endpoint="/reports/user-activity"
            fileName="user-activity-report.csv"
            defaultSort="totalActions"
            dateLabel="Activity"
            columns={[
              { key: 'userName', label: 'User', sortable: true },
              { key: 'role', label: 'Role', render: (r) => ROLE_LABELS[String(r.role)] || text(r.role) },
              { key: 'totalActions', label: 'Total Actions', sortable: true, className: 'text-end' },
              { key: 'logins', label: 'Logins', className: 'text-end' },
              { key: 'failedLogins', label: 'Failed Logins', className: 'text-end' },
              { key: 'creates', label: 'Creates', className: 'text-end' },
              { key: 'updates', label: 'Updates', className: 'text-end' },
              { key: 'deletes', label: 'Deletes', className: 'text-end' },
              { key: 'lastActivity', label: 'Last Activity', render: (r) => (r.lastActivity ? formatDateTime(String(r.lastActivity)) : '-') },
            ]}
          />
        );
      case 'audit':
        return (
          <ReportTable
            key="audit"
            endpoint="/reports/audit"
            fileName="audit-report.csv"
            filters={[
              { name: 'userId', label: 'User', options: users.map((u) => ({ value: String(u.id), label: u.name })) },
              { name: 'result', label: 'Result', options: toOptions(['Success', 'Failure']) },
            ]}
            columns={[
              { key: 'createdAt', label: 'Date & Time', sortable: true, render: (r) => formatDateTime(String(r.createdAt)) },
              { key: 'userName', label: 'User', render: (r) => text(r.userName) },
              { key: 'action', label: 'Action', sortable: true },
              { key: 'entityName', label: 'Module', sortable: true },
              { key: 'recordId', label: 'Record', render: (r) => text(r.recordId) },
              { key: 'result', label: 'Result', render: (r) => <StatusBadge status={String(r.result)} /> },
              { key: 'details', label: 'Details', render: (r) => <span className="small">{text(r.details)}</span> },
              { key: 'ipAddress', label: 'IP', render: (r) => text(r.ipAddress) },
            ]}
          />
        );
      default:
        return null;
    }
  };

  return (
    <>
      <PageHeader title="Reports" subtitle="Filter, sort and export CRM data. Data is limited to records you are allowed to see." />

      <ul className="nav nav-tabs mb-0 flex-nowrap overflow-auto" role="tablist">
        {visibleTabs.map((t) => (
          <li className="nav-item" key={t.key} role="presentation">
            <button
              className={`nav-link text-nowrap ${activeTab === t.key ? 'active' : ''}`}
              role="tab"
              aria-selected={activeTab === t.key}
              onClick={() => setActiveTab(t.key)}
            >
              <i className={`bi ${t.icon} me-1`}></i>
              {t.label}
            </button>
          </li>
        ))}
      </ul>
      <div className="card rounded-top-0">
        <div className="card-body">{renderTab()}</div>
      </div>
    </>
  );
}
