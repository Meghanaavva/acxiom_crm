import { useCallback, useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import api, { cleanParams, getErrorMessage } from '../../api/client';
import { fetchUserLookup } from '../../api/services';
import ConfirmDialog from '../../components/ConfirmDialog';
import Loader from '../../components/Loader';
import PageHeader from '../../components/PageHeader';
import Pagination from '../../components/Pagination';
import SortableHeader from '../../components/SortableHeader';
import StatusBadge from '../../components/StatusBadge';
import { useAuth } from '../../context/AuthContext';
import { useToast } from '../../context/ToastContext';
import type { Activity, PagedResult, UserLookup } from '../../types';
import { ACTIVITY_STATUSES, ACTIVITY_TYPES } from '../../utils/constants';
import { formatDateTime } from '../../utils/format';

const ACTIVITY_ICONS: Record<string, string> = {
  Call: 'bi-telephone',
  Meeting: 'bi-people',
  Email: 'bi-envelope',
  Task: 'bi-check2-square',
};

export default function ActivityList() {
  const navigate = useNavigate();
  const { user, hasRole } = useAuth();
  const { showToast } = useToast();

  const [result, setResult] = useState<PagedResult<Activity> | null>(null);
  const [loading, setLoading] = useState(true);
  const [users, setUsers] = useState<UserLookup[]>([]);

  const [search, setSearch] = useState('');
  const [filters, setFilters] = useState({ search: '', activityType: '', status: '', assignedTo: '', dateFrom: '', dateTo: '' });
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [sortBy, setSortBy] = useState('activityDate');
  const [sortOrder, setSortOrder] = useState<'asc' | 'desc'>('desc');

  const [deleteTarget, setDeleteTarget] = useState<Activity | null>(null);
  const [deleting, setDeleting] = useState(false);

  const loadActivities = useCallback(async () => {
    setLoading(true);
    try {
      const { data } = await api.get<PagedResult<Activity>>('/activities', {
        params: cleanParams({ ...filters, page, pageSize, sortBy, sortOrder }),
      });
      setResult(data);
    } catch (err) {
      showToast(getErrorMessage(err), 'danger');
    } finally {
      setLoading(false);
    }
  }, [filters, page, pageSize, sortBy, sortOrder, showToast]);

  useEffect(() => {
    loadActivities();
  }, [loadActivities]);

  useEffect(() => {
    fetchUserLookup().then(setUsers).catch(() => setUsers([]));
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

  // owner can delete his own activity, Admin/Manager can delete any in scope
  const canDelete = (a: Activity) => hasRole('Admin', 'Manager') || a.assignedTo === user?.id;

  const handleDelete = async () => {
    if (!deleteTarget) return;
    setDeleting(true);
    try {
      await api.delete(`/activities/${deleteTarget.id}`);
      showToast('Activity deleted successfully.');
      setDeleteTarget(null);
      loadActivities();
    } catch (err) {
      showToast(getErrorMessage(err), 'danger');
    } finally {
      setDeleting(false);
    }
  };

  return (
    <>
      <PageHeader
        title="Activities"
        subtitle="Calls, meetings, emails and tasks"
        actions={
          <Link to="/activities/new" className="btn btn-primary">
            <i className="bi bi-plus-lg me-1"></i>Log Activity
          </Link>
        }
      />

      <div className="card">
        <div className="card-body">
          <div className="row g-2 mb-3">
            <div className="col-md-3">
              <input
                type="search"
                className="form-control"
                placeholder="Search by subject"
                aria-label="Search activities"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
              />
            </div>
            <div className="col-md-2">
              <select className="form-select" aria-label="Filter by type" value={filters.activityType} onChange={(e) => handleFilterChange('activityType', e.target.value)}>
                <option value="">All Types</option>
                {ACTIVITY_TYPES.map((t) => (
                  <option key={t}>{t}</option>
                ))}
              </select>
            </div>
            <div className="col-md-2">
              <select className="form-select" aria-label="Filter by status" value={filters.status} onChange={(e) => handleFilterChange('status', e.target.value)}>
                <option value="">All Statuses</option>
                {ACTIVITY_STATUSES.map((s) => (
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
            <div className="col-6 col-md">
              <input type="date" className="form-control" aria-label="From date" title="From date" value={filters.dateFrom} onChange={(e) => handleFilterChange('dateFrom', e.target.value)} />
            </div>
            <div className="col-6 col-md">
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
                    <SortableHeader label="Type" field="activityType" sortBy={sortBy} sortOrder={sortOrder} onSort={handleSort} />
                    <SortableHeader label="Subject" field="subject" sortBy={sortBy} sortOrder={sortOrder} onSort={handleSort} />
                    <SortableHeader label="Date" field="activityDate" sortBy={sortBy} sortOrder={sortOrder} onSort={handleSort} />
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
                        No activities found.
                      </td>
                    </tr>
                  )}
                  {result?.data.map((a) => (
                    <tr key={a.id}>
                      <td className="text-nowrap">
                        <i className={`bi ${ACTIVITY_ICONS[a.activityType] || 'bi-dot'} text-primary me-2`}></i>
                        {a.activityType}
                      </td>
                      <td>
                        <div className="fw-semibold">{a.subject}</div>
                        {a.description && <div className="small text-muted text-truncate" style={{ maxWidth: 260 }}>{a.description}</div>}
                      </td>
                      <td className="text-nowrap">{formatDateTime(a.activityDate)}</td>
                      <td>
                        {a.customerId ? (
                          <Link to={`/customers/${a.customerId}`}>{a.customerName}</Link>
                        ) : a.leadId ? (
                          <Link to={`/leads/${a.leadId}`}>{a.leadName}</Link>
                        ) : (
                          '-'
                        )}
                      </td>
                      <td>
                        <StatusBadge status={a.status} />
                      </td>
                      <td>{a.assignedToName}</td>
                      <td className="text-end text-nowrap">
                        <button className="btn btn-sm btn-outline-primary me-1" title="Edit" aria-label="Edit" onClick={() => navigate(`/activities/${a.id}/edit`)}>
                          <i className="bi bi-pencil"></i>
                        </button>
                        {canDelete(a) && (
                          <button className="btn btn-sm btn-outline-danger" title="Delete" aria-label="Delete" onClick={() => setDeleteTarget(a)}>
                            <i className="bi bi-trash"></i>
                          </button>
                        )}
                      </td>
                    </tr>
                  ))}
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

      <ConfirmDialog
        show={!!deleteTarget}
        title="Delete Activity"
        message={
          <>
            Are you sure you want to delete <strong>{deleteTarget?.subject}</strong>? This action will be recorded in the audit log.
          </>
        }
        busy={deleting}
        onConfirm={handleDelete}
        onCancel={() => setDeleteTarget(null)}
      />
    </>
  );
}
