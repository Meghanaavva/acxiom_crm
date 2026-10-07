import { useEffect, useState } from 'react';
import api, { downloadFile, getErrorMessage } from '../../api/client';
import Loader from '../../components/Loader';
import { useToast } from '../../context/ToastContext';
import { formatCurrency } from '../../utils/format';

interface ConversionData {
  totalLeads: number;
  convertedLeads: number;
  lostLeads: number;
  conversionRate: number;
  totalOpportunities: number;
  wonOpportunities: number;
  lostOpportunities: number;
  winRate: number;
  wonAmount: number;
  bySource: { source: string; total: number; converted: number; conversionRate: number }[];
}

function StatBox({ label, value, className = '' }: { label: string; value: string | number; className?: string }) {
  return (
    <div className="col-6 col-md-4 col-xl-3">
      <div className="border rounded p-3 text-center h-100">
        <div className="small text-muted">{label}</div>
        <div className={`fs-4 fw-bold ${className}`}>{value}</div>
      </div>
    </div>
  );
}

export default function ConversionReport() {
  const { showToast } = useToast();
  const [data, setData] = useState<ConversionData | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api
      .get<ConversionData>('/reports/conversion')
      .then(({ data }) => setData(data))
      .catch((err) => showToast(getErrorMessage(err), 'danger'))
      .finally(() => setLoading(false));
  }, [showToast]);

  const handleExport = async () => {
    try {
      await downloadFile('/reports/conversion', {}, 'conversion-report.csv');
    } catch (err) {
      showToast(getErrorMessage(err, 'Export failed.'), 'danger');
    }
  };

  if (loading) return <Loader />;
  if (!data) return null;

  return (
    <>
      <div className="d-flex justify-content-between align-items-center mb-3">
        <p className="text-muted small mb-0">Lead conversion and opportunity outcomes.</p>
        <button className="btn btn-sm btn-success" onClick={handleExport}>
          <i className="bi bi-file-earmark-spreadsheet me-1"></i>Export CSV
        </button>
      </div>

      <div className="row g-3 mb-4">
        <StatBox label="Total Leads" value={data.totalLeads} />
        <StatBox label="Converted Leads" value={data.convertedLeads} className="text-success" />
        <StatBox label="Lost Leads" value={data.lostLeads} className="text-danger" />
        <StatBox label="Lead Conversion Rate" value={`${data.conversionRate}%`} className="text-primary" />
        <StatBox label="Total Opportunities" value={data.totalOpportunities} />
        <StatBox label="Won Opportunities" value={data.wonOpportunities} className="text-success" />
        <StatBox label="Lost Opportunities" value={data.lostOpportunities} className="text-danger" />
        <StatBox label="Win Rate" value={`${data.winRate}%`} className="text-primary" />
        <StatBox label="Won Amount" value={formatCurrency(data.wonAmount)} className="text-success" />
      </div>

      <h6>Conversion by Lead Source</h6>
      <div className="table-responsive">
        <table className="table table-sm align-middle">
          <thead>
            <tr>
              <th>Source</th>
              <th className="text-end">Total Leads</th>
              <th className="text-end">Converted</th>
              <th style={{ width: '35%' }}>Conversion Rate</th>
            </tr>
          </thead>
          <tbody>
            {data.bySource.length === 0 && (
              <tr>
                <td colSpan={4} className="text-center text-muted">
                  No leads yet.
                </td>
              </tr>
            )}
            {data.bySource.map((s) => (
              <tr key={s.source}>
                <td>{s.source}</td>
                <td className="text-end">{s.total}</td>
                <td className="text-end">{s.converted}</td>
                <td>
                  <div className="d-flex align-items-center gap-2">
                    <div className="progress flex-grow-1" style={{ height: 8 }} role="progressbar" aria-label={`${s.source} conversion rate`} aria-valuenow={s.conversionRate} aria-valuemin={0} aria-valuemax={100}>
                      <div className="progress-bar bg-success" style={{ width: `${s.conversionRate}%` }}></div>
                    </div>
                    <span className="small">{s.conversionRate}%</span>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </>
  );
}
