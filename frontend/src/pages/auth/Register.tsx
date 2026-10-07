import { useState, type ChangeEvent, type FormEvent } from 'react';
import { Link, Navigate, useNavigate } from 'react-router-dom';
import api, { getErrorMessage, getFieldErrors } from '../../api/client';
import { useAuth } from '../../context/AuthContext';
import { useToast } from '../../context/ToastContext';
import { collectErrors, email, maxLength, passwordPolicy, required, type Errors } from '../../utils/validation';

const emptyForm = { name: '', email: '', password: '', confirmPassword: '' };

export default function Register() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const { showToast } = useToast();

  const [form, setForm] = useState(emptyForm);
  const [errors, setErrors] = useState<Errors>({});
  const [serverError, setServerError] = useState('');
  const [submitting, setSubmitting] = useState(false);

  if (user) return <Navigate to="/dashboard" replace />;

  const handleChange = (e: ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setForm({ ...form, [name]: value });
    if (errors[name]) setErrors({ ...errors, [name]: '' });
  };

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setServerError('');
    const validationErrors = collectErrors({
      name: [required(form.name, 'Name'), maxLength(form.name, 100, 'Name')],
      email: [required(form.email, 'Email'), email(form.email), maxLength(form.email, 150, 'Email')],
      password: [passwordPolicy(form.password)],
      confirmPassword: [
        required(form.confirmPassword, 'Confirm Password'),
        form.confirmPassword && form.confirmPassword !== form.password ? 'Passwords do not match.' : '',
      ],
    });
    setErrors(validationErrors);
    if (Object.keys(validationErrors).length > 0) return;

    setSubmitting(true);
    try {
      await api.post('/auth/register', form);
      showToast('Registration successful. Please login.');
      navigate('/login');
    } catch (err) {
      setErrors(getFieldErrors(err));
      setServerError(getErrorMessage(err));
    } finally {
      setSubmitting(false);
    }
  };

  const inputClass = (field: string) => `form-control ${errors[field] ? 'is-invalid' : ''}`;

  return (
    <div className="auth-page d-flex align-items-center justify-content-center p-3">
      <div className="card shadow" style={{ maxWidth: 460, width: '100%' }}>
        <div className="card-body p-4">
          <div className="text-center mb-4">
            <i className="bi bi-diagram-3-fill text-primary fs-1"></i>
            <h1 className="h4 fw-bold mt-2 mb-0">Create Account</h1>
            <p className="text-muted small">New accounts are created as Sales Executive</p>
          </div>

          {serverError && <div className="alert alert-danger py-2 small">{serverError}</div>}

          <form onSubmit={handleSubmit} noValidate>
            <div className="mb-3">
              <label htmlFor="name" className="form-label">
                Full Name <span className="text-danger">*</span>
              </label>
              <input id="name" name="name" className={inputClass('name')} maxLength={100} value={form.name} onChange={handleChange} />
              {errors.name && <div className="invalid-feedback">{errors.name}</div>}
            </div>
            <div className="mb-3">
              <label htmlFor="email" className="form-label">
                Email <span className="text-danger">*</span>
              </label>
              <input id="email" name="email" type="email" className={inputClass('email')} maxLength={150} value={form.email} onChange={handleChange} />
              {errors.email && <div className="invalid-feedback">{errors.email}</div>}
            </div>
            <div className="mb-3">
              <label htmlFor="password" className="form-label">
                Password <span className="text-danger">*</span>
              </label>
              <input
                id="password"
                name="password"
                type="password"
                autoComplete="new-password"
                className={inputClass('password')}
                maxLength={64}
                value={form.password}
                onChange={handleChange}
              />
              {errors.password ? (
                <div className="invalid-feedback">{errors.password}</div>
              ) : (
                <div className="form-text">Min 8 characters with uppercase, lowercase, number and special character.</div>
              )}
            </div>
            <div className="mb-3">
              <label htmlFor="confirmPassword" className="form-label">
                Confirm Password <span className="text-danger">*</span>
              </label>
              <input
                id="confirmPassword"
                name="confirmPassword"
                type="password"
                autoComplete="new-password"
                className={inputClass('confirmPassword')}
                maxLength={64}
                value={form.confirmPassword}
                onChange={handleChange}
              />
              {errors.confirmPassword && <div className="invalid-feedback">{errors.confirmPassword}</div>}
            </div>
            <button type="submit" className="btn btn-primary w-100" disabled={submitting}>
              {submitting && <span className="spinner-border spinner-border-sm me-1"></span>}
              Register
            </button>
          </form>

          <p className="text-center small mt-3 mb-0">
            Already have an account? <Link to="/login">Login</Link>
          </p>
        </div>
      </div>
    </div>
  );
}
