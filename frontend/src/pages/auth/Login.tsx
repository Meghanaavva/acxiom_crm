import { useState, type ChangeEvent, type FormEvent } from 'react';
import { Link, Navigate, useLocation, useNavigate, useSearchParams } from 'react-router-dom';
import { getErrorMessage } from '../../api/client';
import { useAuth } from '../../context/AuthContext';
import { collectErrors, email, required, type Errors } from '../../utils/validation';

const DEMO_USERS = [
  { label: 'Admin', email: 'admin@acxiomcrm.com', password: 'Admin@123' },
  { label: 'Manager', email: 'manager@acxiomcrm.com', password: 'Manager@123' },
  { label: 'Sales Executive', email: 'sales1@acxiomcrm.com', password: 'Sales@123' },
];

export default function Login() {
  const { user, login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [searchParams] = useSearchParams();

  const [form, setForm] = useState({ email: '', password: '' });
  const [errors, setErrors] = useState<Errors>({});
  const [serverError, setServerError] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const sessionExpired = searchParams.get('expired') === '1';
  const redirectTo = (location.state as { from?: string } | null)?.from || '/dashboard';

  // already logged in - no need to see the login page
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
      email: [required(form.email, 'Email'), email(form.email)],
      password: [required(form.password, 'Password')],
    });
    setErrors(validationErrors);
    if (Object.keys(validationErrors).length > 0) return;

    setSubmitting(true);
    try {
      await login(form.email.trim(), form.password);
      navigate(redirectTo, { replace: true });
    } catch (err) {
      setServerError(getErrorMessage(err, 'Login failed. Please try again.'));
    } finally {
      setSubmitting(false);
    }
  };

  const fillDemo = (demoEmail: string, demoPassword: string) => {
    setForm({ email: demoEmail, password: demoPassword });
    setErrors({});
    setServerError('');
  };

  return (
    <div className="auth-page d-flex align-items-center justify-content-center p-3">
      <div className="card shadow" style={{ maxWidth: 420, width: '100%' }}>
        <div className="card-body p-4">
          <div className="text-center mb-4">
            <i className="bi bi-diagram-3-fill text-primary fs-1"></i>
            <h1 className="h4 fw-bold mt-2 mb-0">AcxiomCRM</h1>
            <p className="text-muted small">Sign in to your account</p>
          </div>

          {sessionExpired && !serverError && (
            <div className="alert alert-warning py-2 small">Your session has expired. Please login again.</div>
          )}
          {serverError && <div className="alert alert-danger py-2 small">{serverError}</div>}

          <form onSubmit={handleSubmit} noValidate>
            <div className="mb-3">
              <label htmlFor="email" className="form-label">
                Email
              </label>
              <input
                id="email"
                name="email"
                type="email"
                autoComplete="username"
                className={`form-control ${errors.email ? 'is-invalid' : ''}`}
                value={form.email}
                onChange={handleChange}
              />
              {errors.email && <div className="invalid-feedback">{errors.email}</div>}
            </div>
            <div className="mb-3">
              <label htmlFor="password" className="form-label">
                Password
              </label>
              <input
                id="password"
                name="password"
                type="password"
                autoComplete="current-password"
                className={`form-control ${errors.password ? 'is-invalid' : ''}`}
                value={form.password}
                onChange={handleChange}
              />
              {errors.password && <div className="invalid-feedback">{errors.password}</div>}
            </div>
            <button type="submit" className="btn btn-primary w-100" disabled={submitting}>
              {submitting && <span className="spinner-border spinner-border-sm me-1"></span>}
              Login
            </button>
          </form>

          <p className="text-center small mt-3 mb-0">
            Don't have an account? <Link to="/register">Register</Link>
          </p>

          <div className="border rounded p-2 mt-4 bg-light small">
            <div className="fw-semibold mb-1">
              <i className="bi bi-info-circle me-1"></i>Demo accounts (click to fill)
            </div>
            {DEMO_USERS.map((d) => (
              <button
                key={d.email}
                type="button"
                className="btn btn-link btn-sm p-0 d-block text-start text-decoration-none"
                onClick={() => fillDemo(d.email, d.password)}
              >
                <strong>{d.label}:</strong> {d.email} / {d.password}
              </button>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}
