import type { ReactNode } from 'react';

interface Props {
  show: boolean;
  title: string;
  onClose: () => void;
  children: ReactNode;
  footer?: ReactNode;
  size?: 'sm' | 'lg' | 'xl';
}

// Generic modal for small forms (convert lead, reschedule follow-up etc.)
export default function Modal({ show, title, onClose, children, footer, size }: Props) {
  if (!show) return null;
  return (
    <>
      <div className="modal fade show d-block" tabIndex={-1} role="dialog" aria-modal="true">
        <div className={`modal-dialog modal-dialog-centered ${size ? `modal-${size}` : ''}`}>
          <div className="modal-content">
            <div className="modal-header">
              <h5 className="modal-title">{title}</h5>
              <button type="button" className="btn-close" aria-label="Close" onClick={onClose}></button>
            </div>
            <div className="modal-body">{children}</div>
            {footer && <div className="modal-footer">{footer}</div>}
          </div>
        </div>
      </div>
      <div className="modal-backdrop fade show"></div>
    </>
  );
}
