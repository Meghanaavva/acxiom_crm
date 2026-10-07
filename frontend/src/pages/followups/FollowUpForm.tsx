import { useEffect, useState, type ChangeEvent, type FormEvent } from 'react';
import { Link, useNavigate, useParams, useSearchParams } from 'react-router-dom';
import api, { getErrorMessage, getFieldErrors } from '../../api/client';
import { fetchUserLookup } from '../../api/services';
import FormField from '../../components/FormField';
import Loader from '../../components/Loader';
import PageHeader from '../../components/PageHeader';
import { useAuth } from '../../context/AuthContext';
import { useToast } from '../../context/ToastContext';
import type { Customer, FollowUp, Lead, Opportunity, PagedResult, UserLookup } from '../../types';
import { FOLLOWUP_TYPES } from '../../utils/constants';
import { collectErrors, maxLength, notPastDate, required, todayString, validDate, type Errors } from '../../utils/validation';

const emptyForm = {
  subject: '',
  followUpType: 'Call',
  followUpDate: '',
  remarks: '',
  customerId: '',
  leadId: '',
  opportunityId: '',
  assignedTo: '',
};

type FormState = typeof emptyForm;

function validateForm(form: FormState): Errors {
  const errors = collectErrors({
    subject: [required(form.subject, 'Subject'), maxLength(form.subject, 150, 'Subject')],
    followUpType: [required(form.followUpType, 'Follow-up type')],
    followUpDate: [
      required(form.followUpDate, 'Follow-up date'),
      validDate(form.followUpDate, 'Follow-up date'),
      notPastDate(form.followUpDate, 'Follow-up date cannot be earlier than today.'),
    ],
    remarks: [maxLength(form.remarks, 1000, 'Remarks')],
  });
  // must be linked to at least one CRM record
  if (!form.customerId && !form.leadId && !form.opportunityId) {
    errors.customerId = 'Select a related customer, lead or opportunity.';
  }
  return errors;
}

export default function FollowUpForm() {
  const { id } = useParams();
  const isEdit = !!id;
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const { user } = useAuth();
  const { showToast } = useToast();

  const [form, setForm] = useState<FormState>({
    ...emptyForm,
    assignedTo: String(user?.id ?? ''),
    customerId: searchParams.get('customerId') || '',
    leadId: searchParams.get('leadId') || '',
    opportunityId: searchParams.get('opportunityId') || '',
  });
  const [errors, setErrors] = useState<Errors>({});
  const [users, setUsers] = useState<UserLookup[]>([]);
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [leads, setLeads] = useState<Lead[]>([]);
  const [opportunities, setOpportunities] = useState<Opportunity[]>([]);
  const [loading, setLoading] = useState(isEdit);
  const [saving, setSaving] = useState(false);
  const [serverError, setServerError] = useState('');

  // dropdown data
  useEffect(() => {
    fetchUserLookup().then(setUsers).catch(() => setUsers([]));
    const params = { pageSize: 100, sortBy: 'createdAt', sortOrder: 'desc' };
    api.get<PagedResult<Customer>>('/customers', { params }).then(({ data }) => setCustomers(data.data)).catch(() => setCustomers([]));
    api.get<PagedResult<Lead>>('/leads', { params }).then(({ data }) => setLeads(data.data)).catch(() => setLeads([]));
    api
      .get<PagedResult<Opportunity>>('/opportunities', { params: { ...params, status: 'Open' } })
      .then(({ data }) => setOpportunities(data.data))
      .catch(() => setOpportunities([]));
  }, []);

  useEffect(() => {
    if (!isEdit) return;
    api
      .get<FollowUp>(`/followups/${id}`)
      .then(({ data }) => {
        if (data.status !== 'Planned') {
          showToast('Only planned follow-ups can be edited.', 'warning');
          navigate('/followups');
          return;
        }
        setForm({
          subject: data.subject,
          followUpType: data.followUpType,
          followUpDate: data.followUpDate,
          remarks: data.remarks || '',
          customerId: data.customerId ? String(data.customerId) : '',
          leadId: data.leadId ? String(data.leadId) : '',
          opportunityId: data.opportunityId ? String(data.opportunityId) : '',
          assignedTo: String(data.assignedTo),
        });
      })
      .catch((err) => {
        showToast(getErrorMessage(err), 'danger');
        navigate('/followups');
      })
      .finally(() => setLoading(false));
  }, [id, isEdit, navigate, showToast]);

  const handleChange = (e: ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>) => {
    const { name, value } = e.target;
    setForm({ ...form, [name]: value });
    if (errors[name]) setErrors({ ...errors, [name]: '' });
    // the "related" error is shown under customer, clear it when any related field is chosen
    if (['leadId', 'opportunityId'].includes(name) && errors.customerId) setErrors({ ...errors, customerId: '' });
  };

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setServerError('');
    const validationErrors = validateForm(form);
    setErrors(validationErrors);
    if (Object.keys(validationErrors).length > 0) return;

    setSaving(true);
    const toId = (v: string) => (v ? Number(v) : null);
    const payload = {
      ...form,
      customerId: toId(form.customerId),
      leadId: toId(form.leadId),
      opportunityId: toId(form.opportunityId),
      assignedTo: toId(form.assignedTo),
    };
    try {
      if (isEdit) {
        await api.put(`/followups/${id}`, payload);
        showToast('Follow-up updated successfully.');
      } else {
        await api.post('/followups', payload);
        showToast('Follow-up scheduled successfully.');
      }
      navigate('/followups');
    } catch (err) {
      setErrors(getFieldErrors(err));
      setServerError(getErrorMessage(err));
    } finally {
      setSaving(false);
    }
  };

  if (loading) return <Loader />;

  const inputClass = (field: string) => `form-control ${errors[field] ? 'is-invalid' : ''}`;
  const selectClass = (field: string) => `form-select ${errors[field] ? 'is-invalid' : ''}`;

  return (
    <>
      <PageHeader title={isEdit ? 'Edit Follow-Up' : 'Schedule Follow-Up'} subtitle="Fields marked * are mandatory" />

      <div className="card">
        <div className="card-body">
          {serverError && <div className="alert alert-danger">{serverError}</div>}

          <form onSubmit={handleSubmit} noValidate>
            <div className="row g-3">
              <FormField label="Subject" name="subject" error={errors.subject} required>
                <input id="subject" name="subject" className={inputClass('subject')} maxLength={150} value={form.subject} onChange={handleChange} />
              </FormField>
              <FormField label="Type" name="followUpType" error={errors.followUpType} required className="col-md-3">
                <select id="followUpType" name="followUpType" className={selectClass('followUpType')} value={form.followUpType} onChange={handleChange}>
                  {FOLLOWUP_TYPES.map((t) => (
                    <option key={t}>{t}</option>
                  ))}
                </select>
              </FormField>
              <FormField label="Follow-Up Date" name="followUpDate" error={errors.followUpDate} required className="col-md-3">
                <input id="followUpDate" name="followUpDate" type="date" min={todayString()} className={inputClass('followUpDate')} value={form.followUpDate} onChange={handleChange} />
              </FormField>

              <FormField label="Customer" name="customerId" error={errors.customerId} className="col-md-4" hint="Select at least one related record">
                <select id="customerId" name="customerId" className={selectClass('customerId')} value={form.customerId} onChange={handleChange}>
                  <option value="">-- None --</option>
                  {customers.map((c) => (
                    <option key={c.id} value={c.id}>
                      {c.customerName} ({c.customerCode})
                    </option>
                  ))}
                </select>
              </FormField>
              <FormField label="Lead" name="leadId" error={errors.leadId} className="col-md-4">
                <select id="leadId" name="leadId" className={selectClass('leadId')} value={form.leadId} onChange={handleChange}>
                  <option value="">-- None --</option>
                  {leads.map((l) => (
                    <option key={l.id} value={l.id}>
                      {l.leadName} ({l.leadCode})
                    </option>
                  ))}
                </select>
              </FormField>
              <FormField label="Opportunity" name="opportunityId" error={errors.opportunityId} className="col-md-4">
                <select id="opportunityId" name="opportunityId" className={selectClass('opportunityId')} value={form.opportunityId} onChange={handleChange}>
                  <option value="">-- None --</option>
                  {opportunities.map((o) => (
                    <option key={o.id} value={o.id}>
                      {o.opportunityName}
                    </option>
                  ))}
                </select>
              </FormField>

              <FormField label="Assigned To" name="assignedTo" error={errors.assignedTo}>
                <select id="assignedTo" name="assignedTo" className={selectClass('assignedTo')} value={form.assignedTo} onChange={handleChange} disabled={users.length <= 1}>
                  {users.map((u) => (
                    <option key={u.id} value={u.id}>
                      {u.name}
                    </option>
                  ))}
                </select>
              </FormField>
              <FormField label="Remarks" name="remarks" error={errors.remarks} className="col-12">
                <textarea id="remarks" name="remarks" rows={3} className={inputClass('remarks')} maxLength={1000} value={form.remarks} onChange={handleChange} />
              </FormField>
            </div>

            <div className="d-flex gap-2 mt-4">
              <button type="submit" className="btn btn-primary" disabled={saving}>
                {saving && <span className="spinner-border spinner-border-sm me-1"></span>}
                {isEdit ? 'Update Follow-Up' : 'Save Follow-Up'}
              </button>
              <Link to="/followups" className="btn btn-outline-secondary">
                Cancel
              </Link>
            </div>
          </form>
        </div>
      </div>
    </>
  );
}
