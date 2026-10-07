import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import api, { getErrorMessage } from '../../api/client';
import Loader from '../../components/Loader';
import PageHeader from '../../components/PageHeader';
import { useToast } from '../../context/ToastContext';
import type { Opportunity } from '../../types';
import { OPEN_STAGES } from '../../utils/constants';
import { formatCurrency, formatDate } from '../../utils/format';

interface PipelineStage {
  stage: string;
  count: number;
  amount: number;
  weightedAmount: number;
  items: Opportunity[];
}

const STAGE_COLORS: Record<string, string> = {
  Qualification: 'primary',
  Proposal: 'info',
  Negotiation: 'warning',
  Won: 'success',
  Lost: 'danger',
};

// next stage for the "move forward" button on open cards
function nextStage(stage: string): string | null {
  const index = OPEN_STAGES.indexOf(stage);
  if (index === -1) return null;
  return index < OPEN_STAGES.length - 1 ? OPEN_STAGES[index + 1] : 'Won';
}

export default function PipelineBoard() {
  const { showToast } = useToast();
  const [stages, setStages] = useState<PipelineStage[]>([]);
  const [loading, setLoading] = useState(true);
  const [movingId, setMovingId] = useState<number | null>(null);

  const loadPipeline = useCallback(async () => {
    try {
      const { data } = await api.get<{ stages: PipelineStage[] }>('/opportunities/pipeline');
      setStages(data.stages);
    } catch (err) {
      showToast(getErrorMessage(err), 'danger');
    } finally {
      setLoading(false);
    }
  }, [showToast]);

  useEffect(() => {
    loadPipeline();
  }, [loadPipeline]);

  const moveForward = async (opp: Opportunity) => {
    const target = nextStage(opp.stage);
    if (!target) return;
    if (target === 'Won' && !window.confirm(`Mark "${opp.opportunityName}" as Won?`)) return;
    setMovingId(opp.id);
    try {
      await api.patch(`/opportunities/${opp.id}/stage`, { stage: target });
      showToast(`Moved to ${target}.`);
      await loadPipeline();
    } catch (err) {
      showToast(getErrorMessage(err), 'danger');
    } finally {
      setMovingId(null);
    }
  };

  if (loading) return <Loader />;

  const openStages = stages.filter((s) => OPEN_STAGES.includes(s.stage));
  const totalOpen = openStages.reduce((sum, s) => sum + s.amount, 0);
  const totalWeighted = openStages.reduce((sum, s) => sum + s.weightedAmount, 0);

  return (
    <>
      <PageHeader
        title="Sales Pipeline"
        subtitle={`Open pipeline ${formatCurrency(totalOpen)} · Weighted ${formatCurrency(totalWeighted)}`}
        actions={
          <>
            <Link to="/opportunities" className="btn btn-outline-secondary">
              <i className="bi bi-list-ul me-1"></i>List View
            </Link>
            <Link to="/opportunities/new" className="btn btn-primary">
              <i className="bi bi-plus-lg me-1"></i>Add Opportunity
            </Link>
          </>
        }
      />

      <div className="d-flex gap-3 overflow-auto pb-3">
        {stages.map((column) => {
          const color = STAGE_COLORS[column.stage] || 'secondary';
          return (
            <div key={column.stage} className="pipeline-column flex-shrink-0" style={{ width: 270 }}>
              <div className={`card border-top border-3 border-${color} h-100`}>
                <div className="card-header bg-white">
                  <div className="d-flex justify-content-between align-items-center">
                    <span className="fw-semibold">{column.stage}</span>
                    <span className={`badge bg-${color}`}>{column.count}</span>
                  </div>
                  <div className="small text-muted">
                    {formatCurrency(column.amount)}
                    {OPEN_STAGES.includes(column.stage) && <> · Wtd {formatCurrency(column.weightedAmount)}</>}
                  </div>
                </div>
                <div className="card-body bg-light p-2" style={{ maxHeight: '65vh', overflowY: 'auto' }}>
                  {column.items.length === 0 && <div className="text-center text-muted small py-3">No opportunities</div>}
                  {column.items.map((opp) => {
                    const target = nextStage(opp.stage);
                    return (
                      <div key={opp.id} className="card mb-2">
                        <div className="card-body p-2">
                          <Link to={`/opportunities/${opp.id}`} className="fw-semibold text-decoration-none d-block small">
                            {opp.opportunityName}
                          </Link>
                          <div className="small text-muted">{opp.customerName}</div>
                          <div className="d-flex justify-content-between small mt-1">
                            <span>{formatCurrency(opp.amount)}</span>
                            <span className="text-muted">{opp.probability}%</span>
                          </div>
                          <div className="d-flex justify-content-between align-items-center small mt-1">
                            <span className="text-muted">
                              <i className="bi bi-calendar me-1"></i>
                              {formatDate(opp.expectedCloseDate)}
                            </span>
                            {target && (
                              <button
                                className="btn btn-sm btn-outline-primary py-0 px-1"
                                title={`Move to ${target}`}
                                aria-label={`Move ${opp.opportunityName} to ${target}`}
                                disabled={movingId === opp.id}
                                onClick={() => moveForward(opp)}
                              >
                                <i className="bi bi-arrow-right"></i>
                              </button>
                            )}
                          </div>
                          <div className="small text-muted mt-1">
                            <i className="bi bi-person me-1"></i>
                            {opp.assignedToName}
                          </div>
                        </div>
                      </div>
                    );
                  })}
                </div>
              </div>
            </div>
          );
        })}
      </div>
    </>
  );
}
