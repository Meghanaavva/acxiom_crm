import { useEffect, useState, type ChangeEvent, type FormEvent } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import api, { getErrorMessage, getFieldErrors } from '../../api/client';
import FormField from '../../components/FormField';
import Loader from '../../components/Loader';
import PageHeader from '../../components/PageHeader';
import { useToast } from '../../context/ToastContext';
import type { User } from '../../types';
import { ROLE_LABELS } from '../../utils/constants';
import { collectErrors, email, maxLength, passwordPolicy, required, type Errors } from '../../utils/validation';

const emptyForm = {
  name: '',
  email: '',
  password: '',
  confirmPassword: '',
  role: 'SalesExecutive',
  managerId: '',
  isActive: true,
};

type FormState = typeof emptyForm;

function validateForm(form: FormState, isEdit: boolean): Errors {
  const errors = collectErrors({
    name: [required(form.name, 'Name'), maxLength(form.name, 100, 'Name')],
    email: [required(form.email, 'Email'), email(form.email), maxLength(form.email, 150, 'Email')],
    role: [required(form.role, 'Role')],
  });
  // password is only set while creating, admins use "Reset Password" later
  if (!isEdit) {
    const pwdError = passwordPolicy(form.password);
    if (pwdError) errors.password = pwdError;
    if (form.password !== form.confirmPassword) errors.confirmPassword = 'Passwords do not match.';
  }
  return errors;
}

export default function UserForm() {
  const { id } = useParams();
  const isEdit = !!id;
  const navigate = useNavigate();
  const { showToast } = useToast();

  const [form, setForm] = useState<FormState>(emptyForm);
  const [errors, setErrors] = useState<Errors>({});
  const [managers, setManagers] = useState<{ id: number; name: string }[]>([]);
  const [loading, setLoading] = useState(isEdit);
  const [saving, setSaving] = useState(false);
  const [serverError, setServerError] = useState('');

  useEffect(() => {
    api
      .get<{ id: number; name: string }[]>('/users/managers')
      .then(({ data }) => setManagers(data))
      .catch(() => setManagers([]));
  }, []);

  useEffect(() => {
    if (!isEdit) return;
    api
      .get<User>(`/users/${id}`)
      .then(({ data }) => {
        setForm({
          ...emptyForm,
          name: data.name,
          email: data.email,
          role: data.role,
          managerId: data.managerId ? String(data.managerId) : '',
          isActive: data.isActive,
        });
      })
      .catch((err) => {
        showToast(getErrorMessage(err), 'danger');
        navigate('/users');
      })
      .finally(() => setLoading(false));
  }, [id, isEdit, navigate, showToast]);

  const handleChange = (e: ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
    const { name, value, type } = e.target;
    const newValue = type === 'checkbox' ? (e.target as HTMLInputElement).checked : value;
    setForm({ ...form, [name]: newValue });
    if (errors[name]) setErrors({ ...errors, [name]: '' });
  };

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setServerError('');
    const validationErrors = validateForm(form, isEdit);
    setErrors(validationErrors);
    if (Object.keys(validationErrors).length > 0) return;

    setSaving(true);
    // only sales executives report to a manager
    const managerId = form.role === 'SalesExecutive' && form.managerId ? Number(form.managerId) : null;
    try {
      if (isEdit) {
        await api.put(`/users/${id}`, { name: form.name, email: form.email, role: form.role, managerId, isActive: form.isActive });
        showToast('User updated successfully.');
      } else {
        await api.post('/users', {
          name: form.name,
          email: form.email,
          password: form.password,
          role: form.role,
          managerId,
          isActive: form.isActive,
        });
        showToast('User created successfully.');
      }
      navigate('/users');
    } catch (err) {
      setErrors(getFieldErrors(err));
      setServerError(getErrorMessage(err));
    } finally {
      setSaving(false);
    }
  };

  if (loading) return <Loader />;

  const inputClass = (field: string) => `form-control ${errors[field] ? 'is-invalid' : ''}`;

  return (
    <>
      <PageHeader title={isEdit ? 'Edit User' : 'Add User'} subtitle="Fields marked * are mandatory" />

      <div className="card">
        <div className="card-body">
          {serverError && <div className="alert alert-danger">{serverError}</div>}

          <form onSubmit={handleSubmit} noValidate>
            <div className="row g-3">
              <FormField label="Full Name" name="name" error={errors.name} required>
                <input id="name" name="name" className={inputClass('name')} maxLength={100} value={form.name} onChange={handleChange} />
              </FormField>
              <FormField label="Email" name="email" error={errors.email} required>
                <input id="email" name="email" type="email" autoComplete="off" className={inputClass('email')} maxLength={150} value={form.email} onChange={handleChange} />
              </FormField>

              {!isEdit && (
                <>
                  <FormField label="Password" name="password" error={errors.password} required hint="Min 8 characters with uppercase, lowercase, number and special character">
                    <input id="password" name="password" type="password" autoComplete="new-password" className={inputClass('password')} value={form.password} onChange={handleChange} />
                  </FormField>
                  <FormField label="Confirm Password" name="confirmPassword" error={errors.confirmPassword} required>
                    <input
                      id="confirmPassword"
                      name="confirmPassword"
                      type="password"
                      autoComplete="new-password"
                      className={inputClass('confirmPassword')}
                      value={form.confirmPassword}
                      onChange={handleChange}
                    />
                  </FormField>
                </>
              )}

              <FormField label="Role" name="role" error={errors.role} required>
                <select id="role" name="role" className={`form-select ${errors.role ? 'is-invalid' : ''}`} value={form.role} onChange={handleChange}>
                  {Object.entries(ROLE_LABELS).map(([value, label]) => (
                    <option key={value} value={value}>
                      {label}
                    </option>
                  ))}
                </select>
              </FormField>

              {form.role === 'SalesExecutive' && (
                <FormField label="Reports To (Manager)" name="managerId" error={errors.managerId}>
                  <select id="managerId" name="managerId" className={`form-select ${errors.managerId ? 'is-invalid' : ''}`} value={form.managerId} onChange={handleChange}>
                    <option value="">-- No Manager --</option>
                    {managers.map((m) => (
                      <option key={m.id} value={m.id}>
                        {m.name}
                      </option>
                    ))}
                  </select>
                </FormField>
              )}

              <div className="col-12">
                <div className="form-check form-switch">
                  <input id="isActive" name="isActive" type="checkbox" className="form-check-input" checked={form.isActive} onChange={handleChange} />
                  <label htmlFor="isActive" className="form-check-label">
                    Active (user can login)
                  </label>
                </div>
              </div>
            </div>

            <div className="d-flex gap-2 mt-4">
              <button type="submit" className="btn btn-primary" disabled={saving}>
                {saving && <span className="spinner-border spinner-border-sm me-1"></span>}
                {isEdit ? 'Update User' : 'Create User'}
              </button>
              <Link to="/users" className="btn btn-outline-secondary">
                Cancel
              </Link>
            </div>
          </form>
        </div>
      </div>
    </>
  );
}
