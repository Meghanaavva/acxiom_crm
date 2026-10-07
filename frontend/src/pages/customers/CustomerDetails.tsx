import { useEffect, useState, type ReactNode } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import api, { getErrorMessage } from '../../api/client';
import ConfirmDialog from '../../components/ConfirmDialog';
import Loader from '../../components/Loader';
import PageHeader from '../../components/PageHeader';
import StatusBadge from '../../components/StatusBadge';
import { useAuth } from '../../context/AuthContext';
import { useToast } from '../../context/ToastContext';
import type { Activity, Customer, FollowUp, Opportunity } from '../../types';
import { formatCurrency, formatDate, formatDateTime } from '../../utils/format';

interface HistoryLog {
  id: number;
  action: string;
  userName: string | null;
  oldValue: string | null;
  newValue: string | null;
  details: string | null;
  createdAt: string;
}

interface CustomerHistory {
  opportunities: Opportunity[];
  followUps: FollowUp[];
  activities: Activity[];
  convertedFromLead: { id: number; leadCode: string; leadName: string; convertedAt: string } | null;
  auditLogs: HistoryLog[];
}

type Tab = 'opportunities' | 'followups' | 'activities' | 'history';

// Shows a JSON audit value as "key: value" lines
function describeChange(value: string | null): string {
  if (!value) return '';
  try {
    const obj = JSON.parse(value) as Record<string, unknown>;
    return Object.entries(obj)
      .map(([k, v]) => `${k}: ${v ?? '-'}`)
      .join(', ');
  } catch {
    return value;
  }
}

function InfoRow({ label, value }: { label: string; value: ReactNode }) {
  return (
    <div className="col-sm-6 col-lg-4">
      <div className="text-muted small">{label}</div>
      <div className="fw-semibold">{value || '-'}</div>
    </div>
  );
}

export default function CustomerDetails() {
  const { id } = useParams();
  const navigate = useNavigate();
  const { hasRole } = useAuth();
  const { showToast } = useToast();
  const canDelete = hasRole('Admin', 'Manager');

  const [customer, setCustomer] = useState<Customer | null>(null);
  const [history, setHistory] = useState<CustomerHistory | null>(null);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState<Tab>('opportunities');
  const [showDelete, setShowDelete] = useState(false);
  const [deleting, setDeleting] = useState(false);

  useEffect(() => {
    setLoading(true);
    Promise.all([api.get<Customer>(`/customers/${id}`), api.get<CustomerHistory>(`/customers/${id}/history`)])
      .then(([customerRes, historyRes]) => {
        setCustomer(customerRes.data);
        setHistory(historyRes.data);
      })
      .catch((err) => {
        showToast(getErrorMessage(err), 'danger');
        navigate('/customers');
      })
      .finally(() => setLoading(false));
  }, [id, navigate, showToast]);

  const handleDelete = async () => {
    setDeleting(true);
    try {
      await api.delete(`/customers/${id}`);
      showToast('Customer deleted successfully.');
      navigate('/customers');
    } catch (err) {
      showToast(getErrorMessage(err), 'danger');
      setShowDelete(false);
    } finally {
      setDeleting(false);
    }
  };

  if (loading || !customer || !history) return <Loader />;

  const tabs: { key: Tab; label: string; count: number }[] = [
    { key: 'opportunities', label: 'Opportunities', count: history.opportunities.length },
    { key: 'followups', label: 'Follow-Ups', count: history.followUps.length },
    { key: 'activities', label: 'Activities', count: history.activities.length },
    { key: 'history', label: 'History', count: history.auditLogs.length },
  ];

  return (
    <>
      <PageHeader
        title={customer.customerName}
        subtitle={`${customer.customerCode} ${customer.companyName ? '· ' + customer.companyName : ''}`}
        actions={
          <>
            <Link to="/customers" className="btn btn-outline-secondary">
              <i className="bi bi-arrow-left me-1"></i>Back
            </Link>
            <Link to={`/customers/${customer.id}/edit`} className="btn btn-primary">
              <i className="bi bi-pencil me-1"></i>Edit
            </Link>
            {canDelete && (
              <button className="btn btn-outline-danger" onClick={() => setShowDelete(true)}>
                <i className="bi bi-trash me-1"></i>Delete
              </button>
            )}
          </>
        }
      />

      <div className="card mb-4">
        <div className="card-body">
          <div className="row g-3">
            <InfoRow label="Email" value={customer.email} />
            <InfoRow label="Phone" value={customer.phone} />
            <InfoRow label="Status" value={<StatusBadge status={customer.status} />} />
            <InfoRow label="Company" value={customer.companyName} />
            <InfoRow label="Address" value={[customer.address, customer.city, customer.state].filter(Boolean).join(', ')} />
            <InfoRow label="Assigned To" value={customer.assignedToName} />
            <InfoRow label="Created By" value={customer.createdByName} />
            <InfoRow label="Created On" value={formatDateTime(customer.createdAt)} />
            <InfoRow label="Last Updated" value={formatDateTime(customer.updatedAt)} />
            {customer.notes && (
              <div className="col-12">
                <div className="text-muted small">Notes</div>
                <div style={{ whiteSpace: 'pre-wrap' }}>{customer.notes}</div>
              </div>
            )}
          </div>
          {history.convertedFromLead && (
            <div className="alert alert-info mt-3 mb-0 py-2 small">
              <i className="bi bi-arrow-repeat me-1"></i>
              Converted from lead{' '}
              <Link to={`/leads/${history.convertedFromLead.id}`}>
                {history.convertedFromLead.leadCode} - {history.convertedFromLead.leadName}
              </Link>{' '}
              on {formatDate(history.convertedFromLead.convertedAt)}
            </div>
          )}
        </div>
      </div>

      <div className="card">
        <div className="card-header bg-white d-flex flex-wrap justify-content-between align-items-center gap-2">
          <ul className="nav nav-tabs card-header-tabs">
            {tabs.map((t) => (
              <li className="nav-item" key={t.key}>
                <button className={`nav-link ${activeTab === t.key ? 'active' : ''}`} onClick={() => setActiveTab(t.key)}>
                  {t.label} <span className="badge bg-secondary-subtle text-secondary">{t.count}</span>
                </button>
              </li>
            ))}
          </ul>
          <div className="d-flex gap-2">
            <Link to={`/opportunities/new?customerId=${customer.id}`} className="btn btn-sm btn-outline-primary">
              <i className="bi bi-plus-lg me-1"></i>Opportunity
            </Link>
            <Link to={`/followups/new?customerId=${customer.id}`} className="btn btn-sm btn-outline-primary">
              <i className="bi bi-plus-lg me-1"></i>Follow-Up
            </Link>
          </div>
        </div>
        <div className="card-body">
          <div className="table-responsive">
            {activeTab === 'opportunities' && (
              <table className="table table-hover align-middle mb-0">
                <thead>
                  <tr>
                    <th>Name</th>
                    <th>Stage</th>
                    <th className="text-end">Amount</th>
                    <th className="text-end">Probability</th>
                    <th>Expected Close</th>
                    <th>Owner</th>
                  </tr>
                </thead>
                <tbody>
                  {history.opportunities.length === 0 && (
                    <tr>
                      <td colSpan={6} className="text-center text-muted py-4">
                        No opportunities yet.
                      </td>
                    </tr>
                  )}
                  {history.opportunities.map((o) => (
                    <tr key={o.id}>
                      <td>
                        <Link to={`/opportunities/${o.id}`} className="text-decoration-none">
                          {o.opportunityName}
                        </Link>
                      </td>
                      <td>
                        <StatusBadge status={o.stage} />
                      </td>
                      <td className="text-end">{formatCurrency(o.amount)}</td>
                      <td className="text-end">{o.probability}%</td>
                      <td>{formatDate(o.expectedCloseDate)}</td>
                      <td>{o.assignedToName}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}

            {activeTab === 'followups' && (
              <table className="table table-hover align-middle mb-0">
                <thead>
                  <tr>
                    <th>Date</th>
                    <th>Subject</th>
                    <th>Type</th>
                    <th>Status</th>
                    <th>Assigned To</th>
                    <th>Remarks</th>
                  </tr>
                </thead>
                <tbody>
                  {history.followUps.length === 0 && (
                    <tr>
                      <td colSpan={6} className="text-center text-muted py-4">
                        No follow-ups yet.
                      </td>
                    </tr>
                  )}
                  {history.followUps.map((f) => (
                    <tr key={f.id}>
                      <td className="text-nowrap">{formatDate(f.followUpDate)}</td>
                      <td>{f.subject}</td>
                      <td>{f.followUpType}</td>
                      <td>
                        <StatusBadge status={f.isOverdue ? 'Overdue' : f.status} />
                      </td>
                      <td>{f.assignedToName}</td>
                      <td className="small text-muted">{f.remarks || '-'}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}

            {activeTab === 'activities' && (
              <table className="table table-hover align-middle mb-0">
                <thead>
                  <tr>
                    <th>Date</th>
                    <th>Type</th>
                    <th>Subject</th>
                    <th>Status</th>
                    <th>Assigned To</th>
                  </tr>
                </thead>
                <tbody>
                  {history.activities.length === 0 && (
                    <tr>
                      <td colSpan={5} className="text-center text-muted py-4">
                        No activities yet.
                      </td>
                    </tr>
                  )}
                  {history.activities.map((a) => (
                    <tr key={a.id}>
                      <td className="text-nowrap">{formatDateTime(a.activityDate)}</td>
                      <td>{a.activityType}</td>
                      <td>{a.subject}</td>
                      <td>
                        <StatusBadge status={a.status} />
                      </td>
                      <td>{a.assignedToName}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}

            {activeTab === 'history' && (
              <table className="table align-middle mb-0">
                <thead>
                  <tr>
                    <th>When</th>
                    <th>Action</th>
                    <th>By</th>
                    <th>Changes</th>
                  </tr>
                </thead>
                <tbody>
                  {history.auditLogs.length === 0 && (
                    <tr>
                      <td colSpan={4} className="text-center text-muted py-4">
                        No history found.
                      </td>
                    </tr>
                  )}
                  {history.auditLogs.map((log) => (
                    <tr key={log.id}>
                      <td className="text-nowrap small">{formatDateTime(log.createdAt)}</td>
                      <td>
                        <span className="badge bg-light text-dark border">{log.action}</span>
                      </td>
                      <td>{log.userName || '-'}</td>
                      <td className="small">
                        {log.details && <div>{log.details}</div>}
                        {log.oldValue && <div className="text-danger">Old: {describeChange(log.oldValue)}</div>}
                        {log.newValue && <div className="text-success">New: {describeChange(log.newValue)}</div>}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>
        </div>
      </div>

      <ConfirmDialog
        show={showDelete}
        title="Delete Customer"
        message={
          <>
            Are you sure you want to delete <strong>{customer.customerName}</strong>?
          </>
        }
        busy={deleting}
        onConfirm={handleDelete}
        onCancel={() => setShowDelete(false)}
      />
    </>
  );
}
