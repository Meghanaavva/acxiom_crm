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
import type { Lead, PagedResult, UserLookup } from '../../types';
import { LEAD_PRIORITIES, LEAD_SOURCES, LEAD_STATUSES } from '../../utils/constants';
import { formatCurrency, formatDate } from '../../utils/format';

export default function LeadList() {
  const navigate = useNavigate();
  const { hasRole } = useAuth();
  const { showToast } = useToast();
  const canDelete = hasRole('Admin', 'Manager');

  const [result, setResult] = useState<PagedResult<Lead> | null>(null);
  const [loading, setLoading] = useState(true);
  const [users, setUsers] = useState<UserLookup[]>([]);

  const [search, setSearch] = useState('');
  const [filters, setFilters] = useState({ search: '', status: '', source: '', priority: '', assignedTo: '' });
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [sortBy, setSortBy] = useState('createdAt');
  const [sortOrder, setSortOrder] = useState<'asc' | 'desc'>('desc');

  const [deleteTarget, setDeleteTarget] = useState<Lead | null>(null);
  const [deleting, setDeleting] = useState(false);

  const loadLeads = useCallback(async () => {
    setLoading(true);
    try {
      const { data } = await api.get<PagedResult<Lead>>('/leads', {
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
    loadLeads();
  }, [loadLeads]);

  useEffect(() => {
    fetchUserLookup().then(setUsers).catch(() => setUsers([]));
  }, []);

  // debounce the search box
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

  const handleDelete = async () => {
    if (!deleteTarget) return;
    setDeleting(true);
    try {
      await api.delete(`/leads/${deleteTarget.id}`);
      showToast('Lead deleted successfully.');
      setDeleteTarget(null);
      loadLeads();
    } catch (err) {
      showToast(getErrorMessage(err), 'danger');
    } finally {
      setDeleting(false);
    }
  };

  return (
    <>
      <PageHeader
        title="Leads"
        subtitle="Capture, qualify and convert prospects"
        actions={
          <Link to="/leads/new" className="btn btn-primary">
            <i className="bi bi-plus-lg me-1"></i>Add Lead
          </Link>
        }
      />

      <div className="card">
        <div className="card-body">
          <div className="row g-2 mb-3">
            <div className="col-md-4">
              <div className="input-group">
                <span className="input-group-text">
                  <i className="bi bi-search"></i>
                </span>
                <input
                  type="search"
                  className="form-control"
                  placeholder="Search by name, company, email or phone"
                  aria-label="Search leads"
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                />
              </div>
            </div>
            <div className="col-6 col-md-2">
              <select className="form-select" aria-label="Filter by status" value={filters.status} onChange={(e) => handleFilterChange('status', e.target.value)}>
                <option value="">All Statuses</option>
                {LEAD_STATUSES.map((s) => (
                  <option key={s}>{s}</option>
                ))}
              </select>
            </div>
            <div className="col-6 col-md-2">
              <select className="form-select" aria-label="Filter by source" value={filters.source} onChange={(e) => handleFilterChange('source', e.target.value)}>
                <option value="">All Sources</option>
                {LEAD_SOURCES.map((s) => (
                  <option key={s}>{s}</option>
                ))}
              </select>
            </div>
            <div className="col-6 col-md-2">
              <select
                className="form-select"
                aria-label="Filter by priority"
                value={filters.priority}
                onChange={(e) => handleFilterChange('priority', e.target.value)}
              >
                <option value="">All Priorities</option>
                {LEAD_PRIORITIES.map((p) => (
                  <option key={p}>{p}</option>
                ))}
              </select>
            </div>
            {users.length > 1 && (
              <div className="col-6 col-md-2">
                <select
                  className="form-select"
                  aria-label="Filter by assigned user"
                  value={filters.assignedTo}
                  onChange={(e) => handleFilterChange('assignedTo', e.target.value)}
                >
                  <option value="">All Owners</option>
                  {users.map((u) => (
                    <option key={u.id} value={u.id}>
                      {u.name}
                    </option>
                  ))}
                </select>
              </div>
            )}
          </div>

          {loading && !result ? (
            <Loader />
          ) : (
            <div className="table-responsive">
              <table className="table table-hover align-middle mb-0">
                <thead>
                  <tr>
                    <SortableHeader label="Code" field="leadCode" sortBy={sortBy} sortOrder={sortOrder} onSort={handleSort} />
                    <SortableHeader label="Lead Name" field="leadName" sortBy={sortBy} sortOrder={sortOrder} onSort={handleSort} />
                    <SortableHeader label="Company" field="companyName" sortBy={sortBy} sortOrder={sortOrder} onSort={handleSort} />
                    <th>Source</th>
                    <SortableHeader label="Status" field="status" sortBy={sortBy} sortOrder={sortOrder} onSort={handleSort} />
                    <SortableHeader label="Priority" field="priority" sortBy={sortBy} sortOrder={sortOrder} onSort={handleSort} />
                    <SortableHeader label="Expected Value" field="expectedValue" sortBy={sortBy} sortOrder={sortOrder} onSort={handleSort} />
                    <th>Assigned To</th>
                    <SortableHeader label="Created" field="createdAt" sortBy={sortBy} sortOrder={sortOrder} onSort={handleSort} />
                    <th className="text-end">Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {result?.data.length === 0 && (
                    <tr>
                      <td colSpan={10} className="text-center text-muted py-4">
                        No leads found.
                      </td>
                    </tr>
                  )}
                  {result?.data.map((l) => {
                    const isClosed = l.status === 'Converted' || l.status === 'Lost';
                    return (
                      <tr key={l.id}>
                        <td className="text-nowrap">{l.leadCode}</td>
                        <td>
                          <Link to={`/leads/${l.id}`} className="fw-semibold text-decoration-none">
                            {l.leadName}
                          </Link>
                          <div className="small text-muted">{l.email || l.phone}</div>
                        </td>
                        <td>{l.companyName || '-'}</td>
                        <td>{l.source}</td>
                        <td>
                          <StatusBadge status={l.status} />
                        </td>
                        <td>
                          <StatusBadge status={l.priority} />
                        </td>
                        <td className="text-end">{formatCurrency(l.expectedValue)}</td>
                        <td>{l.assignedToName}</td>
                        <td className="text-nowrap">{formatDate(l.createdAt)}</td>
                        <td className="text-end text-nowrap">
                          <button className="btn btn-sm btn-outline-secondary me-1" title="View" aria-label="View lead" onClick={() => navigate(`/leads/${l.id}`)}>
                            <i className="bi bi-eye"></i>
                          </button>
                          {!isClosed && (
                            <button
                              className="btn btn-sm btn-outline-primary me-1"
                              title="Edit"
                              aria-label="Edit lead"
                              onClick={() => navigate(`/leads/${l.id}/edit`)}
                            >
                              <i className="bi bi-pencil"></i>
                            </button>
                          )}
                          {canDelete && (
                            <button className="btn btn-sm btn-outline-danger" title="Delete" aria-label="Delete lead" onClick={() => setDeleteTarget(l)}>
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

      <ConfirmDialog
        show={!!deleteTarget}
        title="Delete Lead"
        message={
          <>
            Are you sure you want to delete lead <strong>{deleteTarget?.leadName}</strong>?
          </>
        }
        busy={deleting}
        onConfirm={handleDelete}
        onCancel={() => setDeleteTarget(null)}
      />
    </>
  );
}
