import { useCallback, useEffect, useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import api, { cleanParams, getErrorMessage } from '../../api/client';
import { fetchCustomerOptions, fetchUserLookup, type Option } from '../../api/services';
import ConfirmDialog from '../../components/ConfirmDialog';
import Loader from '../../components/Loader';
import PageHeader from '../../components/PageHeader';
import Pagination from '../../components/Pagination';
import SortableHeader from '../../components/SortableHeader';
import StatusBadge from '../../components/StatusBadge';
import { useAuth } from '../../context/AuthContext';
import { useToast } from '../../context/ToastContext';
import type { Opportunity, PagedResult, UserLookup } from '../../types';
import { OPPORTUNITY_STAGES } from '../../utils/constants';
import { formatCurrency, formatDate } from '../../utils/format';

export default function OpportunityList() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const { hasRole } = useAuth();
  const { showToast } = useToast();
  const canDelete = hasRole('Admin', 'Manager');

  const [result, setResult] = useState<PagedResult<Opportunity> | null>(null);
  const [loading, setLoading] = useState(true);
  const [users, setUsers] = useState<UserLookup[]>([]);
  const [customers, setCustomers] = useState<Option[]>([]);

  const [search, setSearch] = useState('');
  const [filters, setFilters] = useState({
    search: '',
    stage: searchParams.get('stage') || '',
    status: searchParams.get('status') || '',
    assignedTo: '',
    customerId: searchParams.get('customerId') || '',
  });
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [sortBy, setSortBy] = useState('createdAt');
  const [sortOrder, setSortOrder] = useState<'asc' | 'desc'>('desc');

  const [deleteTarget, setDeleteTarget] = useState<Opportunity | null>(null);
  const [deleting, setDeleting] = useState(false);

  const loadOpportunities = useCallback(async () => {
    setLoading(true);
    try {
      const { data } = await api.get<PagedResult<Opportunity>>('/opportunities', {
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
    loadOpportunities();
  }, [loadOpportunities]);

  useEffect(() => {
    fetchUserLookup().then(setUsers).catch(() => setUsers([]));
    fetchCustomerOptions().then(setCustomers).catch(() => setCustomers([]));
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

  const handleDelete = async () => {
    if (!deleteTarget) return;
    setDeleting(true);
    try {
      await api.delete(`/opportunities/${deleteTarget.id}`);
      showToast('Opportunity deleted successfully.');
      setDeleteTarget(null);
      loadOpportunities();
    } catch (err) {
      showToast(getErrorMessage(err), 'danger');
    } finally {
      setDeleting(false);
    }
  };

  return (
    <>
      <PageHeader
        title="Opportunities"
        subtitle="Track deals through the sales pipeline"
        actions={
          <>
            <Link to="/pipeline" className="btn btn-outline-primary">
              <i className="bi bi-kanban me-1"></i>Pipeline View
            </Link>
            <Link to="/opportunities/new" className="btn btn-primary">
              <i className="bi bi-plus-lg me-1"></i>Add Opportunity
            </Link>
          </>
        }
      />

      <div className="card">
        <div className="card-body">
          <div className="row g-2 mb-3">
            <div className="col-md-3">
              <div className="input-group">
                <span className="input-group-text">
                  <i className="bi bi-search"></i>
                </span>
                <input
                  type="search"
                  className="form-control"
                  placeholder="Search by opportunity name"
                  aria-label="Search opportunities"
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                />
              </div>
            </div>
            <div className="col-md-3">
              <select className="form-select" aria-label="Filter by customer" value={filters.customerId} onChange={(e) => handleFilterChange('customerId', e.target.value)}>
                <option value="">All Customers</option>
                {customers.map((c) => (
                  <option key={c.id} value={c.id}>
                    {c.name}
                  </option>
                ))}
              </select>
            </div>
            <div className="col-6 col-md-2">
              <select className="form-select" aria-label="Filter by stage" value={filters.stage} onChange={(e) => handleFilterChange('stage', e.target.value)}>
                <option value="">All Stages</option>
                {OPPORTUNITY_STAGES.map((s) => (
                  <option key={s}>{s}</option>
                ))}
              </select>
            </div>
            <div className="col-6 col-md-2">
              <select className="form-select" aria-label="Filter by status" value={filters.status} onChange={(e) => handleFilterChange('status', e.target.value)}>
                <option value="">All Statuses</option>
                <option>Open</option>
                <option>Won</option>
                <option>Lost</option>
              </select>
            </div>
            {users.length > 1 && (
              <div className="col-md-2">
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
                    <SortableHeader label="Opportunity" field="opportunityName" sortBy={sortBy} sortOrder={sortOrder} onSort={handleSort} />
                    <th>Customer</th>
                    <SortableHeader label="Stage" field="stage" sortBy={sortBy} sortOrder={sortOrder} onSort={handleSort} />
                    <SortableHeader label="Amount" field="amount" sortBy={sortBy} sortOrder={sortOrder} onSort={handleSort} />
                    <SortableHeader label="Probability" field="probability" sortBy={sortBy} sortOrder={sortOrder} onSort={handleSort} />
                    <th className="text-end">Weighted</th>
                    <SortableHeader label="Close Date" field="expectedCloseDate" sortBy={sortBy} sortOrder={sortOrder} onSort={handleSort} />
                    <th>Owner</th>
                    <th className="text-end">Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {result?.data.length === 0 && (
                    <tr>
                      <td colSpan={9} className="text-center text-muted py-4">
                        No opportunities found.
                      </td>
                    </tr>
                  )}
                  {result?.data.map((o) => (
                    <tr key={o.id}>
                      <td>
                        <Link to={`/opportunities/${o.id}`} className="fw-semibold text-decoration-none">
                          {o.opportunityName}
                        </Link>
                      </td>
                      <td>
                        <Link to={`/customers/${o.customerId}`} className="text-decoration-none">
                          {o.customerName}
                        </Link>
                      </td>
                      <td>
                        <StatusBadge status={o.stage} />
                      </td>
                      <td className="text-end">{formatCurrency(o.amount)}</td>
                      <td className="text-end">{o.probability}%</td>
                      <td className="text-end">{formatCurrency(o.weightedAmount)}</td>
                      <td className="text-nowrap">{formatDate(o.expectedCloseDate)}</td>
                      <td>{o.assignedToName}</td>
                      <td className="text-end text-nowrap">
                        <button
                          className="btn btn-sm btn-outline-secondary me-1"
                          title="View"
                          aria-label="View opportunity"
                          onClick={() => navigate(`/opportunities/${o.id}`)}
                        >
                          <i className="bi bi-eye"></i>
                        </button>
                        {o.status === 'Open' && (
                          <button
                            className="btn btn-sm btn-outline-primary me-1"
                            title="Edit"
                            aria-label="Edit opportunity"
                            onClick={() => navigate(`/opportunities/${o.id}/edit`)}
                          >
                            <i className="bi bi-pencil"></i>
                          </button>
                        )}
                        {canDelete && (
                          <button className="btn btn-sm btn-outline-danger" title="Delete" aria-label="Delete opportunity" onClick={() => setDeleteTarget(o)}>
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
        title="Delete Opportunity"
        message={
          <>
            Are you sure you want to delete <strong>{deleteTarget?.opportunityName}</strong>?
          </>
        }
        busy={deleting}
        onConfirm={handleDelete}
        onCancel={() => setDeleteTarget(null)}
      />
    </>
  );
}
