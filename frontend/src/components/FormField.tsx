import type { ReactNode } from 'react';

interface Props {
  label: string;
  name: string;
  error?: string;
  required?: boolean;
  children: ReactNode;
  className?: string;
  hint?: string;
}

// Wraps an input with label + inline error message (bootstrap invalid-feedback)
export default function FormField({ label, name, error, required, children, className = 'col-md-6', hint }: Props) {
  return (
    <div className={className}>
      <label htmlFor={name} className="form-label">
        {label} {required && <span className="text-danger">*</span>}
      </label>
      {children}
      {error ? <div className="invalid-feedback d-block">{error}</div> : hint && <div className="form-text">{hint}</div>}
    </div>
  );
}
