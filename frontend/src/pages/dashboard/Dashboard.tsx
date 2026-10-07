import { useCallback, useEffect, useState } from 'react';
import {
  ArcElement,
  BarElement,
  CategoryScale,
  Chart as ChartJS,
  Legend,
  LinearScale,
  LineElement,
  PointElement,
  Tooltip,
} from 'chart.js';
import { Bar, Doughnut, Line } from 'react-chartjs-2';
import { Link } from 'react-router-dom';
import api, { cleanParams, getErrorMessage } from '../../api/client';
import Loader from '../../components/Loader';
import StatusBadge from '../../components/StatusBadge';
import { useAuth } from '../../context/AuthContext';
import { useToast } from '../../context/ToastContext';
import type { Activity, FollowUp } from '../../types';
import { formatCurrency, formatDate, formatDateTime, roleLabel } from '../../utils/format';

ChartJS.register(ArcElement, BarElement, CategoryScale, LinearScale, LineElement, PointElement, Tooltip, Legend);

interface DashboardData {
  range: { from: string | null; to: string | null };
  kpis: {
    totalCustomers: number;
    totalLeads: number;
    openLeads: number;
    totalOpportunities: number;
    openOpportunities: number;
    wonOpportunities: number;
    lostOpportunities: number;
    totalPipelineValue: number;
    weightedPipelineValue: number;
    wonAmount: number;
    pendingFollowUps: number;
    overdueFollowUps: number;
  };
  charts: {
    leadStatus: { labels: string[]; data: number[] };
    opportunityPipeline: { labels: string[]; counts: number[]; amounts: number[] };
    monthlySales: { labels: string[]; won: number[]; lost: number[] };
  };
  upcomingFollowUps: FollowUp[];
  recentActivities: Activity[];
  adminStats: { totalUsers: number; activeUsers: number; lockedUsers: number; failedLoginsToday: number } | null;
  teamPerformance: { userId: number; userName: string; openOpportunities: number; pipelineValue: number; wonAmount: number }[] | null;
}

const RANGES = [
  { value: 'all', label: 'All Time' },
  { value: 'today', label: 'Today' },
  { value: 'week', label: 'This Week' },
  { value: 'month', label: 'This Month' },
  { value: 'custom', label: 'Custom' },
];

const LEAD_COLORS: Record<string, string> = {
  New: '#0d6efd',
  Contacted: '#0dcaf0',
  Qualified: '#198754',
  Unqualified: '#adb5bd',
  Lost: '#dc3545',
  Converted: '#212529',
};

const SUBTITLES: Record<string, string> = {
  Admin: 'Overview of the whole organisation, users and security.',
  Manager: 'Overview of your team pipeline, leads and opportunities.',
  SalesExecutive: 'Overview of your assigned customers, leads and follow-ups.',
};

const ACTIVITY_ICONS: Record<string, string> = {
  Call: 'bi-telephone',
  Meeting: 'bi-people',
  Email: 'bi-envelope',
  Task: 'bi-check2-square',
};

function KpiCard({ label, value, icon, color }: { label: string; value: string | number; icon: string; color: string }) {
  return (
    <div className="col-6 col-md-4 col-xl-3">
      <div className="card kpi-card h-100">
        <div className="card-body d-flex align-items-center">
          <div className={`kpi-icon bg-${color} bg-opacity-10 text-${color} me-3`}>
            <i className={`bi ${icon}`}></i>
          </div>
          <div>
            <div className="text-muted small">{label}</div>
            <div className="fs-5 fw-bold">{value}</div>
          </div>
        </div>
      </div>
    </div>
  );
}

export default function Dashboard() {
  const { user, hasRole } = useAuth();
  const { showToast } = useToast();

  const [data, setData] = useState<DashboardData | null>(null);
  const [loading, setLoading] = useState(true);
  const [range, setRange] = useState('all');
  const [custom, setCustom] = useState({ from: '', to: '' });
  const [customError, setCustomError] = useState('');

  const loadDashboard = useCallback(
    async (selectedRange: string, from = '', to = '') => {
      setLoading(true);
      try {
        const { data } = await api.get<DashboardData>('/dashboard', {
          params: cleanParams({ range: selectedRange, from, to }),
        });
        setData(data);
      } catch (err) {
        showToast(getErrorMessage(err), 'danger');
      } finally {
        setLoading(false);
      }
    },
    [showToast],
  );

  useEffect(() => {
    if (range !== 'custom') loadDashboard(range);
  }, [range, loadDashboard]);

  const applyCustomRange = () => {
    if (!custom.from || !custom.to) {
      setCustomError('Please select both From and To dates.');
      return;
    }
    if (custom.from > custom.to) {
      setCustomError('From date cannot be after To date.');
      return;
    }
    setCustomError('');
    loadDashboard('custom', custom.from, custom.to);
  };

  const kpis = data?.kpis;

  return (
    <>
      <div className="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-4">
        <div>
          <h1 className="h4 mb-0">Welcome, {user?.name}</h1>
          <p className="text-muted small mb-0">
            {roleLabel(user?.role || '')} dashboard &middot; {SUBTITLES[user?.role || ''] || ''}
          </p>
        </div>
        <div className="btn-group flex-wrap" role="group" aria-label="Date range">
          {RANGES.map((r) => (
            <button
              key={r.value}
              type="button"
              className={`btn btn-sm ${range === r.value ? 'btn-primary' : 'btn-outline-primary'}`}
              onClick={() => setRange(r.value)}
            >
              {r.label}
            </button>
          ))}
        </div>
      </div>

      {range === 'custom' && (
        <div className="card mb-4">
          <div className="card-body">
            <div className="row g-2 align-items-end">
              <div className="col-sm-4 col-md-3">
                <label htmlFor="dashFrom" className="form-label small">From</label>
                <input id="dashFrom" type="date" className="form-control form-control-sm" value={custom.from} onChange={(e) => setCustom({ ...custom, from: e.target.value })} />
              </div>
              <div className="col-sm-4 col-md-3">
                <label htmlFor="dashTo" className="form-label small">To</label>
                <input id="dashTo" type="date" className="form-control form-control-sm" value={custom.to} onChange={(e) => setCustom({ ...custom, to: e.target.value })} />
              </div>
              <div className="col-sm-4 col-md-3">
                <button className="btn btn-sm btn-primary" onClick={applyCustomRange}>
                  Apply
                </button>
              </div>
            </div>
            {customError && <div className="text-danger small mt-2">{customError}</div>}
          </div>
        </div>
      )}

      {loading && !data ? (
        <Loader />
      ) : (
        kpis &&
        data && (
          <>
            <div className="row g-3 mb-4">
              <KpiCard label="Total Customers" value={kpis.totalCustomers} icon="bi-people" color="primary" />
              <KpiCard label="Total Leads" value={kpis.totalLeads} icon="bi-person-plus" color="info" />
              <KpiCard label="Open Leads" value={kpis.openLeads} icon="bi-hourglass-split" color="warning" />
              <KpiCard label="Total Opportunities" value={kpis.totalOpportunities} icon="bi-briefcase" color="secondary" />
              <KpiCard label="Open Opportunities" value={kpis.openOpportunities} icon="bi-briefcase-fill" color="primary" />
              <KpiCard label="Won Opportunities" value={kpis.wonOpportunities} icon="bi-trophy" color="success" />
              <KpiCard label="Lost Opportunities" value={kpis.lostOpportunities} icon="bi-x-octagon" color="danger" />
              <KpiCard label="Total Pipeline Value" value={formatCurrency(kpis.totalPipelineValue)} icon="bi-currency-rupee" color="success" />
              <KpiCard label="Weighted Pipeline" value={formatCurrency(kpis.weightedPipelineValue)} icon="bi-graph-up-arrow" color="info" />
              <KpiCard label="Won Amount" value={formatCurrency(kpis.wonAmount)} icon="bi-cash-stack" color="success" />
              <KpiCard label="Pending Follow-Ups" value={kpis.pendingFollowUps} icon="bi-calendar-check" color="warning" />
              <KpiCard label="Overdue Follow-Ups" value={kpis.overdueFollowUps} icon="bi-calendar-x" color="danger" />
            </div>

            <div className="row g-3 mb-4">
              <div className="col-lg-4">
                <div className="card h-100">
                  <div className="card-header bg-white fw-semibold">Lead Status</div>
                  <div className="card-body d-flex justify-content-center" style={{ height: 280 }}>
                    <Doughnut
                      data={{
                        labels: data.charts.leadStatus.labels,
                        datasets: [
                          {
                            data: data.charts.leadStatus.data,
                            backgroundColor: data.charts.leadStatus.labels.map((l) => LEAD_COLORS[l] || '#6c757d'),
                          },
                        ],
                      }}
                      options={{ maintainAspectRatio: false, plugins: { legend: { position: 'bottom' } } }}
                    />
                  </div>
                </div>
              </div>
              <div className="col-lg-8">
                <div className="card h-100">
                  <div className="card-header bg-white fw-semibold">Opportunity Pipeline</div>
                  <div className="card-body" style={{ height: 280 }}>
                    <Bar
                      data={{
                        labels: data.charts.opportunityPipeline.labels,
                        datasets: [
                          {
                            label: 'Count',
                            data: data.charts.opportunityPipeline.counts,
                            backgroundColor: 'rgba(13, 110, 253, 0.7)',
                            yAxisID: 'y',
                          },
                          {
                            label: 'Amount (INR)',
                            data: data.charts.opportunityPipeline.amounts,
                            backgroundColor: 'rgba(25, 135, 84, 0.6)',
                            yAxisID: 'y1',
                          },
                        ],
                      }}
                      options={{
                        maintainAspectRatio: false,
                        scales: {
                          y: { beginAtZero: true, position: 'left', title: { display: true, text: 'Count' }, ticks: { precision: 0 } },
                          y1: { beginAtZero: true, position: 'right', grid: { drawOnChartArea: false }, title: { display: true, text: 'Amount' } },
                        },
                      }}
                    />
                  </div>
                </div>
              </div>
            </div>

            <div className="card mb-4">
              <div className="card-header bg-white fw-semibold">Monthly Sales (last 6 months)</div>
              <div className="card-body" style={{ height: 300 }}>
                <Line
                  data={{
                    labels: data.charts.monthlySales.labels,
                    datasets: [
                      {
                        label: 'Won Amount',
                        data: data.charts.monthlySales.won,
                        borderColor: '#198754',
                        backgroundColor: 'rgba(25, 135, 84, 0.2)',
                        tension: 0.3,
                      },
                      {
                        label: 'Lost Amount',
                        data: data.charts.monthlySales.lost,
                        borderColor: '#dc3545',
                        backgroundColor: 'rgba(220, 53, 69, 0.2)',
                        tension: 0.3,
                      },
                    ],
                  }}
                  options={{ maintainAspectRatio: false, scales: { y: { beginAtZero: true } } }}
                />
              </div>
            </div>

            <div className="row g-3 mb-4">
              <div className="col-lg-6">
                <div className="card h-100">
                  <div className="card-header bg-white d-flex justify-content-between">
                    <span className="fw-semibold">Upcoming Follow-Ups</span>
                    <Link to="/followups" className="small">View all</Link>
                  </div>
                  <ul className="list-group list-group-flush">
                    {data.upcomingFollowUps.length === 0 && <li className="list-group-item text-muted">No upcoming follow-ups.</li>}
                    {data.upcomingFollowUps.map((f) => (
                      <li key={f.id} className="list-group-item d-flex justify-content-between align-items-center">
                        <div>
                          <div className="fw-semibold">{f.subject}</div>
                          <div className="small text-muted">
                            {f.followUpType} &middot; {f.customerName || f.leadName || f.opportunityName || '-'} &middot; {f.assignedToName}
                          </div>
                        </div>
                        <span className={`badge ${f.isOverdue ? 'bg-danger' : 'bg-light text-dark border'}`}>{formatDate(f.followUpDate)}</span>
                      </li>
                    ))}
                  </ul>
                </div>
              </div>
              <div className="col-lg-6">
                <div className="card h-100">
                  <div className="card-header bg-white d-flex justify-content-between">
                    <span className="fw-semibold">Recent Activities</span>
                    <Link to="/activities" className="small">View all</Link>
                  </div>
                  <ul className="list-group list-group-flush">
                    {data.recentActivities.length === 0 && <li className="list-group-item text-muted">No activities yet.</li>}
                    {data.recentActivities.map((a) => (
                      <li key={a.id} className="list-group-item d-flex justify-content-between align-items-center">
                        <div>
                          <i className={`bi ${ACTIVITY_ICONS[a.activityType] || 'bi-dot'} me-2 text-primary`}></i>
                          <span className="fw-semibold">{a.subject}</span>
                          <div className="small text-muted">
                            {formatDateTime(a.activityDate)} &middot; {a.assignedToName}
                          </div>
                        </div>
                        <StatusBadge status={a.status} />
                      </li>
                    ))}
                  </ul>
                </div>
              </div>
            </div>

            <div className="row g-3">
              {hasRole('Admin') && data.adminStats && (
                <div className="col-lg-4">
                  <div className="card h-100">
                    <div className="card-header bg-white fw-semibold">
                      <i className="bi bi-shield-check me-2"></i>Users &amp; Security
                    </div>
                    <ul className="list-group list-group-flush">
                      <li className="list-group-item d-flex justify-content-between">Total Users <strong>{data.adminStats.totalUsers}</strong></li>
                      <li className="list-group-item d-flex justify-content-between">Active Users <strong>{data.adminStats.activeUsers}</strong></li>
                      <li className="list-group-item d-flex justify-content-between">
                        Locked Accounts <strong className={data.adminStats.lockedUsers ? 'text-danger' : ''}>{data.adminStats.lockedUsers}</strong>
                      </li>
                      <li className="list-group-item d-flex justify-content-between">
                        Failed Logins Today <strong className={data.adminStats.failedLoginsToday ? 'text-danger' : ''}>{data.adminStats.failedLoginsToday}</strong>
                      </li>
                    </ul>
                  </div>
                </div>
              )}
              {hasRole('Admin', 'Manager') && data.teamPerformance && (
                <div className={hasRole('Admin') ? 'col-lg-8' : 'col-12'}>
                  <div className="card h-100">
                    <div className="card-header bg-white fw-semibold">
                      <i className="bi bi-people me-2"></i>Team Performance
                    </div>
                    <div className="table-responsive">
                      <table className="table table-sm align-middle mb-0">
                        <thead>
                          <tr>
                            <th>Sales Person</th>
                            <th className="text-end">Open Opportunities</th>
                            <th className="text-end">Pipeline Value</th>
                            <th className="text-end">Won Amount</th>
                          </tr>
                        </thead>
                        <tbody>
                          {data.teamPerformance.length === 0 && (
                            <tr>
                              <td colSpan={4} className="text-center text-muted py-3">No data.</td>
                            </tr>
                          )}
                          {data.teamPerformance.map((t) => (
                            <tr key={t.userId}>
                              <td>{t.userName}</td>
                              <td className="text-end">{t.openOpportunities}</td>
                              <td className="text-end">{formatCurrency(t.pipelineValue)}</td>
                              <td className="text-end">{formatCurrency(t.wonAmount)}</td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  </div>
                </div>
              )}
            </div>
          </>
        )
      )}
    </>
  );
}
