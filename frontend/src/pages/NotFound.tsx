import { Link } from 'react-router-dom';

export default function NotFound() {
  return (
    <div className="text-center py-5">
      <h1 className="display-4 fw-bold text-primary">404</h1>
      <p className="lead">The page you are looking for does not exist.</p>
      <Link to="/dashboard" className="btn btn-primary">
        Go to Dashboard
      </Link>
    </div>
  );
}
