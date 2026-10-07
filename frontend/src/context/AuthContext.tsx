import { createContext, useCallback, useContext, useEffect, useState, type ReactNode } from 'react';
import api, { refreshCsrfToken } from '../api/client';
import { fetchMe } from '../api/services';
import type { Role, User } from '../types';

interface AuthContextValue {
  user: User | null;
  loading: boolean;
  login: (email: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  hasRole: (...roles: Role[]) => boolean;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);

  // On page load: get the anti-forgery token, then check if the auth cookie is still valid
  useEffect(() => {
    refreshCsrfToken()
      .then(() => fetchMe())
      .then(setUser)
      .catch(() => setUser(null))
      .finally(() => setLoading(false));
  }, []);

  const login = async (email: string, password: string) => {
    const { data } = await api.post<{ user: User }>('/auth/login', { email, password });
    // the anti-forgery token is tied to the user, so get a new one after login
    await refreshCsrfToken();
    setUser(data.user);
  };

  const logout = async () => {
    try {
      await api.post('/auth/logout');
    } catch {
      // ignore - the session is cleared on the client anyway
    }
    setUser(null);
    await refreshCsrfToken().catch(() => undefined);
  };

  const hasRole = useCallback((...roles: Role[]) => !!user && roles.includes(user.role), [user]);

  return <AuthContext.Provider value={{ user, loading, login, logout, hasRole }}>{children}</AuthContext.Provider>;
}

// eslint-disable-next-line react-refresh/only-export-components
export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used inside AuthProvider');
  return ctx;
}
