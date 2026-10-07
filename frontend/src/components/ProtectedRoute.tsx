import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import type { Role } from '../types';
import Loader from './Loader';

interface Props {
  roles?: Role[];
}

// Blocks anonymous users, and optionally users without one of the given roles.
// Note: the server checks roles again on every API call, this is only for the UI.
export default function ProtectedRoute({ roles }: Props) {
  const { user, loading } = useAuth();
  const location = useLocation();

  if (loading) return <Loader fullPage />;
  if (!user) return <Navigate to="/login" replace state={{ from: location.pathname }} />;
  if (roles && !roles.includes(user.role)) return <Navigate to="/forbidden" replace />;
  return <Outlet />;
}
