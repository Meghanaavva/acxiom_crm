import { useEffect, useState, type ChangeEvent, type FormEvent } from 'react';
import { Link, useNavigate, useParams, useSearchParams } from 'react-router-dom';
import api, { getErrorMessage, getFieldErrors } from '../../api/client';
import { fetchUserLookup } from '../../api/services';
import FormField from '../../components/FormField';
import Loader from '../../components/Loader';
import PageHeader from '../../components/PageHeader';
import { useAuth } from '../../context/AuthContext';
import { useToast } from '../../context/ToastContext';
import type { Customer, Opportunity, PagedResult, UserLookup } from '../../types';
import { LEAD_SOURCES, OPEN_STAGES, OPPORTUNITY_STAGES } from '../../utils/constants';
import { formatCurrency } from '../../utils/format';
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

const emptyForm = {
  opportunityName: '',
  customerId: '',
  amount: '',
  stage: 'Qualification',
  probability: '20',
  expectedCloseDate: '',
  source: '',
  notes: '',
  assignedTo: '',
};

type FormState = typeof emptyForm;

function validateForm(form: FormState): Errors {
  const isActive = OPEN_STAGES.includes(form.stage);
  return collectErrors({
    opportunityName: [required(form.opportunityName, 'Opportunity Name'), maxLength(form.opportunityName, 150, 'Opportunity Name')],
    customerId: [required(form.customerId, 'Customer')],
    amount: [
      required(form.amount, 'Amount'),
      form.amount !== '' && Number(form.amount) < 0 ? 'Opportunity Amount cannot be negative.' : '',
      // business rule: active opportunities need a real amount
      isActive && form.amount !== '' && Number(form.amount) <= 0 ? 'Opportunity Amount must be greater than 0.' : '',
    ],
    stage: [required(form.stage, 'Stage')],
    probability: [
      required(form.probability, 'Probability'),
      numberRange(form.probability, 0, 100, 'Probability must be between 0 and 100.'),
      integer(form.probability, 'Probability'),
    ],
    expectedCloseDate: [
      required(form.expectedCloseDate, 'Expected Close Date'),
      validDate(form.expectedCloseDate, 'Expected Close Date'),
      isActive ? notPastDate(form.expectedCloseDate, 'Expected Close Date cannot be in the past.') : '',
    ],
    notes: [maxLength(form.notes, 1000, 'Notes')],
  });
}

export default function OpportunityForm() {
  const { id } = useParams();
  const isEdit = !!id;
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const { user } = useAuth();
  const { showToast } = useToast();

  const [form, setForm] = useState<FormState>({
    ...emptyForm,
    customerId: searchParams.get('customerId') || '',
    assignedTo: String(user?.id ?? ''),
  });
  const [isClosed, setIsClosed] = useState(false);
  const [leadId, setLeadId] = useState<number | null>(null);
  const [errors, setErrors] = useState<Errors>({});
  const [users, setUsers] = useState<UserLookup[]>([]);
  const [customers, setCustomers] = useState<Customer[]>([]);
  // customer of the record being edited (may not be in the first 100 of the dropdown)
  const [currentCustomer, setCurrentCustomer] = useState<{ id: number; name: string } | null>(null);
  const [loading, setLoading] = useState(isEdit);
  const [saving, setSaving] = useState(false);
  const [serverError, setServerError] = useState('');

  useEffect(() => {
    fetchUserLookup().then(setUsers).catch(() => setUsers([]));
    api
      .get<PagedResult<Customer>>('/customers', { params: { pageSize: 100, sortBy: 'customerName', sortOrder: 'asc' } })
      .then(({ data }) => setCustomers(data.data))
      .catch(() => setCustomers([]));
  }, []);

  useEffect(() => {
    if (!isEdit) return;
    api
      .get<Opportunity>(`/opportunities/${id}`)
      .then(({ data }) => {
        setIsClosed(data.status !== 'Open');
        setLeadId(data.leadId);
        setForm({
          opportunityName: data.opportunityName,
          customerId: String(data.customerId),
          amount: String(data.amount),
          stage: data.stage,
          probability: String(data.probability),
          expectedCloseDate: data.expectedCloseDate,
          source: data.source || '',
          notes: data.notes || '',
          assignedTo: String(data.assignedTo),
        });
        setCurrentCustomer({ id: data.customerId, name: data.customerName || `Customer #${data.customerId}` });
      })
      .catch((err) => {
        showToast(getErrorMessage(err), 'danger');
        navigate('/opportunities');
      })
      .finally(() => setLoading(false));
  }, [id, isEdit, navigate, showToast]);

  const handleChange = (e: ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>) => {
    const { name, value } = e.target;
    const next = { ...form, [name]: value };
    // Won / Lost set the probability automatically (same as the server)
    if (name === 'stage' && value === 'Won') next.probability = '100';
    if (name === 'stage' && value === 'Lost') next.probability = '0';
    setForm(next);
    if (errors[name]) setErrors({ ...errors, [name]: '' });
  };

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setServerError('');
    const validationErrors = validateForm(form);
    setErrors(validationErrors);
    if (Object.keys(validationErrors).length > 0) return;

    setSaving(true);
    const payload = {
      ...form,
      customerId: Number(form.customerId),
      leadId,
      amount: Number(form.amount),
      probability: Number(form.probability),
      source: form.source || null,
      assignedTo: form.assignedTo ? Number(form.assignedTo) : null,
    };
    try {
      if (isEdit) {
        await api.put(`/opportunities/${id}`, payload);
        showToast('Opportunity updated successfully.');
        navigate(`/opportunities/${id}`);
      } else {
        const { data } = await api.post<Opportunity>('/opportunities', payload);
        showToast('Opportunity created successfully.');
        navigate(`/opportunities/${data.id}`);
      }
    } catch (err) {
      setErrors(getFieldErrors(err));
      setServerError(getErrorMessage(err));
    } finally {
      setSaving(false);
    }
  };

  if (loading) return <Loader />;

  const isActive = OPEN_STAGES.includes(form.stage);
  const weighted = (Number(form.amount) || 0) * (Number(form.probability) || 0) / 100;
  const inputClass = (field: string) => `form-control ${errors[field] ? 'is-invalid' : ''}`;
  const selectClass = (field: string) => `form-select ${errors[field] ? 'is-invalid' : ''}`;

  return (
    <>
      <PageHeader title={isEdit ? 'Edit Opportunity' : 'Add Opportunity'} subtitle="Fields marked * are mandatory" />

      <div className="card">
        <div className="card-body">
          {isClosed && <div className="alert alert-warning">This opportunity is closed (Won/Lost) and can no longer be edited.</div>}
          {serverError && <div className="alert alert-danger">{serverError}</div>}

          <form onSubmit={handleSubmit} noValidate>
            <fieldset disabled={isClosed}>
              <div className="row g-3">
                <FormField label="Opportunity Name" name="opportunityName" error={errors.opportunityName} required>
                  <input
                    id="opportunityName"
                    name="opportunityName"
                    className={inputClass('opportunityName')}
                    maxLength={150}
                    value={form.opportunityName}
                    onChange={handleChange}
                  />
                </FormField>
                <FormField label="Customer" name="customerId" error={errors.customerId} required>
                  <select id="customerId" name="customerId" className={selectClass('customerId')} value={form.customerId} onChange={handleChange}>
                    <option value="">-- Select customer --</option>
                    {currentCustomer && !customers.some((c) => c.id === currentCustomer.id) && (
                      <option value={currentCustomer.id}>{currentCustomer.name}</option>
                    )}
                    {customers.map((c) => (
                      <option key={c.id} value={c.id}>
                        {c.customerName}
                        {c.companyName ? ` (${c.companyName})` : ''}
                      </option>
                    ))}
                  </select>
                </FormField>
                <FormField label="Amount (₹)" name="amount" error={errors.amount} required className="col-md-4">
                  <input
                    id="amount"
                    name="amount"
                    type="number"
                    min={0}
                    step="0.01"
                    className={inputClass('amount')}
                    value={form.amount}
                    onChange={handleChange}
                  />
                </FormField>
                <FormField label="Stage" name="stage" error={errors.stage} required className="col-md-4">
                  <select id="stage" name="stage" className={selectClass('stage')} value={form.stage} onChange={handleChange}>
                    {OPPORTUNITY_STAGES.map((s) => (
                      <option key={s}>{s}</option>
                    ))}
                  </select>
                </FormField>
                <FormField label="Probability (%)" name="probability" error={errors.probability} required className="col-md-4">
                  <input
                    id="probability"
                    name="probability"
                    type="number"
                    min={0}
                    max={100}
                    className={inputClass('probability')}
                    value={form.probability}
                    onChange={handleChange}
                  />
                </FormField>
                <FormField label="Expected Close Date" name="expectedCloseDate" error={errors.expectedCloseDate} required className="col-md-4">
                  <input
                    id="expectedCloseDate"
                    name="expectedCloseDate"
                    type="date"
                    min={isActive ? todayString() : undefined}
                    className={inputClass('expectedCloseDate')}
                    value={form.expectedCloseDate}
                    onChange={handleChange}
                  />
                </FormField>
                <FormField label="Source" name="source" error={errors.source} className="col-md-4">
                  <select id="source" name="source" className={selectClass('source')} value={form.source} onChange={handleChange}>
                    <option value="">-- Select --</option>
                    {LEAD_SOURCES.map((s) => (
                      <option key={s}>{s}</option>
                    ))}
                  </select>
                </FormField>
                <FormField label="Owner" name="assignedTo" error={errors.assignedTo} className="col-md-4">
                  <select
                    id="assignedTo"
                    name="assignedTo"
                    className={selectClass('assignedTo')}
                    value={form.assignedTo}
                    onChange={handleChange}
                    disabled={users.length <= 1}
                  >
                    {users.map((u) => (
                      <option key={u.id} value={u.id}>
                        {u.name}
                      </option>
                    ))}
                  </select>
                </FormField>
                <div className="col-12">
                  <div className="alert alert-light border py-2 mb-0 small">
                    <i className="bi bi-calculator me-1"></i>
                    Weighted amount (Amount × Probability / 100): <strong>{formatCurrency(weighted)}</strong>
                  </div>
                </div>
                <FormField label="Notes" name="notes" error={errors.notes} className="col-12">
                  <textarea id="notes" name="notes" rows={3} className={inputClass('notes')} maxLength={1000} value={form.notes} onChange={handleChange} />
                </FormField>
              </div>
            </fieldset>

            <div className="d-flex gap-2 mt-4">
              {!isClosed && (
                <button type="submit" className="btn btn-primary" disabled={saving}>
                  {saving && <span className="spinner-border spinner-border-sm me-1"></span>}
                  {isEdit ? 'Update Opportunity' : 'Save Opportunity'}
                </button>
              )}
              <Link to={isEdit ? `/opportunities/${id}` : '/opportunities'} className="btn btn-outline-secondary">
                Cancel
              </Link>
            </div>
          </form>
        </div>
      </div>
    </>
  );
}
