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
import type { Customer, PagedResult, UserLookup } from '../../types';
import { CUSTOMER_STATUSES } from '../../utils/constants';
import { formatDate } from '../../utils/format';

export default function CustomerList() {
  const navigate = useNavigate();
  const { hasRole } = useAuth();
  const { showToast } = useToast();
  const canDelete = hasRole('Admin', 'Manager');

  const [result, setResult] = useState<PagedResult<Customer> | null>(null);
  const [loading, setLoading] = useState(true);
  const [users, setUsers] = useState<UserLookup[]>([]);

  const [search, setSearch] = useState('');
  const [filters, setFilters] = useState({ search: '', status: '', assignedTo: '' });
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [sortBy, setSortBy] = useState('createdAt');
  const [sortOrder, setSortOrder] = useState<'asc' | 'desc'>('desc');

  const [deleteTarget, setDeleteTarget] = useState<Customer | null>(null);
  const [deleting, setDeleting] = useState(false);

  const loadCustomers = useCallback(async () => {
    setLoading(true);
    try {
      const { data } = await api.get<PagedResult<Customer>>('/customers', {
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
    loadCustomers();
  }, [loadCustomers]);

  useEffect(() => {
    fetchUserLookup().then(setUsers).catch(() => setUsers([]));
  }, []);

  // wait a little after typing before searching
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
      await api.delete(`/customers/${deleteTarget.id}`);
      showToast('Customer deleted successfully.');
      setDeleteTarget(null);
      loadCustomers();
    } catch (err) {
      showToast(getErrorMessage(err), 'danger');
    } finally {
      setDeleting(false);
    }
  };

  return (
    <>
      <PageHeader
        title="Customers"
        subtitle="Manage customer master data"
        actions={
          <Link to="/customers/new" className="btn btn-primary">
            <i className="bi bi-plus-lg me-1"></i>Add Customer
          </Link>
        }
      />

      <div className="card">
        <div className="card-body">
          <div className="row g-2 mb-3">
            <div className="col-md-5">
              <div className="input-group">
                <span className="input-group-text">
                  <i className="bi bi-search"></i>
                </span>
                <input
                  type="search"
                  className="form-control"
                  placeholder="Search by name, email, phone or company"
                  aria-label="Search customers"
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                />
              </div>
            </div>
            <div className="col-md-3">
              <select
                className="form-select"
                aria-label="Filter by status"
                value={filters.status}
                onChange={(e) => handleFilterChange('status', e.target.value)}
              >
                <option value="">All Statuses</option>
                {CUSTOMER_STATUSES.map((s) => (
                  <option key={s}>{s}</option>
                ))}
              </select>
            </div>
            {users.length > 1 && (
              <div className="col-md-3">
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
                    <SortableHeader label="Code" field="customerCode" sortBy={sortBy} sortOrder={sortOrder} onSort={handleSort} />
                    <SortableHeader label="Name" field="customerName" sortBy={sortBy} sortOrder={sortOrder} onSort={handleSort} />
                    <SortableHeader label="Company" field="companyName" sortBy={sortBy} sortOrder={sortOrder} onSort={handleSort} />
                    <th>Email / Phone</th>
                    <SortableHeader label="Status" field="status" sortBy={sortBy} sortOrder={sortOrder} onSort={handleSort} />
                    <th>Assigned To</th>
                    <SortableHeader label="Created" field="createdAt" sortBy={sortBy} sortOrder={sortOrder} onSort={handleSort} />
                    <th className="text-end">Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {result?.data.length === 0 && (
                    <tr>
                      <td colSpan={8} className="text-center text-muted py-4">
                        No customers found.
                      </td>
                    </tr>
                  )}
                  {result?.data.map((c) => (
                    <tr key={c.id}>
                      <td className="text-nowrap">{c.customerCode}</td>
                      <td>
                        <Link to={`/customers/${c.id}`} className="fw-semibold text-decoration-none">
                          {c.customerName}
                        </Link>
                      </td>
                      <td>{c.companyName || '-'}</td>
                      <td className="small">
                        <div>{c.email}</div>
                        <div className="text-muted">{c.phone}</div>
                      </td>
                      <td>
                        <StatusBadge status={c.status} />
                      </td>
                      <td>{c.assignedToName}</td>
                      <td className="text-nowrap">{formatDate(c.createdAt)}</td>
                      <td className="text-end text-nowrap">
                        <button className="btn btn-sm btn-outline-secondary me-1" title="View" onClick={() => navigate(`/customers/${c.id}`)}>
                          <i className="bi bi-eye"></i>
                        </button>
                        <button
                          className="btn btn-sm btn-outline-primary me-1"
                          title="Edit"
                          onClick={() => navigate(`/customers/${c.id}/edit`)}
                        >
                          <i className="bi bi-pencil"></i>
                        </button>
                        {canDelete && (
                          <button className="btn btn-sm btn-outline-danger" title="Delete" onClick={() => setDeleteTarget(c)}>
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
        title="Delete Customer"
        message={
          <>
            Are you sure you want to delete <strong>{deleteTarget?.customerName}</strong>? This action will be recorded in the audit log.
          </>
        }
        busy={deleting}
        onConfirm={handleDelete}
        onCancel={() => setDeleteTarget(null)}
      />
    </>
  );
}
