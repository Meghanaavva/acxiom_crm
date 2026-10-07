import type { ReactNode } from 'react';

interface Props {
  show: boolean;
  title?: string;
  message: ReactNode;
  confirmText?: string;
  confirmVariant?: string;
  busy?: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}

// Bootstrap modal controlled by React state (no bootstrap JS needed)
export default function ConfirmDialog({
  show,
  title = 'Please confirm',
  message,
  confirmText = 'Delete',
  confirmVariant = 'danger',
  busy = false,
  onConfirm,
  onCancel,
}: Props) {
  if (!show) return null;
  return (
    <>
      <div className="modal fade show d-block" tabIndex={-1} role="dialog" aria-modal="true">
        <div className="modal-dialog modal-dialog-centered">
          <div className="modal-content">
            <div className="modal-header">
              <h5 className="modal-title">{title}</h5>
              <button type="button" className="btn-close" aria-label="Close" onClick={onCancel}></button>
            </div>
            <div className="modal-body">{message}</div>
            <div className="modal-footer">
              <button type="button" className="btn btn-outline-secondary" onClick={onCancel} disabled={busy}>
                Cancel
              </button>
              <button type="button" className={`btn btn-${confirmVariant}`} onClick={onConfirm} disabled={busy}>
                {busy && <span className="spinner-border spinner-border-sm me-1"></span>}
                {confirmText}
              </button>
            </div>
          </div>
        </div>
      </div>
      <div className="modal-backdrop fade show"></div>
    </>
  );
}
