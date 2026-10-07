import { useCallback, useEffect, useState, type ReactNode } from 'react';
import api, { cleanParams, downloadFile, getErrorMessage } from '../../api/client';
import Loader from '../../components/Loader';
import Pagination from '../../components/Pagination';
import SortableHeader from '../../components/SortableHeader';
import { useToast } from '../../context/ToastContext';
import type { PagedResult } from '../../types';

type Row = Record<string, unknown>;

export interface ReportColumn {
  key: string;
  label: string;
  sortable?: boolean;
  className?: string;
  render?: (row: Row) => ReactNode;
}

export interface ReportFilter {
  name: string;
  label: string;
  options: { value: string; label: string }[];
}

interface Props {
  endpoint: string;
  fileName: string;
  columns: ReportColumn[];
  filters?: ReportFilter[];
  defaultSort?: string;
  showDateRange?: boolean;
  dateLabel?: string;
}

// Generic paged report: filters + date range + sortable table + CSV export.
// Used by every tabular tab on the Reports page.
export default function ReportTable({
  endpoint,
  fileName,
  columns,
  filters = [],
  defaultSort = 'createdAt',
  showDateRange = true,
  dateLabel = 'Created',
}: Props) {
  const { showToast } = useToast();
  const [result, setResult] = useState<PagedResult<Row> | null>(null);
  const [loading, setLoading] = useState(true);
  const [exporting, setExporting] = useState(false);

  const [filterValues, setFilterValues] = useState<Record<string, string>>({ dateFrom: '', dateTo: '' });
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [sortBy, setSortBy] = useState(defaultSort);
  const [sortOrder, setSortOrder] = useState<'asc' | 'desc'>('desc');
  const [dateError, setDateError] = useState('');

  const loadReport = useCallback(async () => {
    if (filterValues.dateFrom && filterValues.dateTo && filterValues.dateFrom > filterValues.dateTo) {
      setDateError('From date cannot be after To date.');
      return;
    }
    setDateError('');
    setLoading(true);
    try {
      const { data } = await api.get<PagedResult<Row> | Row[]>(endpoint, {
        params: cleanParams({ ...filterValues, page, pageSize, sortBy, sortOrder }),
      });
      // some reports may return a plain array - wrap it so the table works the same way
      if (Array.isArray(data)) {
        setResult({ data, pagination: { page: 1, pageSize: data.length || 1, total: data.length, totalPages: 1 } });
      } else {
        setResult(data);
      }
    } catch (err) {
      showToast(getErrorMessage(err), 'danger');
    } finally {
      setLoading(false);
    }
  }, [endpoint, filterValues, page, pageSize, sortBy, sortOrder, showToast]);

  useEffect(() => {
    loadReport();
  }, [loadReport]);

  const handleSort = (field: string) => {
    if (sortBy === field) {
      setSortOrder(sortOrder === 'asc' ? 'desc' : 'asc');
    } else {
      setSortBy(field);
      setSortOrder('asc');
    }
  };

  const handleFilterChange = (name: string, value: string) => {
    setFilterValues({ ...filterValues, [name]: value });
    setPage(1);
  };

  const handleExport = async () => {
    setExporting(true);
    try {
      await downloadFile(endpoint, cleanParams({ ...filterValues, sortBy, sortOrder }), fileName);
    } catch (err) {
      showToast(getErrorMessage(err, 'Export failed.'), 'danger');
    } finally {
      setExporting(false);
    }
  };

  return (
    <>
      <div className="row g-2 mb-3 align-items-end">
        {filters.map((f) => (
          <div className="col-6 col-md-2" key={f.name}>
            <label htmlFor={`rf-${f.name}`} className="form-label small mb-1">
              {f.label}
            </label>
            <select
              id={`rf-${f.name}`}
              className="form-select form-select-sm"
              value={filterValues[f.name] || ''}
              onChange={(e) => handleFilterChange(f.name, e.target.value)}
            >
              <option value="">All</option>
              {f.options.map((o) => (
                <option key={o.value} value={o.value}>
                  {o.label}
                </option>
              ))}
            </select>
          </div>
        ))}
        {showDateRange && (
          <>
            <div className="col-6 col-md-2">
              <label htmlFor="rf-dateFrom" className="form-label small mb-1">
                {dateLabel} From
              </label>
              <input id="rf-dateFrom" type="date" className="form-control form-control-sm" value={filterValues.dateFrom} onChange={(e) => handleFilterChange('dateFrom', e.target.value)} />
            </div>
            <div className="col-6 col-md-2">
              <label htmlFor="rf-dateTo" className="form-label small mb-1">
                {dateLabel} To
              </label>
              <input id="rf-dateTo" type="date" className="form-control form-control-sm" value={filterValues.dateTo} onChange={(e) => handleFilterChange('dateTo', e.target.value)} />
            </div>
          </>
        )}
        <div className="col-md ms-auto text-md-end">
          <button className="btn btn-sm btn-success" onClick={handleExport} disabled={exporting}>
            {exporting ? <span className="spinner-border spinner-border-sm me-1"></span> : <i className="bi bi-file-earmark-spreadsheet me-1"></i>}
            Export CSV
          </button>
        </div>
      </div>
      {dateError && <div className="text-danger small mb-2">{dateError}</div>}

      {loading && !result ? (
        <Loader />
      ) : (
        <div className="table-responsive">
          <table className="table table-hover table-sm align-middle mb-0">
            <thead>
              <tr>
                {columns.map((c) =>
                  c.sortable ? (
                    <SortableHeader key={c.key} label={c.label} field={c.key} sortBy={sortBy} sortOrder={sortOrder} onSort={handleSort} />
                  ) : (
                    <th key={c.key} className={c.className}>
                      {c.label}
                    </th>
                  ),
                )}
              </tr>
            </thead>
            <tbody>
              {result?.data.length === 0 && (
                <tr>
                  <td colSpan={columns.length} className="text-center text-muted py-4">
                    No records found for the selected filters.
                  </td>
                </tr>
              )}
              {result?.data.map((row, index) => (
                <tr key={String(row.id ?? row.userId ?? index)}>
                  {columns.map((c) => (
                    <td key={c.key} className={c.className}>
                      {c.render ? c.render(row) : String(row[c.key] ?? '-')}
                    </td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {result && (
        <Pagination
          pagination={result.pagination}
          onPageChange={setPage}
          onPageSizeChange={(size) => {
            setPageSize(size);
            setPage(1);
          }}
        />
      )}
    </>
  );
}
