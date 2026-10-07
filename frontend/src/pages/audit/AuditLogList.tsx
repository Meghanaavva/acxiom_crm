import { useCallback, useEffect, useState } from 'react';
import api, { cleanParams, getErrorMessage } from '../../api/client';
import { fetchUserLookup } from '../../api/services';
import Loader from '../../components/Loader';
import Modal from '../../components/Modal';
import PageHeader from '../../components/PageHeader';
import Pagination from '../../components/Pagination';
import StatusBadge from '../../components/StatusBadge';
import { useToast } from '../../context/ToastContext';
import type { AuditLog, PagedResult, UserLookup } from '../../types';
import { formatDateTime } from '../../utils/format';

// old/new values are stored as JSON text - show them nicely formatted
function prettyJson(value: string | null): string {
  if (!value) return '-';
  try {
    return JSON.stringify(JSON.parse(value), null, 2);
  } catch {
    return value;
  }
}

export default function AuditLogList() {
  const { showToast } = useToast();

  const [result, setResult] = useState<PagedResult<AuditLog> | null>(null);
  const [loading, setLoading] = useState(true);
  const [users, setUsers] = useState<UserLookup[]>([]);
  const [options, setOptions] = useState<{ actions: string[]; entities: string[] }>({ actions: [], entities: [] });

  const [search, setSearch] = useState('');
  const [filters, setFilters] = useState({ search: '', userId: '', entityName: '', action: '', result: '', dateFrom: '', dateTo: '' });
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const [selected, setSelected] = useState<AuditLog | null>(null);

  const loadLogs = useCallback(async () => {
    setLoading(true);
    try {
      const { data } = await api.get<PagedResult<AuditLog>>('/audit-logs', {
        params: cleanParams({ ...filters, page, pageSize }),
      });
      setResult(data);
    } catch (err) {
      showToast(getErrorMessage(err), 'danger');
    } finally {
      setLoading(false);
    }
  }, [filters, page, pageSize, showToast]);

  useEffect(() => {
    loadLogs();
  }, [loadLogs]);

  useEffect(() => {
    fetchUserLookup().then(setUsers).catch(() => setUsers([]));
    api
      .get<{ actions: string[]; entities: string[] }>('/audit-logs/filters')
      .then(({ data }) => setOptions(data))
      .catch(() => setOptions({ actions: [], entities: [] }));
  }, []);

  useEffect(() => {
    const timer = setTimeout(() => {
      setFilters((f) => (f.search === search ? f : { ...f, search }));
      setPage(1);
    }, 400);
    return () => clearTimeout(timer);
  }, [search]);

  const handleFilterChange = (name: string, value: string) => {
    setFilters({ ...filters, [name]: value });
    setPage(1);
  };

  const clearFilters = () => {
    setSearch('');
    setFilters({ search: '', userId: '', entityName: '', action: '', result: '', dateFrom: '', dateTo: '' });
    setPage(1);
  };

  return (
    <>
      <PageHeader title="Audit Log" subtitle="Security and business events recorded by the system" />

      <div className="alert alert-info small py-2">
        <i className="bi bi-info-circle me-2"></i>
        Audit records are read-only. They cannot be modified or deleted.
      </div>

      <div className="card">
        <div className="card-body">
          <div className="row g-2 mb-3">
            <div className="col-md-3">
              <input
                type="search"
                className="form-control"
                placeholder="Search user or details"
                aria-label="Search audit log"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
              />
            </div>
            <div className="col-md-2">
              <select className="form-select" aria-label="Filter by user" value={filters.userId} onChange={(e) => handleFilterChange('userId', e.target.value)}>
                <option value="">All Users</option>
                {users.map((u) => (
                  <option key={u.id} value={u.id}>
                    {u.name}
                  </option>
                ))}
              </select>
            </div>
            <div className="col-md-2">
              <select className="form-select" aria-label="Filter by module" value={filters.entityName} onChange={(e) => handleFilterChange('entityName', e.target.value)}>
                <option value="">All Modules</option>
                {options.entities.map((m) => (
                  <option key={m}>{m}</option>
                ))}
              </select>
            </div>
            <div className="col-md-2">
              <select className="form-select" aria-label="Filter by action" value={filters.action} onChange={(e) => handleFilterChange('action', e.target.value)}>
                <option value="">All Actions</option>
                {options.actions.map((a) => (
                  <option key={a}>{a}</option>
                ))}
              </select>
            </div>
            <div className="col-md-3">
              <select className="form-select" aria-label="Filter by result" value={filters.result} onChange={(e) => handleFilterChange('result', e.target.value)}>
                <option value="">All Results</option>
                <option>Success</option>
                <option>Failure</option>
              </select>
            </div>
            <div className="col-md-3">
              <input type="date" className="form-control" aria-label="From date" title="From date" value={filters.dateFrom} onChange={(e) => handleFilterChange('dateFrom', e.target.value)} />
            </div>
            <div className="col-md-3">
              <input type="date" className="form-control" aria-label="To date" title="To date" value={filters.dateTo} onChange={(e) => handleFilterChange('dateTo', e.target.value)} />
            </div>
            <div className="col-md-2">
              <button className="btn btn-outline-secondary w-100" onClick={clearFilters}>
                Clear Filters
              </button>
            </div>
          </div>

          {loading && !result ? (
            <Loader />
          ) : (
            <div className="table-responsive">
              <table className="table table-hover table-sm align-middle mb-0">
                <thead>
                  <tr>
                    <th>Date &amp; Time</th>
                    <th>User</th>
                    <th>Action</th>
                    <th>Module</th>
                    <th>Record</th>
                    <th>Result</th>
                    <th>Details</th>
                    <th>IP Address</th>
                    <th></th>
                  </tr>
                </thead>
                <tbody>
                  {result?.data.length === 0 && (
                    <tr>
                      <td colSpan={9} className="text-center text-muted py-4">
                        No audit records found.
                      </td>
                    </tr>
                  )}
                  {result?.data.map((log) => (
                    <tr key={log.id}>
                      <td className="text-nowrap small">{formatDateTime(log.createdAt)}</td>
                      <td>{log.userName || <span className="text-muted">System / Anonymous</span>}</td>
                      <td>
                        <span className="badge bg-light text-dark border">{log.action}</span>
                      </td>
                      <td>{log.entityName}</td>
                      <td>{log.recordId || '-'}</td>
                      <td>
                        <StatusBadge status={log.result} />
                      </td>
                      <td className="small text-truncate" style={{ maxWidth: 240 }} title={log.details || ''}>
                        {log.details || '-'}
                      </td>
                      <td className="small text-muted">{log.ipAddress || '-'}</td>
                      <td className="text-end">
                        {(log.oldValue || log.newValue) && (
                          <button className="btn btn-sm btn-outline-secondary" title="View changes" aria-label="View changes" onClick={() => setSelected(log)}>
                            <i className="bi bi-eye"></i>
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

      <Modal show={!!selected} title={`Audit #${selected?.id} - ${selected?.action} ${selected?.entityName}`} onClose={() => setSelected(null)} size="lg">
        <p className="small text-muted mb-3">
          {selected?.userName || 'Anonymous'} &middot; {formatDateTime(selected?.createdAt)} &middot; Record {selected?.recordId || '-'}
        </p>
        <div className="row g-3">
          <div className="col-md-6">
            <h6>Old Value</h6>
            <pre className="bg-light border rounded p-2 small mb-0" style={{ maxHeight: 350 }}>
              {prettyJson(selected?.oldValue ?? null)}
            </pre>
          </div>
          <div className="col-md-6">
            <h6>New Value</h6>
            <pre className="bg-light border rounded p-2 small mb-0" style={{ maxHeight: 350 }}>
              {prettyJson(selected?.newValue ?? null)}
            </pre>
          </div>
        </div>
      </Modal>
    </>
  );
}
