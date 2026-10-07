import { useCallback, useEffect, useState, type ReactNode } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import api, { getErrorMessage } from '../../api/client';
import ConfirmDialog from '../../components/ConfirmDialog';
import Loader from '../../components/Loader';
import PageHeader from '../../components/PageHeader';
import StatusBadge from '../../components/StatusBadge';
import { useAuth } from '../../context/AuthContext';
import { useToast } from '../../context/ToastContext';
import type { Opportunity } from '../../types';
import { OPPORTUNITY_STAGES } from '../../utils/constants';
import { formatCurrency, formatDate, formatDateTime } from '../../utils/format';

function InfoRow({ label, value }: { label: string; value: ReactNode }) {
  return (
    <div className="col-sm-6 col-lg-4">
      <div className="text-muted small">{label}</div>
      <div className="fw-semibold">{value || '-'}</div>
    </div>
  );
}

export default function OpportunityDetails() {
  const { id } = useParams();
  const navigate = useNavigate();
  const { hasRole } = useAuth();
  const { showToast } = useToast();
  const canDelete = hasRole('Admin', 'Manager');

  const [opp, setOpp] = useState<Opportunity | null>(null);
  const [loading, setLoading] = useState(true);
  const [stageTarget, setStageTarget] = useState('');
  const [changing, setChanging] = useState(false);
  const [showDelete, setShowDelete] = useState(false);
  const [deleting, setDeleting] = useState(false);

  const loadOpportunity = useCallback(async () => {
    try {
      const { data } = await api.get<Opportunity>(`/opportunities/${id}`);
      setOpp(data);
    } catch (err) {
      showToast(getErrorMessage(err), 'danger');
      navigate('/opportunities');
    } finally {
      setLoading(false);
    }
  }, [id, navigate, showToast]);

  useEffect(() => {
    loadOpportunity();
  }, [loadOpportunity]);

  const changeStage = async (stage: string) => {
    setChanging(true);
    try {
      const { data } = await api.patch<Opportunity>(`/opportunities/${id}/stage`, { stage });
      setOpp(data);
      showToast(`Opportunity moved to ${stage}.`);
    } catch (err) {
      showToast(getErrorMessage(err), 'danger');
    } finally {
      setChanging(false);
      setStageTarget('');
    }
  };

  // Won / Lost are final so ask for confirmation first
  const handleStageClick = (stage: string) => {
    if (stage === 'Won' || stage === 'Lost') {
      setStageTarget(stage);
    } else {
      changeStage(stage);
    }
  };

  const handleDelete = async () => {
    setDeleting(true);
    try {
      await api.delete(`/opportunities/${id}`);
      showToast('Opportunity deleted successfully.');
      navigate('/opportunities');
    } catch (err) {
      showToast(getErrorMessage(err), 'danger');
      setShowDelete(false);
    } finally {
      setDeleting(false);
    }
  };

  if (loading || !opp) return <Loader />;

  const isOpen = opp.status === 'Open';
  const currentIndex = OPPORTUNITY_STAGES.indexOf(opp.stage);

  return (
    <>
      <PageHeader
        title={opp.opportunityName}
        subtitle={opp.customerName || ''}
        actions={
          <>
            <Link to="/opportunities" className="btn btn-outline-secondary">
              <i className="bi bi-arrow-left me-1"></i>Back
            </Link>
            {isOpen && (
              <Link to={`/opportunities/${opp.id}/edit`} className="btn btn-primary">
                <i className="bi bi-pencil me-1"></i>Edit
              </Link>
            )}
            {canDelete && (
              <button className="btn btn-outline-danger" onClick={() => setShowDelete(true)}>
                <i className="bi bi-trash me-1"></i>Delete
              </button>
            )}
          </>
        }
      />

      {/* Stage progress */}
      <div className="card mb-4">
        <div className="card-body">
          <div className="d-flex flex-wrap gap-2 align-items-center">
            {OPPORTUNITY_STAGES.map((stage, index) => {
              const isCurrent = stage === opp.stage;
              const isPassed = isOpen && index < currentIndex;
              let btnClass = 'btn-outline-secondary';
              if (isCurrent) btnClass = stage === 'Lost' ? 'btn-danger' : stage === 'Won' ? 'btn-success' : 'btn-primary';
              else if (isPassed) btnClass = 'btn-outline-primary';
              return (
                <button
                  key={stage}
                  className={`btn btn-sm ${btnClass}`}
                  disabled={!isOpen || isCurrent || changing}
                  onClick={() => handleStageClick(stage)}
                  title={isOpen && !isCurrent ? `Move to ${stage}` : stage}
                >
                  {isPassed && <i className="bi bi-check2 me-1"></i>}
                  {stage}
                </button>
              );
            })}
          </div>
          {!isOpen && (
            <div className="small text-muted mt-2">
              This opportunity was closed as <strong>{opp.status}</strong> on {formatDateTime(opp.closedAt)}.
            </div>
          )}
        </div>
      </div>

      <div className="card">
        <div className="card-header bg-white fw-semibold">Opportunity Information</div>
        <div className="card-body">
          <div className="row g-3">
            <InfoRow label="Customer" value={<Link to={`/customers/${opp.customerId}`}>{opp.customerName}</Link>} />
            <InfoRow label="Stage" value={<StatusBadge status={opp.stage} />} />
            <InfoRow label="Status" value={<StatusBadge status={opp.status} />} />
            <InfoRow label="Amount" value={formatCurrency(opp.amount)} />
            <InfoRow label="Probability" value={`${opp.probability}%`} />
            <InfoRow label="Weighted Amount" value={formatCurrency(opp.weightedAmount)} />
            <InfoRow label="Expected Close Date" value={formatDate(opp.expectedCloseDate)} />
            <InfoRow label="Source" value={opp.source} />
            <InfoRow label="Owner" value={opp.assignedToName} />
            <InfoRow label="Lead" value={opp.leadId ? <Link to={`/leads/${opp.leadId}`}>{opp.leadName || 'View lead'}</Link> : null} />
            <InfoRow label="Created On" value={formatDateTime(opp.createdAt)} />
            <InfoRow label="Last Updated" value={formatDateTime(opp.updatedAt)} />
            {opp.notes && (
              <div className="col-12">
                <div className="text-muted small">Notes</div>
                <div style={{ whiteSpace: 'pre-wrap' }}>{opp.notes}</div>
              </div>
            )}
          </div>
          {isOpen && (
            <Link to={`/followups/new?opportunityId=${opp.id}&customerId=${opp.customerId}`} className="btn btn-sm btn-outline-primary mt-3">
              <i className="bi bi-calendar-plus me-1"></i>Schedule Follow-Up
            </Link>
          )}
        </div>
      </div>

      <ConfirmDialog
        show={!!stageTarget}
        title={`Mark as ${stageTarget}`}
        message={
          <>
            Are you sure you want to mark <strong>{opp.opportunityName}</strong> as <strong>{stageTarget}</strong>? Closed opportunities
            cannot be edited later.
          </>
        }
        confirmText={`Mark ${stageTarget}`}
        confirmVariant={stageTarget === 'Won' ? 'success' : 'danger'}
        busy={changing}
        onConfirm={() => changeStage(stageTarget)}
        onCancel={() => setStageTarget('')}
      />

      <ConfirmDialog
        show={showDelete}
        title="Delete Opportunity"
        message={
          <>
            Are you sure you want to delete <strong>{opp.opportunityName}</strong>?
          </>
        }
        busy={deleting}
        onConfirm={handleDelete}
        onCancel={() => setShowDelete(false)}
      />
    </>
  );
}
