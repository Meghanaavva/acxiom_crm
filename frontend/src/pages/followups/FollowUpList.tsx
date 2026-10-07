import { useCallback, useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import api, { cleanParams, getErrorMessage } from '../../api/client';
import { fetchCustomerOptions, fetchLeadOptions, fetchUserLookup, type Option } from '../../api/services';
import ConfirmDialog from '../../components/ConfirmDialog';
import Loader from '../../components/Loader';
import Modal from '../../components/Modal';
import PageHeader from '../../components/PageHeader';
import Pagination from '../../components/Pagination';
import SortableHeader from '../../components/SortableHeader';
import StatusBadge from '../../components/StatusBadge';
import { useAuth } from '../../context/AuthContext';
import { useToast } from '../../context/ToastContext';
import type { FollowUp, PagedResult, UserLookup } from '../../types';
import { FOLLOWUP_STATUSES } from '../../utils/constants';
import { formatDate } from '../../utils/format';
import { maxLength, notPastDate, required, todayString } from '../../utils/validation';

const VIEWS = [
  { value: '', label: 'All' },
  { value: 'today', label: 'Today' },
  { value: 'upcoming', label: 'Upcoming' },
  { value: 'overdue', label: 'Overdue' },
];

type ConfirmAction = { type: 'missed' | 'cancel' | 'delete'; item: FollowUp } | null;

export default function FollowUpList() {
  const navigate = useNavigate();
  const { hasRole } = useAuth();
  const { showToast } = useToast();
  const canDelete = hasRole('Admin', 'Manager');

  const [result, setResult] = useState<PagedResult<FollowUp> | null>(null);
  const [loading, setLoading] = useState(true);
  const [users, setUsers] = useState<UserLookup[]>([]);
  const [customers, setCustomers] = useState<Option[]>([]);
  const [leads, setLeads] = useState<Option[]>([]);

  const [search, setSearch] = useState('');
  const [view, setView] = useState('');
  const [filters, setFilters] = useState({ search: '', status: '', assignedTo: '', dateFrom: '', dateTo: '', customerId: '', leadId: '' });
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [sortBy, setSortBy] = useState('followUpDate');
  const [sortOrder, setSortOrder] = useState<'asc' | 'desc'>('asc');

  // complete / reschedule modals
  const [completeTarget, setCompleteTarget] = useState<FollowUp | null>(null);
  const [completeRemarks, setCompleteRemarks] = useState('');
  const [rescheduleTarget, setRescheduleTarget] = useState<FollowUp | null>(null);
  const [rescheduleForm, setRescheduleForm] = useState({ followUpDate: '', remarks: '' });
  const [rescheduleError, setRescheduleError] = useState('');
  const [confirmAction, setConfirmAction] = useState<ConfirmAction>(null);
  const [busy, setBusy] = useState(false);

  const loadFollowUps = useCallback(async () => {
    setLoading(true);
    try {
      const { data } = await api.get<PagedResult<FollowUp>>('/followups', {
        params: cleanParams({ ...filters, view, page, pageSize, sortBy, sortOrder }),
      });
      setResult(data);
    } catch (err) {
      showToast(getErrorMessage(err), 'danger');
    } finally {
      setLoading(false);
    }
  }, [filters, view, page, pageSize, sortBy, sortOrder, showToast]);

  useEffect(() => {
    loadFollowUps();
  }, [loadFollowUps]);

  useEffect(() => {
    fetchUserLookup().then(setUsers).catch(() => setUsers([]));
    fetchCustomerOptions().then(setCustomers).catch(() => setCustomers([]));
    fetchLeadOptions().then(setLeads).catch(() => setLeads([]));
  }, []);

  useEffect(() => {
    const timer = setTimeout(() => {
      setFilters((f) => (f.search === search ? f : { ...f, search }));
      setPage(1);
    }, 400);
    return () => clearTimeout(timer);
  }, [search]);

  const handleSort = (field: string) => {
    if (sortBy === field) {
      setSortOrder(sortOrder === 'asc' ? 'desc' : 'asc');
    } else {
      setSortBy(field);
      setSortOrder('asc');
    }
  };

  const handleFilterChange = (name: string, value: string) => {
    setFilters({ ...filters, [name]: value });
    setPage(1);
  };

  const handleComplete = async () => {
    if (!completeTarget) return;
    setBusy(true);
    try {
      await api.patch(`/followups/${completeTarget.id}/complete`, { remarks: completeRemarks || undefined });
      showToast('Follow-up marked as completed.');
      setCompleteTarget(null);
      loadFollowUps();
    } catch (err) {
      showToast(getErrorMessage(err), 'danger');
    } finally {
      setBusy(false);
    }
  };

  const handleReschedule = async () => {
    if (!rescheduleTarget) return;
    const error =
      required(rescheduleForm.followUpDate, 'Follow-up date') ||
      notPastDate(rescheduleForm.followUpDate, 'Follow-up date cannot be earlier than today.') ||
      maxLength(rescheduleForm.remarks, 1000, 'Remarks');
    setRescheduleError(error);
    if (error) return;

    setBusy(true);
    try {
      await api.post(`/followups/${rescheduleTarget.id}/reschedule`, {
        followUpDate: rescheduleForm.followUpDate,
        remarks: rescheduleForm.remarks || undefined,
      });
      showToast('Follow-up rescheduled.');
      setRescheduleTarget(null);
      loadFollowUps();
    } catch (err) {
      setRescheduleError(getErrorMessage(err));
    } finally {
      setBusy(false);
    }
  };

  const handleConfirm = async () => {
    if (!confirmAction) return;
    const { type, item } = confirmAction;
    setBusy(true);
    try {
      if (type === 'delete') {
        await api.delete(`/followups/${item.id}`);
        showToast('Follow-up deleted successfully.');
      } else if (type === 'missed') {
        await api.patch(`/followups/${item.id}/missed`);
        showToast('Follow-up marked as missed.', 'warning');
      } else {
        await api.patch(`/followups/${item.id}/cancel`);
        showToast('Follow-up cancelled.', 'warning');
      }
      setConfirmAction(null);
      loadFollowUps();
    } catch (err) {
      showToast(getErrorMessage(err), 'danger');
    } finally {
      setBusy(false);
    }
  };

  const relatedTo = (f: FollowUp) => {
    if (f.customerId) return <Link to={`/customers/${f.customerId}`}>{f.customerName}</Link>;
    if (f.leadId) return <Link to={`/leads/${f.leadId}`}>{f.leadName}</Link>;
    if (f.opportunityId) return <Link to={`/opportunities/${f.opportunityId}`}>{f.opportunityName}</Link>;
    return '-';
  };

  const confirmText: Record<string, { title: string; question: string; button: string; variant: string }> = {
    missed: { title: 'Mark as Missed', question: 'Mark this follow-up as missed', button: 'Mark Missed', variant: 'warning' },
    cancel: { title: 'Cancel Follow-Up', question: 'Cancel this follow-up', button: 'Cancel Follow-Up', variant: 'secondary' },
    delete: { title: 'Delete Follow-Up', question: 'Delete this follow-up', button: 'Delete', variant: 'danger' },
  };

  return (
    <>
      <PageHeader
        title="Follow-Ups"
        subtitle="Schedule and track follow-up activities"
        actions={
          <Link to="/followups/new" className="btn btn-primary">
            <i className="bi bi-plus-lg me-1"></i>Schedule Follow-Up
          </Link>
        }
      />

      <ul className="nav nav-pills mb-3">
        {VIEWS.map((v) => (
          <li className="nav-item" key={v.value}>
            <button
              className={`nav-link ${view === v.value ? 'active' : ''}`}
              onClick={() => {
                setView(v.value);
                setPage(1);
              }}
            >
              {v.label}
            </button>
          </li>
        ))}
      </ul>

      <div className="card">
        <div className="card-body">
          <div className="row g-2 mb-3">
            <div className="col-md-3">
              <input
                type="search"
                className="form-control"
                placeholder="Search by subject"
                aria-label="Search follow-ups"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
              />
            </div>
            <div className="col-md-2">
              <select className="form-select" aria-label="Filter by status" value={filters.status} onChange={(e) => handleFilterChange('status', e.target.value)}>
                <option value="">All Statuses</option>
                {FOLLOWUP_STATUSES.map((s) => (
                  <option key={s}>{s}</option>
                ))}
              </select>
            </div>
            {users.length > 1 && (
              <div className="col-md-2">
                <select className="form-select" aria-label="Filter by assigned user" value={filters.assignedTo} onChange={(e) => handleFilterChange('assignedTo', e.target.value)}>
                  <option value="">All Users</option>
                  {users.map((u) => (
                    <option key={u.id} value={u.id}>
                      {u.name}
                    </option>
                  ))}
                </select>
              </div>
            )}
            <div className="col-md-3">
              <select className="form-select" aria-label="Filter by related customer" value={filters.customerId} onChange={(e) => handleFilterChange('customerId', e.target.value)}>
                <option value="">All Customers</option>
                {customers.map((c) => (
                  <option key={c.id} value={c.id}>
                    {c.name}
                  </option>
                ))}
              </select>
            </div>
            <div className="col-md-3">
              <select className="form-select" aria-label="Filter by related lead" value={filters.leadId} onChange={(e) => handleFilterChange('leadId', e.target.value)}>
                <option value="">All Leads</option>
                {leads.map((l) => (
                  <option key={l.id} value={l.id}>
                    {l.name}
                  </option>
                ))}
              </select>
            </div>
            <div className="col-md-2">
              <input type="date" className="form-control" aria-label="From date" title="From date" value={filters.dateFrom} onChange={(e) => handleFilterChange('dateFrom', e.target.value)} />
            </div>
            <div className="col-md-2">
              <input type="date" className="form-control" aria-label="To date" title="To date" value={filters.dateTo} onChange={(e) => handleFilterChange('dateTo', e.target.value)} />
            </div>
          </div>

          {loading && !result ? (
            <Loader />
          ) : (
            <div className="table-responsive">
              <table className="table table-hover align-middle mb-0">
                <thead>
                  <tr>
                    <SortableHeader label="Date" field="followUpDate" sortBy={sortBy} sortOrder={sortOrder} onSort={handleSort} />
                    <SortableHeader label="Subject" field="subject" sortBy={sortBy} sortOrder={sortOrder} onSort={handleSort} />
                    <th>Type</th>
                    <th>Related To</th>
                    <SortableHeader label="Status" field="status" sortBy={sortBy} sortOrder={sortOrder} onSort={handleSort} />
                    <th>Assigned To</th>
                    <th className="text-end">Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {result?.data.length === 0 && (
                    <tr>
                      <td colSpan={7} className="text-center text-muted py-4">
                        No follow-ups found.
                      </td>
                    </tr>
                  )}
                  {result?.data.map((f) => {
                    const isOpen = f.status === 'Planned' || f.status === 'Missed';
                    return (
                      <tr key={f.id} className={f.isOverdue ? 'table-danger' : ''}>
                        <td className="text-nowrap">{formatDate(f.followUpDate)}</td>
                        <td>
                          <div className="fw-semibold">{f.subject}</div>
                          {f.remarks && <div className="small text-muted text-truncate" style={{ maxWidth: 260 }}>{f.remarks}</div>}
                        </td>
                        <td>{f.followUpType}</td>
                        <td>{relatedTo(f)}</td>
                        <td className="text-nowrap">
                          <StatusBadge status={f.status} /> {f.isOverdue && <StatusBadge status="Overdue" />}
                        </td>
                        <td>{f.assignedToName}</td>
                        <td className="text-end text-nowrap">
                          {isOpen && (
                            <>
                              <button
                                className="btn btn-sm btn-outline-success me-1"
                                title="Complete"
                                aria-label="Mark as completed"
                                onClick={() => {
                                  setCompleteRemarks('');
                                  setCompleteTarget(f);
                                }}
                              >
                                <i className="bi bi-check-lg"></i>
                              </button>
                              <button
                                className="btn btn-sm btn-outline-info me-1"
                                title="Reschedule"
                                aria-label="Reschedule"
                                onClick={() => {
                                  setRescheduleForm({ followUpDate: '', remarks: '' });
                                  setRescheduleError('');
                                  setRescheduleTarget(f);
                                }}
                              >
                                <i className="bi bi-calendar-event"></i>
                              </button>
                            </>
                          )}
                          {f.status === 'Planned' && (
                            <>
                              <button className="btn btn-sm btn-outline-warning me-1" title="Mark missed" aria-label="Mark as missed" onClick={() => setConfirmAction({ type: 'missed', item: f })}>
                                <i className="bi bi-exclamation-circle"></i>
                              </button>
                              <button className="btn btn-sm btn-outline-primary me-1" title="Edit" aria-label="Edit" onClick={() => navigate(`/followups/${f.id}/edit`)}>
                                <i className="bi bi-pencil"></i>
                              </button>
                            </>
                          )}
                          {isOpen && (
                            <button className="btn btn-sm btn-outline-secondary me-1" title="Cancel" aria-label="Cancel follow-up" onClick={() => setConfirmAction({ type: 'cancel', item: f })}>
                              <i className="bi bi-slash-circle"></i>
                            </button>
                          )}
                          {canDelete && (
                            <button className="btn btn-sm btn-outline-danger" title="Delete" aria-label="Delete" onClick={() => setConfirmAction({ type: 'delete', item: f })}>
                              <i className="bi bi-trash"></i>
                            </button>
                          )}
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          )}

          {result && (
            <Pagination
              pagination={result.pagination}
              onPageChange={setPage}
              onPageSizeChange={(size) => {
                setPageSize(size);
                setPage(1);
              }}
            />
          )}
        </div>
      </div>

      <Modal
        show={!!completeTarget}
        title="Complete Follow-Up"
        onClose={() => setCompleteTarget(null)}
        footer={
          <>
            <button className="btn btn-outline-secondary" onClick={() => setCompleteTarget(null)} disabled={busy}>
              Close
            </button>
            <button className="btn btn-success" onClick={handleComplete} disabled={busy}>
              {busy && <span className="spinner-border spinner-border-sm me-1"></span>}
              Mark Completed
            </button>
          </>
        }
      >
        <p className="mb-2">
          <strong>{completeTarget?.subject}</strong>
        </p>
        <label htmlFor="completeRemarks" className="form-label">
          Remarks
        </label>
        <textarea id="completeRemarks" className="form-control" rows={3} maxLength={1000} value={completeRemarks} onChange={(e) => setCompleteRemarks(e.target.value)} />
      </Modal>

      <Modal
        show={!!rescheduleTarget}
        title="Reschedule Follow-Up"
        onClose={() => setRescheduleTarget(null)}
        footer={
          <>
            <button className="btn btn-outline-secondary" onClick={() => setRescheduleTarget(null)} disabled={busy}>
              Close
            </button>
            <button className="btn btn-primary" onClick={handleReschedule} disabled={busy}>
              {busy && <span className="spinner-border spinner-border-sm me-1"></span>}
              Reschedule
            </button>
          </>
        }
      >
        <p className="mb-2">
          <strong>{rescheduleTarget?.subject}</strong> (currently {formatDate(rescheduleTarget?.followUpDate)})
        </p>
        <div className="mb-3">
          <label htmlFor="rescheduleDate" className="form-label">
            New Date <span className="text-danger">*</span>
          </label>
          <input
            id="rescheduleDate"
            type="date"
            min={todayString()}
            className={`form-control ${rescheduleError ? 'is-invalid' : ''}`}
            value={rescheduleForm.followUpDate}
            onChange={(e) => setRescheduleForm({ ...rescheduleForm, followUpDate: e.target.value })}
          />
          {rescheduleError && <div className="invalid-feedback d-block">{rescheduleError}</div>}
        </div>
        <label htmlFor="rescheduleRemarks" className="form-label">
          Remarks
        </label>
        <textarea
          id="rescheduleRemarks"
          className="form-control"
          rows={2}
          maxLength={1000}
          value={rescheduleForm.remarks}
          onChange={(e) => setRescheduleForm({ ...rescheduleForm, remarks: e.target.value })}
        />
      </Modal>

      <ConfirmDialog
        show={!!confirmAction}
        title={confirmAction ? confirmText[confirmAction.type].title : ''}
        message={
          <>
            {confirmAction && confirmText[confirmAction.type].question}: <strong>{confirmAction?.item.subject}</strong>? This action will be
            recorded in the audit log.
          </>
        }
        confirmText={confirmAction ? confirmText[confirmAction.type].button : ''}
        confirmVariant={confirmAction ? confirmText[confirmAction.type].variant : 'danger'}
        busy={busy}
        onConfirm={handleConfirm}
        onCancel={() => setConfirmAction(null)}
      />
    </>
  );
}
