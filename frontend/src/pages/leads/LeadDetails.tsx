import { useCallback, useEffect, useState, type ChangeEvent, type ReactNode } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import api, { getErrorMessage, getFieldErrors } from '../../api/client';
import ConfirmDialog from '../../components/ConfirmDialog';
import FormField from '../../components/FormField';
import Loader from '../../components/Loader';
import Modal from '../../components/Modal';
import PageHeader from '../../components/PageHeader';
import StatusBadge from '../../components/StatusBadge';
import { useAuth } from '../../context/AuthContext';
import { useToast } from '../../context/ToastContext';
import type { Customer, Lead } from '../../types';
import { LEAD_STATUS_TRANSITIONS } from '../../utils/constants';
import { formatCurrency, formatDateTime } from '../../utils/format';
import {
  collectErrors,
  integer,
  maxLength,
  notPastDate,
  numberRange,
  required,
  todayString,
  validDate,
  type Errors,
} from '../../utils/validation';

interface ConvertResult {
  lead: Lead;
  customer: Customer;
  opportunity: { id: number } | null;
}

function InfoRow({ label, value }: { label: string; value: ReactNode }) {
  return (
    <div className="col-sm-6 col-lg-4">
      <div className="text-muted small">{label}</div>
      <div className="fw-semibold">{value || '-'}</div>
    </div>
  );
}

// button colour for each target status
const STATUS_BUTTON: Record<string, string> = {
  Contacted: 'btn-outline-info',
  Qualified: 'btn-outline-success',
  Unqualified: 'btn-outline-secondary',
  Lost: 'btn-outline-danger',
};

export default function LeadDetails() {
  const { id } = useParams();
  const navigate = useNavigate();
  const { hasRole } = useAuth();
  const { showToast } = useToast();
  const canDelete = hasRole('Admin', 'Manager');

  const [lead, setLead] = useState<Lead | null>(null);
  const [loading, setLoading] = useState(true);
  const [statusTarget, setStatusTarget] = useState('');
  const [changingStatus, setChangingStatus] = useState(false);
  const [showDelete, setShowDelete] = useState(false);
  const [deleting, setDeleting] = useState(false);

  // convert modal state
  const [showConvert, setShowConvert] = useState(false);
  const [convertForm, setConvertForm] = useState({
    createOpportunity: true,
    opportunityName: '',
    amount: '',
    probability: '20',
    expectedCloseDate: '',
  });
  const [convertErrors, setConvertErrors] = useState<Errors>({});
  const [convertError, setConvertError] = useState('');
  const [converting, setConverting] = useState(false);

  const loadLead = useCallback(async () => {
    try {
      const { data } = await api.get<Lead>(`/leads/${id}`);
      setLead(data);
    } catch (err) {
      showToast(getErrorMessage(err), 'danger');
      navigate('/leads');
    } finally {
      setLoading(false);
    }
  }, [id, navigate, showToast]);

  useEffect(() => {
    loadLead();
  }, [loadLead]);

  const handleStatusChange = async () => {
    setChangingStatus(true);
    try {
      const { data } = await api.patch<Lead>(`/leads/${id}/status`, { status: statusTarget });
      setLead(data);
      showToast(`Lead status changed to ${statusTarget}.`);
    } catch (err) {
      showToast(getErrorMessage(err), 'danger');
    } finally {
      setChangingStatus(false);
      setStatusTarget('');
    }
  };

  const handleDelete = async () => {
    setDeleting(true);
    try {
      await api.delete(`/leads/${id}`);
      showToast('Lead deleted successfully.');
      navigate('/leads');
    } catch (err) {
      showToast(getErrorMessage(err), 'danger');
      setShowDelete(false);
    } finally {
      setDeleting(false);
    }
  };

  const openConvert = () => {
    if (!lead) return;
    setConvertForm({
      createOpportunity: true,
      opportunityName: `${lead.companyName || lead.leadName} - Deal`,
      amount: lead.expectedValue > 0 ? String(lead.expectedValue) : '',
      probability: '20',
      expectedCloseDate: '',
    });
    setConvertErrors({});
    setConvertError('');
    setShowConvert(true);
  };

  const handleConvertChange = (e: ChangeEvent<HTMLInputElement>) => {
    const { name, value, type, checked } = e.target;
    setConvertForm({ ...convertForm, [name]: type === 'checkbox' ? checked : value });
    if (convertErrors[name]) setConvertErrors({ ...convertErrors, [name]: '' });
  };

  const handleConvert = async () => {
    setConvertError('');
    const f = convertForm;
    // opportunity rules are only checked when we are creating one
    const validationErrors = f.createOpportunity
      ? collectErrors({
          opportunityName: [required(f.opportunityName, 'Opportunity Name'), maxLength(f.opportunityName, 150, 'Opportunity Name')],
          amount: [
            required(f.amount, 'Amount'),
            f.amount !== '' && Number(f.amount) <= 0 ? 'Opportunity Amount must be greater than 0.' : '',
          ],
          probability: [
            required(f.probability, 'Probability'),
            numberRange(f.probability, 0, 100, 'Probability must be between 0 and 100.'),
            integer(f.probability, 'Probability'),
          ],
          expectedCloseDate: [
            required(f.expectedCloseDate, 'Expected Close Date'),
            validDate(f.expectedCloseDate, 'Expected Close Date'),
            notPastDate(f.expectedCloseDate, 'Expected Close Date cannot be in the past.'),
          ],
        })
      : {};
    setConvertErrors(validationErrors);
    if (Object.keys(validationErrors).length > 0) return;

    setConverting(true);
    const payload = f.createOpportunity
      ? {
          createOpportunity: true,
          opportunityName: f.opportunityName,
          amount: Number(f.amount),
          probability: Number(f.probability),
          expectedCloseDate: f.expectedCloseDate,
        }
      : { createOpportunity: false };
    try {
      const { data } = await api.post<ConvertResult>(`/leads/${id}/convert`, payload);
      showToast('Lead converted successfully.');
      setShowConvert(false);
      navigate(`/customers/${data.customer.id}`);
    } catch (err) {
      setConvertErrors(getFieldErrors(err));
      setConvertError(getErrorMessage(err));
    } finally {
      setConverting(false);
    }
  };

  if (loading || !lead) return <Loader />;

  const nextStatuses = LEAD_STATUS_TRANSITIONS[lead.status] || [];
  const isClosed = lead.status === 'Converted' || lead.status === 'Lost';
  const inputClass = (field: string) => `form-control ${convertErrors[field] ? 'is-invalid' : ''}`;

  return (
    <>
      <PageHeader
        title={lead.leadName}
        subtitle={`${lead.leadCode} ${lead.companyName ? '· ' + lead.companyName : ''}`}
        actions={
          <>
            <Link to="/leads" className="btn btn-outline-secondary">
              <i className="bi bi-arrow-left me-1"></i>Back
            </Link>
            {!isClosed && (
              <Link to={`/leads/${lead.id}/edit`} className="btn btn-primary">
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

      <div className="row g-4">
        <div className="col-lg-8">
          <div className="card h-100">
            <div className="card-header bg-white fw-semibold">Lead Information</div>
            <div className="card-body">
              <div className="row g-3">
                <InfoRow label="Status" value={<StatusBadge status={lead.status} />} />
                <InfoRow label="Priority" value={<StatusBadge status={lead.priority} />} />
                <InfoRow label="Source" value={lead.source} />
                <InfoRow label="Email" value={lead.email} />
                <InfoRow label="Phone" value={lead.phone} />
                <InfoRow label="Expected Value" value={formatCurrency(lead.expectedValue)} />
                <InfoRow label="Assigned To" value={lead.assignedToName} />
                <InfoRow label="Created On" value={formatDateTime(lead.createdAt)} />
                <InfoRow label="Last Updated" value={formatDateTime(lead.updatedAt)} />
                {lead.notes && (
                  <div className="col-12">
                    <div className="text-muted small">Notes</div>
                    <div style={{ whiteSpace: 'pre-wrap' }}>{lead.notes}</div>
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>

        <div className="col-lg-4">
          <div className="card h-100">
            <div className="card-header bg-white fw-semibold">Workflow</div>
            <div className="card-body">
              {lead.status === 'Converted' && (
                <div className="alert alert-success small">
                  Converted on {formatDateTime(lead.convertedAt)}
                  {lead.convertedCustomerId && (
                    <div className="mt-1">
                      Customer:{' '}
                      <Link to={`/customers/${lead.convertedCustomerId}`}>{lead.convertedCustomerName || 'View customer'}</Link>
                    </div>
                  )}
                </div>
              )}
              {lead.status === 'Lost' && <div className="alert alert-danger small">This lead is marked as Lost.</div>}

              {nextStatuses.length > 0 && (
                <>
                  <div className="small text-muted mb-2">Move lead to:</div>
                  <div className="d-flex flex-wrap gap-2 mb-3">
                    {nextStatuses.map((s) => (
                      <button key={s} className={`btn btn-sm ${STATUS_BUTTON[s] || 'btn-outline-primary'}`} onClick={() => setStatusTarget(s)}>
                        {s}
                      </button>
                    ))}
                  </div>
                </>
              )}

              {lead.status === 'Qualified' && (
                <button className="btn btn-success w-100" onClick={openConvert}>
                  <i className="bi bi-arrow-repeat me-1"></i>Convert to Customer
                </button>
              )}
              {!isClosed && lead.status !== 'Qualified' && (
                <p className="small text-muted mb-0">Only Qualified leads can be converted to a customer.</p>
              )}

              {!isClosed && (
                <Link to={`/followups/new?leadId=${lead.id}`} className="btn btn-outline-primary btn-sm w-100 mt-3">
                  <i className="bi bi-calendar-plus me-1"></i>Schedule Follow-Up
                </Link>
              )}
            </div>
          </div>
        </div>
      </div>

      <ConfirmDialog
        show={!!statusTarget}
        title="Change Lead Status"
        message={
          <>
            Change status from <strong>{lead.status}</strong> to <strong>{statusTarget}</strong>?
          </>
        }
        confirmText="Change Status"
        confirmVariant={statusTarget === 'Lost' ? 'danger' : 'primary'}
        busy={changingStatus}
        onConfirm={handleStatusChange}
        onCancel={() => setStatusTarget('')}
      />

      <ConfirmDialog
        show={showDelete}
        title="Delete Lead"
        message={
          <>
            Are you sure you want to delete lead <strong>{lead.leadName}</strong>?
          </>
        }
        busy={deleting}
        onConfirm={handleDelete}
        onCancel={() => setShowDelete(false)}
      />

      <Modal
        show={showConvert}
        title="Convert Lead to Customer"
        onClose={() => setShowConvert(false)}
        footer={
          <>
            <button className="btn btn-outline-secondary" onClick={() => setShowConvert(false)} disabled={converting}>
              Cancel
            </button>
            <button className="btn btn-success" onClick={handleConvert} disabled={converting}>
              {converting && <span className="spinner-border spinner-border-sm me-1"></span>}
              Convert
            </button>
          </>
        }
      >
        {convertError && <div className="alert alert-danger py-2 small">{convertError}</div>}
        <p className="small text-muted">
          A customer will be created from this lead. If a customer with the same email or phone already exists, it will be linked instead.
        </p>
        <div className="form-check mb-3">
          <input
            id="createOpportunity"
            name="createOpportunity"
            type="checkbox"
            className="form-check-input"
            checked={convertForm.createOpportunity}
            onChange={handleConvertChange}
          />
          <label htmlFor="createOpportunity" className="form-check-label">
            Also create an opportunity
          </label>
        </div>

        {convertForm.createOpportunity && (
          <div className="row g-3">
            <FormField label="Opportunity Name" name="opportunityName" error={convertErrors.opportunityName} required className="col-12">
              <input
                id="opportunityName"
                name="opportunityName"
                className={inputClass('opportunityName')}
                maxLength={150}
                value={convertForm.opportunityName}
                onChange={handleConvertChange}
              />
            </FormField>
            <FormField label="Amount (₹)" name="amount" error={convertErrors.amount} required>
              <input
                id="amount"
                name="amount"
                type="number"
                min={0}
                step="0.01"
                className={inputClass('amount')}
                value={convertForm.amount}
                onChange={handleConvertChange}
              />
            </FormField>
            <FormField label="Probability (%)" name="probability" error={convertErrors.probability} required>
              <input
                id="probability"
                name="probability"
                type="number"
                min={0}
                max={100}
                className={inputClass('probability')}
                value={convertForm.probability}
                onChange={handleConvertChange}
              />
            </FormField>
            <FormField label="Expected Close Date" name="expectedCloseDate" error={convertErrors.expectedCloseDate} required className="col-12">
              <input
                id="expectedCloseDate"
                name="expectedCloseDate"
                type="date"
                min={todayString()}
                className={inputClass('expectedCloseDate')}
                value={convertForm.expectedCloseDate}
                onChange={handleConvertChange}
              />
            </FormField>
          </div>
        )}
      </Modal>
    </>
  );
}
