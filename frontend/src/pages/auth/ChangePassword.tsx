import { useState, type ChangeEvent, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import api, { getErrorMessage, getFieldErrors } from '../../api/client';
import FormField from '../../components/FormField';
import PageHeader from '../../components/PageHeader';
import { useAuth } from '../../context/AuthContext';
import { useToast } from '../../context/ToastContext';
import { collectErrors, passwordPolicy, required, type Errors } from '../../utils/validation';

const emptyForm = { currentPassword: '', newPassword: '', confirmPassword: '' };

export default function ChangePassword() {
  const { logout } = useAuth();
  const navigate = useNavigate();
  const { showToast } = useToast();

  const [form, setForm] = useState(emptyForm);
  const [errors, setErrors] = useState<Errors>({});
  const [serverError, setServerError] = useState('');
  const [saving, setSaving] = useState(false);

  const handleChange = (e: ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setForm({ ...form, [name]: value });
    if (errors[name]) setErrors({ ...errors, [name]: '' });
  };

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setServerError('');
    const validationErrors = collectErrors({
      currentPassword: [required(form.currentPassword, 'Current Password')],
      newPassword: [
        passwordPolicy(form.newPassword),
        form.newPassword && form.newPassword === form.currentPassword ? 'New password must be different from the current password.' : '',
      ],
      confirmPassword: [
        required(form.confirmPassword, 'Confirm Password'),
        form.confirmPassword && form.confirmPassword !== form.newPassword ? 'Passwords do not match.' : '',
      ],
    });
    setErrors(validationErrors);
    if (Object.keys(validationErrors).length > 0) return;

    setSaving(true);
    try {
      await api.post('/auth/change-password', form);
      showToast('Password changed successfully. Please login again.');
      // server has invalidated the old token, so clear the session
      await logout();
      navigate('/login', { replace: true });
    } catch (err) {
      setErrors(getFieldErrors(err));
      setServerError(getErrorMessage(err));
    } finally {
      setSaving(false);
    }
  };

  const inputClass = (field: string) => `form-control ${errors[field] ? 'is-invalid' : ''}`;

  return (
    <>
      <PageHeader title="Change Password" subtitle="You will be logged out after changing your password" />
      <div className="card" style={{ maxWidth: 520 }}>
        <div className="card-body">
          {serverError && <div className="alert alert-danger">{serverError}</div>}
          <form onSubmit={handleSubmit} noValidate>
            <div className="row g-3">
              <FormField label="Current Password" name="currentPassword" error={errors.currentPassword} required className="col-12">
                <input
                  id="currentPassword"
                  name="currentPassword"
                  type="password"
                  autoComplete="current-password"
                  className={inputClass('currentPassword')}
                  value={form.currentPassword}
                  onChange={handleChange}
                />
              </FormField>
              <FormField
                label="New Password"
                name="newPassword"
                error={errors.newPassword}
                required
                className="col-12"
                hint="Min 8 characters with uppercase, lowercase, number and special character."
              >
                <input
                  id="newPassword"
                  name="newPassword"
                  type="password"
                  autoComplete="new-password"
                  maxLength={64}
                  className={inputClass('newPassword')}
                  value={form.newPassword}
                  onChange={handleChange}
                />
              </FormField>
              <FormField label="Confirm New Password" name="confirmPassword" error={errors.confirmPassword} required className="col-12">
                <input
                  id="confirmPassword"
                  name="confirmPassword"
                  type="password"
                  autoComplete="new-password"
                  maxLength={64}
                  className={inputClass('confirmPassword')}
                  value={form.confirmPassword}
                  onChange={handleChange}
                />
              </FormField>
            </div>
            <button type="submit" className="btn btn-primary mt-4" disabled={saving}>
              {saving && <span className="spinner-border spinner-border-sm me-1"></span>}
              Change Password
            </button>
          </form>
        </div>
      </div>
    </>
  );
}
