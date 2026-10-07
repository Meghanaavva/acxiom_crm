import { useEffect, useState } from 'react';
import { BarElement, CategoryScale, Chart as ChartJS, Legend, LinearScale, Tooltip } from 'chart.js';
import { Bar } from 'react-chartjs-2';
import api, { downloadFile, getErrorMessage } from '../../api/client';
import Loader from '../../components/Loader';
import { useToast } from '../../context/ToastContext';
import { formatCurrency } from '../../utils/format';

ChartJS.register(BarElement, CategoryScale, LinearScale, Tooltip, Legend);

interface PipelineRow {
  count: number;
  amount: number;
  weightedAmount: number;
}

interface PipelineData {
  byStage: (PipelineRow & { stage: string })[];
  byOwner: (PipelineRow & { ownerId: number; ownerName: string })[];
  totals: PipelineRow;
}

export default function PipelineReport() {
  const { showToast } = useToast();
  const [data, setData] = useState<PipelineData | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api
      .get<PipelineData>('/reports/pipeline')
      .then(({ data }) => setData(data))
      .catch((err) => showToast(getErrorMessage(err), 'danger'))
      .finally(() => setLoading(false));
  }, [showToast]);

  const handleExport = async () => {
    try {
      await downloadFile('/reports/pipeline', {}, 'pipeline-report.csv');
    } catch (err) {
      showToast(getErrorMessage(err, 'Export failed.'), 'danger');
    }
  };

  if (loading) return <Loader />;
  if (!data) return null;

  return (
    <>
      <div className="d-flex justify-content-between align-items-center mb-3">
        <p className="text-muted small mb-0">Open opportunities only. Weighted amount = Amount x Probability / 100.</p>
        <button className="btn btn-sm btn-success" onClick={handleExport}>
          <i className="bi bi-file-earmark-spreadsheet me-1"></i>Export CSV
        </button>
      </div>

      <div className="row g-3 mb-3">
        <div className="col-md-4">
          <div className="border rounded p-3 text-center">
            <div className="small text-muted">Open Opportunities</div>
            <div className="fs-4 fw-bold">{data.totals.count}</div>
          </div>
        </div>
        <div className="col-md-4">
          <div className="border rounded p-3 text-center">
            <div className="small text-muted">Pipeline Amount</div>
            <div className="fs-4 fw-bold">{formatCurrency(data.totals.amount)}</div>
          </div>
        </div>
        <div className="col-md-4">
          <div className="border rounded p-3 text-center">
            <div className="small text-muted">Weighted Pipeline</div>
            <div className="fs-4 fw-bold text-success">{formatCurrency(data.totals.weightedAmount)}</div>
          </div>
        </div>
      </div>

      <div className="row g-3">
        <div className="col-lg-6">
          <h6>Stage-wise Pipeline</h6>
          <div className="table-responsive">
            <table className="table table-sm align-middle">
              <thead>
                <tr>
                  <th>Stage</th>
                  <th className="text-end">Count</th>
                  <th className="text-end">Amount</th>
                  <th className="text-end">Weighted</th>
                </tr>
              </thead>
              <tbody>
                {data.byStage.map((s) => (
                  <tr key={s.stage}>
                    <td>{s.stage}</td>
                    <td className="text-end">{s.count}</td>
                    <td className="text-end">{formatCurrency(s.amount)}</td>
                    <td className="text-end">{formatCurrency(s.weightedAmount)}</td>
                  </tr>
                ))}
              </tbody>
              <tfoot className="fw-bold">
                <tr>
                  <td>Total</td>
                  <td className="text-end">{data.totals.count}</td>
                  <td className="text-end">{formatCurrency(data.totals.amount)}</td>
                  <td className="text-end">{formatCurrency(data.totals.weightedAmount)}</td>
                </tr>
              </tfoot>
            </table>
          </div>
          <div style={{ height: 220 }}>
            <Bar
              data={{
                labels: data.byStage.map((s) => s.stage),
                datasets: [
                  { label: 'Amount', data: data.byStage.map((s) => s.amount), backgroundColor: 'rgba(13, 110, 253, 0.7)' },
                  { label: 'Weighted', data: data.byStage.map((s) => s.weightedAmount), backgroundColor: 'rgba(25, 135, 84, 0.7)' },
                ],
              }}
              options={{ maintainAspectRatio: false, scales: { y: { beginAtZero: true } } }}
            />
          </div>
        </div>
        <div className="col-lg-6">
          <h6>Owner-wise Pipeline</h6>
          <div className="table-responsive">
            <table className="table table-sm align-middle">
              <thead>
                <tr>
                  <th>Owner</th>
                  <th className="text-end">Count</th>
                  <th className="text-end">Amount</th>
                  <th className="text-end">Weighted</th>
                </tr>
              </thead>
              <tbody>
                {data.byOwner.length === 0 && (
                  <tr>
                    <td colSpan={4} className="text-center text-muted">
                      No open opportunities.
                    </td>
                  </tr>
                )}
                {data.byOwner.map((o) => (
                  <tr key={o.ownerId}>
                    <td>{o.ownerName}</td>
                    <td className="text-end">{o.count}</td>
                    <td className="text-end">{formatCurrency(o.amount)}</td>
                    <td className="text-end">{formatCurrency(o.weightedAmount)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </>
  );
}
