import type { Pagination as PaginationInfo } from '../types';
import { PAGE_SIZES } from '../utils/constants';

interface Props {
  pagination: PaginationInfo;
  onPageChange: (page: number) => void;
  onPageSizeChange?: (size: number) => void;
}

export default function Pagination({ pagination, onPageChange, onPageSizeChange }: Props) {
  const { page, pageSize, total, totalPages } = pagination;
  const from = total === 0 ? 0 : (page - 1) * pageSize + 1;
  const to = Math.min(page * pageSize, total);

  // show max 5 page numbers around the current page
  const start = Math.max(1, Math.min(page - 2, totalPages - 4));
  const pages: number[] = [];
  for (let p = start; p <= Math.min(totalPages, start + 4); p++) pages.push(p);

  return (
    <div className="d-flex flex-wrap justify-content-between align-items-center gap-2 mt-3">
      <div className="d-flex align-items-center gap-2 small text-muted">
        <span>
          Showing {from}-{to} of {total}
        </span>
        {onPageSizeChange && (
          <select
            className="form-select form-select-sm w-auto"
            value={pageSize}
            aria-label="Rows per page"
            onChange={(e) => onPageSizeChange(Number(e.target.value))}
          >
            {PAGE_SIZES.map((s) => (
              <option key={s} value={s}>
                {s} / page
              </option>
            ))}
          </select>
        )}
      </div>
      <nav aria-label="Pagination">
        <ul className="pagination pagination-sm mb-0">
          <li className={`page-item ${page <= 1 ? 'disabled' : ''}`}>
            <button className="page-link" onClick={() => onPageChange(page - 1)} aria-label="Previous">
              &laquo;
            </button>
          </li>
          {pages.map((p) => (
            <li key={p} className={`page-item ${p === page ? 'active' : ''}`}>
              <button className="page-link" onClick={() => onPageChange(p)}>
                {p}
              </button>
            </li>
          ))}
          <li className={`page-item ${page >= totalPages ? 'disabled' : ''}`}>
            <button className="page-link" onClick={() => onPageChange(page + 1)} aria-label="Next">
              &raquo;
            </button>
          </li>
        </ul>
      </nav>
    </div>
  );
}
