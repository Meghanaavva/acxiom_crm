import { createContext, useCallback, useContext, useState, type ReactNode } from 'react';

type ToastType = 'success' | 'danger' | 'warning' | 'info';

interface Toast {
  id: number;
  type: ToastType;
  message: string;
}

interface ToastContextValue {
  showToast: (message: string, type?: ToastType) => void;
}

const ToastContext = createContext<ToastContextValue | undefined>(undefined);

const ICONS: Record<ToastType, string> = {
  success: 'bi-check-circle-fill',
  danger: 'bi-x-circle-fill',
  warning: 'bi-exclamation-triangle-fill',
  info: 'bi-info-circle-fill',
};

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<Toast[]>([]);

  const showToast = useCallback((message: string, type: ToastType = 'success') => {
    const id = Date.now() + Math.random();
    setToasts((prev) => [...prev, { id, type, message }]);
    setTimeout(() => setToasts((prev) => prev.filter((t) => t.id !== id)), 4000);
  }, []);

  return (
    <ToastContext.Provider value={{ showToast }}>
      {children}
      <div className="toast-container position-fixed top-0 end-0 p-3" style={{ zIndex: 2000 }}>
        {toasts.map((t) => (
          <div key={t.id} className={`toast show align-items-center text-bg-${t.type} border-0`} role="alert" aria-live="assertive">
            <div className="d-flex">
              <div className="toast-body">
                <i className={`bi ${ICONS[t.type]} me-2`}></i>
                {t.message}
              </div>
              <button
                type="button"
                className="btn-close btn-close-white me-2 m-auto"
                aria-label="Close"
                onClick={() => setToasts((prev) => prev.filter((x) => x.id !== t.id))}
              ></button>
            </div>
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  );
}

// eslint-disable-next-line react-refresh/only-export-components
export function useToast() {
  const ctx = useContext(ToastContext);
  if (!ctx) throw new Error('useToast must be used inside ToastProvider');
  return ctx;
}
