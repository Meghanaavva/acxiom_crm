import { useEffect, useState, type ChangeEvent, type FormEvent } from 'react';
import { Link, useNavigate, useParams, useSearchParams } from 'react-router-dom';
import api, { getErrorMessage, getFieldErrors } from '../../api/client';
import { fetchUserLookup } from '../../api/services';
import FormField from '../../components/FormField';
import Loader from '../../components/Loader';
import PageHeader from '../../components/PageHeader';
import { useAuth } from '../../context/AuthContext';
import { useToast } from '../../context/ToastContext';
import type { Activity, Customer, Lead, PagedResult, UserLookup } from '../../types';
import { ACTIVITY_STATUSES, ACTIVITY_TYPES } from '../../utils/constants';
import { toDateTimeLocal } from '../../utils/format';
import { collectErrors, maxLength, required, validDate, type Errors } from '../../utils/validation';

const emptyForm = {
  activityType: 'Call',
  subject: '',
  description: '',
  activityDate: '',
  status: 'Planned',
  customerId: '',
  leadId: '',
  assignedTo: '',
};

type FormState = typeof emptyForm;

function validateForm(form: FormState): Errors {
  return collectErrors({
    activityType: [required(form.activityType, 'Activity type')],
    subject: [required(form.subject, 'Subject'), maxLength(form.subject, 150, 'Subject')],
    description: [maxLength(form.description, 1000, 'Description')],
    activityDate: [required(form.activityDate, 'Activity date'), validDate(form.activityDate, 'Activity date')],
    status: [required(form.status, 'Status')],
  });
}

export default function ActivityForm() {
  const { id } = useParams();
  const isEdit = !!id;
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const { user } = useAuth();
  const { showToast } = useToast();

  // lazy initial state so "now" is only calculated once
  const [form, setForm] = useState<FormState>(() => ({
    ...emptyForm,
    activityDate: toDateTimeLocal(new Date()),
    assignedTo: String(user?.id ?? ''),
    customerId: searchParams.get('customerId') || '',
    leadId: searchParams.get('leadId') || '',
  }));
  const [errors, setErrors] = useState<Errors>({});
  const [users, setUsers] = useState<UserLookup[]>([]);
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [leads, setLeads] = useState<Lead[]>([]);
  const [loading, setLoading] = useState(isEdit);
  const [saving, setSaving] = useState(false);
  const [serverError, setServerError] = useState('');

  useEffect(() => {
    fetchUserLookup().then(setUsers).catch(() => setUsers([]));
    const params = { pageSize: 100, sortBy: 'createdAt', sortOrder: 'desc' };
    api.get<PagedResult<Customer>>('/customers', { params }).then(({ data }) => setCustomers(data.data)).catch(() => setCustomers([]));
    api.get<PagedResult<Lead>>('/leads', { params }).then(({ data }) => setLeads(data.data)).catch(() => setLeads([]));
  }, []);

  useEffect(() => {
    if (!isEdit) return;
    api
      .get<Activity>(`/activities/${id}`)
      .then(({ data }) => {
        setForm({
          activityType: data.activityType,
          subject: data.subject,
          description: data.description || '',
          activityDate: toDateTimeLocal(data.activityDate),
          status: data.status,
          customerId: data.customerId ? String(data.customerId) : '',
          leadId: data.leadId ? String(data.leadId) : '',
          assignedTo: String(data.assignedTo),
        });
      })
      .catch((err) => {
        showToast(getErrorMessage(err), 'danger');
        navigate('/activities');
      })
      .finally(() => setLoading(false));
  }, [id, isEdit, navigate, showToast]);

  const handleChange = (e: ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>) => {
    const { name, value } = e.target;
    setForm({ ...form, [name]: value });
    if (errors[name]) setErrors({ ...errors, [name]: '' });
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
      // datetime-local has no timezone, convert to ISO so the server stores the right time
      activityDate: new Date(form.activityDate).toISOString(),
      customerId: toId(form.customerId),
      leadId: toId(form.leadId),
      assignedTo: toId(form.assignedTo),
    };
    try {
      if (isEdit) {
        await api.put(`/activities/${id}`, payload);
        showToast('Activity updated successfully.');
      } else {
        await api.post('/activities', payload);
        showToast('Activity saved successfully.');
      }
      navigate('/activities');
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
      <PageHeader title={isEdit ? 'Edit Activity' : 'Log Activity'} subtitle="Fields marked * are mandatory" />

      <div className="card">
        <div className="card-body">
          {serverError && <div className="alert alert-danger">{serverError}</div>}

          <form onSubmit={handleSubmit} noValidate>
            <div className="row g-3">
              <FormField label="Activity Type" name="activityType" error={errors.activityType} required className="col-md-4">
                <select id="activityType" name="activityType" className={selectClass('activityType')} value={form.activityType} onChange={handleChange}>
                  {ACTIVITY_TYPES.map((t) => (
                    <option key={t}>{t}</option>
                  ))}
                </select>
              </FormField>
              <FormField label="Date & Time" name="activityDate" error={errors.activityDate} required className="col-md-4">
                <input id="activityDate" name="activityDate" type="datetime-local" className={inputClass('activityDate')} value={form.activityDate} onChange={handleChange} />
              </FormField>
              <FormField label="Status" name="status" error={errors.status} required className="col-md-4">
                <select id="status" name="status" className={selectClass('status')} value={form.status} onChange={handleChange}>
                  {ACTIVITY_STATUSES.map((s) => (
                    <option key={s}>{s}</option>
                  ))}
                </select>
              </FormField>
              <FormField label="Subject" name="subject" error={errors.subject} required className="col-12">
                <input id="subject" name="subject" className={inputClass('subject')} maxLength={150} value={form.subject} onChange={handleChange} />
              </FormField>
              <FormField label="Customer" name="customerId" error={errors.customerId} className="col-md-4">
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
              <FormField label="Assigned To" name="assignedTo" error={errors.assignedTo} className="col-md-4">
                <select id="assignedTo" name="assignedTo" className={selectClass('assignedTo')} value={form.assignedTo} onChange={handleChange} disabled={users.length <= 1}>
                  {users.map((u) => (
                    <option key={u.id} value={u.id}>
                      {u.name}
                    </option>
                  ))}
                </select>
              </FormField>
              <FormField label="Description" name="description" error={errors.description} className="col-12">
                <textarea id="description" name="description" rows={3} className={inputClass('description')} maxLength={1000} value={form.description} onChange={handleChange} />
              </FormField>
            </div>

            <div className="d-flex gap-2 mt-4">
              <button type="submit" className="btn btn-primary" disabled={saving}>
                {saving && <span className="spinner-border spinner-border-sm me-1"></span>}
                {isEdit ? 'Update Activity' : 'Save Activity'}
              </button>
              <Link to="/activities" className="btn btn-outline-secondary">
                Cancel
              </Link>
            </div>
          </form>
        </div>
      </div>
    </>
  );
}
