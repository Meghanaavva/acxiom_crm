import type { ReactNode } from 'react';

export default function PageHeader({ title, subtitle, actions }: { title: string; subtitle?: string; actions?: ReactNode }) {
  return (
    <div className="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-4">
      <div>
        <h1 className="h4 mb-0">{title}</h1>
        {subtitle && <p className="text-muted small mb-0">{subtitle}</p>}
      </div>
      {actions && <div className="d-flex gap-2">{actions}</div>}
    </div>
  );
}
