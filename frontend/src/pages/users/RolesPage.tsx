import { useEffect, useState } from 'react';
import api, { getErrorMessage } from '../../api/client';
import Loader from '../../components/Loader';
import PageHeader from '../../components/PageHeader';
import { useToast } from '../../context/ToastContext';
import { ROLE_LABELS } from '../../utils/constants';

interface RoleInfo {
  id: number;
  name: string;
  description: string | null;
  userCount: number;
}

interface PermissionMatrix {
  modules: string[];
  matrix: Record<string, Record<string, string>>;
}

// colour the access level so the matrix is easy to read
function accessClass(value: string): string {
  const v = value.toLowerCase();
  if (v === 'no' || v === 'none') return 'text-danger';
  if (v.startsWith('full') || v === 'all') return 'text-success fw-semibold';
  return 'text-primary';
}

export default function RolesPage() {
  const { showToast } = useToast();
  const [roles, setRoles] = useState<RoleInfo[]>([]);
  const [permissions, setPermissions] = useState<PermissionMatrix | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    Promise.all([api.get<RoleInfo[]>('/roles'), api.get<PermissionMatrix>('/roles/permissions')])
      .then(([rolesRes, permRes]) => {
        setRoles(rolesRes.data);
        setPermissions(permRes.data);
      })
      .catch((err) => showToast(getErrorMessage(err), 'danger'))
      .finally(() => setLoading(false));
  }, [showToast]);

  if (loading) return <Loader />;

  const roleNames = permissions ? Object.keys(permissions.matrix) : [];

  return (
    <>
      <PageHeader title="Roles & Permissions" subtitle="Each user has exactly one role. Permissions are enforced on the server." />

      <div className="row g-3 mb-4">
        {roles.map((r) => (
          <div className="col-md-4" key={r.id}>
            <div className="card h-100">
              <div className="card-body">
                <div className="d-flex justify-content-between align-items-start">
                  <h2 className="h6 mb-1">
                    <i className="bi bi-shield-lock me-2 text-primary"></i>
                    {ROLE_LABELS[r.name] || r.name}
                  </h2>
                  <span className="badge bg-light text-dark border">{r.userCount} users</span>
                </div>
                <p className="small text-muted mb-0">{r.description || '-'}</p>
              </div>
            </div>
          </div>
        ))}
      </div>

      {permissions && (
        <div className="card">
          <div className="card-header bg-white fw-semibold">Permission Matrix</div>
          <div className="table-responsive">
            <table className="table table-bordered align-middle mb-0">
              <thead>
                <tr>
                  <th>Module</th>
                  {roleNames.map((role) => (
                    <th key={role}>{ROLE_LABELS[role] || role}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {permissions.modules.map((module) => (
                  <tr key={module}>
                    <td className="fw-semibold">{module}</td>
                    {roleNames.map((role) => {
                      const value = permissions.matrix[role]?.[module] || 'No';
                      return (
                        <td key={role} className={accessClass(value)}>
                          {value}
                        </td>
                      );
                    })}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </>
  );
}
