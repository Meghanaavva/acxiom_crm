import { Link } from 'react-router-dom';

export default function Forbidden() {
  return (
    <div className="text-center py-5">
      <i className="bi bi-shield-lock display-3 text-danger"></i>
      <h2 className="mt-3">Access Denied</h2>
      <p className="text-muted">You do not have permission to view this page.</p>
      <Link to="/dashboard" className="btn btn-primary">
        Go to Dashboard
      </Link>
    </div>
  );
}
