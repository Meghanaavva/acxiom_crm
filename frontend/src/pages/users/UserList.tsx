import { useCallback, useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import api, { cleanParams, getErrorMessage, getFieldErrors } from '../../api/client';
import ConfirmDialog from '../../components/ConfirmDialog';
import Loader from '../../components/Loader';
import Modal from '../../components/Modal';
import PageHeader from '../../components/PageHeader';
import Pagination from '../../components/Pagination';
import { useAuth } from '../../context/AuthContext';
import { useToast } from '../../context/ToastContext';
import type { PagedResult, User } from '../../types';
import { ROLE_LABELS } from '../../utils/constants';
import { formatDateTime } from '../../utils/format';
import { passwordPolicy } from '../../utils/validation';

const ROLE_COLORS: Record<string, string> = {
  Admin: 'danger',
  Manager: 'primary',
  SalesExecutive: 'info',
};

type ConfirmAction = { type: 'status' | 'unlock'; user: User } | null;

export default function UserList() {
  const navigate = useNavigate();
  const { user: currentUser, hasRole } = useAuth();
  const { showToast } = useToast();
  const isAdmin = hasRole('Admin');

  const [result, setResult] = useState<PagedResult<User> | null>(null);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');
  const [filters, setFilters] = useState({ search: '', role: '', isActive: '' });
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);

  const [confirmAction, setConfirmAction] = useState<ConfirmAction>(null);
  const [resetTarget, setResetTarget] = useState<User | null>(null);
  const [resetForm, setResetForm] = useState({ newPassword: '', confirmPassword: '' });
  const [resetErrors, setResetErrors] = useState<Record<string, string>>({});
  const [busy, setBusy] = useState(false);

  const loadUsers = useCallback(async () => {
    setLoading(true);
    try {
      const { data } = await api.get<PagedResult<User>>('/users', {
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
    loadUsers();
  }, [loadUsers]);

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

  const handleConfirm = async () => {
    if (!confirmAction) return;
    const { type, user } = confirmAction;
    setBusy(true);
    try {
      if (type === 'status') {
        await api.patch(`/users/${user.id}/status`, { isActive: !user.isActive });
        showToast(`User ${user.isActive ? 'deactivated' : 'activated'} successfully.`);
      } else {
        await api.post(`/users/${user.id}/unlock`);
        showToast('User account unlocked.');
      }
      setConfirmAction(null);
      loadUsers();
    } catch (err) {
      showToast(getErrorMessage(err), 'danger');
    } finally {
      setBusy(false);
    }
  };

  const handleResetPassword = async () => {
    if (!resetTarget) return;
    const errors: Record<string, string> = {};
    const policyError = passwordPolicy(resetForm.newPassword);
    if (policyError) errors.newPassword = policyError;
    if (resetForm.newPassword !== resetForm.confirmPassword) errors.confirmPassword = 'Passwords do not match.';
    setResetErrors(errors);
    if (Object.keys(errors).length > 0) return;

    setBusy(true);
    try {
      await api.post(`/users/${resetTarget.id}/reset-password`, { newPassword: resetForm.newPassword });
      showToast('Password reset successfully.');
      setResetTarget(null);
      loadUsers();
    } catch (err) {
      const fieldErrors = getFieldErrors(err);
      setResetErrors(Object.keys(fieldErrors).length ? fieldErrors : { newPassword: getErrorMessage(err) });
    } finally {
      setBusy(false);
    }
  };

  return (
    <>
      <PageHeader
        title="Users"
        subtitle={isAdmin ? 'Create users, assign roles and manage account access' : 'Members of your team'}
        actions={
          isAdmin && (
            <Link to="/users/new" className="btn btn-primary">
              <i className="bi bi-plus-lg me-1"></i>Add User
            </Link>
          )
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
                  placeholder="Search by name or email"
                  aria-label="Search users"
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                />
              </div>
            </div>
            <div className="col-md-3">
              <select className="form-select" aria-label="Filter by role" value={filters.role} onChange={(e) => handleFilterChange('role', e.target.value)}>
                <option value="">All Roles</option>
                {Object.entries(ROLE_LABELS).map(([value, label]) => (
                  <option key={value} value={value}>
                    {label}
                  </option>
                ))}
              </select>
            </div>
            <div className="col-md-3">
              <select className="form-select" aria-label="Filter by status" value={filters.isActive} onChange={(e) => handleFilterChange('isActive', e.target.value)}>
                <option value="">All Statuses</option>
                <option value="true">Active</option>
                <option value="false">Inactive</option>
              </select>
            </div>
          </div>

          {loading && !result ? (
            <Loader />
          ) : (
            <div className="table-responsive">
              <table className="table table-hover align-middle mb-0">
                <thead>
                  <tr>
                    <th>Name</th>
                    <th>Role</th>
                    <th>Manager</th>
                    <th>Status</th>
                    <th>Lockout</th>
                    <th className="text-center">Failed Attempts</th>
                    <th>Last Login</th>
                    {isAdmin && <th className="text-end">Actions</th>}
                  </tr>
                </thead>
                <tbody>
                  {result?.data.length === 0 && (
                    <tr>
                      <td colSpan={8} className="text-center text-muted py-4">
                        No users found.
                      </td>
                    </tr>
                  )}
                  {result?.data.map((u) => (
                    <tr key={u.id}>
                      <td>
                        <div className="fw-semibold">{u.name}</div>
                        <div className="small text-muted">{u.email}</div>
                      </td>
                      <td>
                        <span className={`badge bg-${ROLE_COLORS[u.role] || 'secondary'} ${u.role === 'SalesExecutive' ? 'text-dark' : ''}`}>
                          {ROLE_LABELS[u.role] || u.role}
                        </span>
                      </td>
                      <td>{u.managerName || '-'}</td>
                      <td>
                        <span className={`badge ${u.isActive ? 'bg-success' : 'bg-secondary'}`}>{u.isActive ? 'Active' : 'Inactive'}</span>
                      </td>
                      <td>
                        {u.isLockedOut ? (
                          <span className="badge bg-danger" title={`Locked until ${formatDateTime(u.lockoutEnd)}`}>
                            <i className="bi bi-lock-fill me-1"></i>Locked
                          </span>
                        ) : (
                          <span className="text-muted small">Not locked</span>
                        )}
                      </td>
                      <td className="text-center">{u.failedLoginCount}</td>
                      <td className="text-nowrap small">{formatDateTime(u.lastLoginAt)}</td>
                      {isAdmin && (
                        <td className="text-end text-nowrap">
                          <button className="btn btn-sm btn-outline-primary me-1" title="Edit" aria-label="Edit user" onClick={() => navigate(`/users/${u.id}/edit`)}>
                            <i className="bi bi-pencil"></i>
                          </button>
                          <button
                            className="btn btn-sm btn-outline-secondary me-1"
                            title="Reset password"
                            aria-label="Reset password"
                            onClick={() => {
                              setResetForm({ newPassword: '', confirmPassword: '' });
                              setResetErrors({});
                              setResetTarget(u);
                            }}
                          >
                            <i className="bi bi-key"></i>
                          </button>
                          {u.isLockedOut && (
                            <button className="btn btn-sm btn-outline-warning me-1" title="Unlock" aria-label="Unlock account" onClick={() => setConfirmAction({ type: 'unlock', user: u })}>
                              <i className="bi bi-unlock"></i>
                            </button>
                          )}
                          {u.id !== currentUser?.id && (
                            <button
                              className={`btn btn-sm ${u.isActive ? 'btn-outline-danger' : 'btn-outline-success'}`}
                              title={u.isActive ? 'Deactivate' : 'Activate'}
                              aria-label={u.isActive ? 'Deactivate user' : 'Activate user'}
                              onClick={() => setConfirmAction({ type: 'status', user: u })}
                            >
                              <i className={`bi ${u.isActive ? 'bi-person-x' : 'bi-person-check'}`}></i>
                            </button>
                          )}
                        </td>
                      )}
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
        show={!!confirmAction}
        title={confirmAction?.type === 'unlock' ? 'Unlock Account' : confirmAction?.user.isActive ? 'Deactivate User' : 'Activate User'}
        message={
          confirmAction?.type === 'unlock' ? (
            <>
              Unlock the account of <strong>{confirmAction.user.name}</strong>? The failed login count will be reset.
            </>
          ) : (
            <>
              Are you sure you want to {confirmAction?.user.isActive ? 'deactivate' : 'activate'} <strong>{confirmAction?.user.name}</strong>?
              {confirmAction?.user.isActive && ' The user will not be able to login.'}
            </>
          )
        }
        confirmText={confirmAction?.type === 'unlock' ? 'Unlock' : confirmAction?.user.isActive ? 'Deactivate' : 'Activate'}
        confirmVariant={confirmAction?.type === 'status' && confirmAction.user.isActive ? 'danger' : 'primary'}
        busy={busy}
        onConfirm={handleConfirm}
        onCancel={() => setConfirmAction(null)}
      />

      <Modal
        show={!!resetTarget}
        title="Reset Password"
        onClose={() => setResetTarget(null)}
        footer={
          <>
            <button className="btn btn-outline-secondary" onClick={() => setResetTarget(null)} disabled={busy}>
              Cancel
            </button>
            <button className="btn btn-primary" onClick={handleResetPassword} disabled={busy}>
              {busy && <span className="spinner-border spinner-border-sm me-1"></span>}
              Reset Password
            </button>
          </>
        }
      >
        <p className="small text-muted">
          Set a new password for <strong>{resetTarget?.name}</strong>. Min 8 characters with uppercase, lowercase, number and special character.
        </p>
        <div className="mb-3">
          <label htmlFor="newPassword" className="form-label">
            New Password <span className="text-danger">*</span>
          </label>
          <input
            id="newPassword"
            type="password"
            autoComplete="new-password"
            className={`form-control ${resetErrors.newPassword ? 'is-invalid' : ''}`}
            value={resetForm.newPassword}
            onChange={(e) => setResetForm({ ...resetForm, newPassword: e.target.value })}
          />
          {resetErrors.newPassword && <div className="invalid-feedback d-block">{resetErrors.newPassword}</div>}
        </div>
        <div>
          <label htmlFor="confirmNewPassword" className="form-label">
            Confirm Password <span className="text-danger">*</span>
          </label>
          <input
            id="confirmNewPassword"
            type="password"
            autoComplete="new-password"
            className={`form-control ${resetErrors.confirmPassword ? 'is-invalid' : ''}`}
            value={resetForm.confirmPassword}
            onChange={(e) => setResetForm({ ...resetForm, confirmPassword: e.target.value })}
          />
          {resetErrors.confirmPassword && <div className="invalid-feedback d-block">{resetErrors.confirmPassword}</div>}
        </div>
      </Modal>
    </>
  );
}
