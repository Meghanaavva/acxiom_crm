import { useEffect, useState, type ChangeEvent, type FormEvent } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import api, { getErrorMessage, getFieldErrors } from '../../api/client';
import { fetchUserLookup } from '../../api/services';
import FormField from '../../components/FormField';
import Loader from '../../components/Loader';
import PageHeader from '../../components/PageHeader';
import { useAuth } from '../../context/AuthContext';
import { useToast } from '../../context/ToastContext';
import type { Lead, UserLookup } from '../../types';
import { LEAD_MAX_EXPECTED_VALUE, LEAD_PRIORITIES, LEAD_SOURCES, LEAD_STATUS_TRANSITIONS } from '../../utils/constants';
import { collectErrors, email, maxLength, numberRange, phone, required, type Errors } from '../../utils/validation';

// statuses allowed when a lead is first created
const CREATE_STATUSES = ['New', 'Contacted', 'Qualified'];

const emptyForm = {
  leadName: '',
  email: '',
  phone: '',
  companyName: '',
  source: 'Website',
  status: 'New',
  priority: 'Medium',
  expectedValue: '0',
  notes: '',
  assignedTo: '',
};

type FormState = typeof emptyForm;

function validateForm(form: FormState): Errors {
  const noContact = !form.email.trim() && !form.phone.trim();
  return collectErrors({
    leadName: [required(form.leadName, 'Lead Name'), maxLength(form.leadName, 100, 'Lead Name')],
    email: [noContact ? 'Enter at least an email or a phone number.' : '', email(form.email), maxLength(form.email, 150, 'Email')],
    phone: [noContact ? 'Enter at least an email or a phone number.' : '', phone(form.phone)],
    companyName: [maxLength(form.companyName, 100, 'Company Name')],
    source: [required(form.source, 'Source')],
    status: [required(form.status, 'Status')],
    priority: [required(form.priority, 'Priority')],
    expectedValue: [
      numberRange(form.expectedValue, 0, LEAD_MAX_EXPECTED_VALUE, `Expected Value must be between 0 and ${LEAD_MAX_EXPECTED_VALUE}.`),
    ],
    notes: [maxLength(form.notes, 1000, 'Notes')],
  });
}

export default function LeadForm() {
  const { id } = useParams();
  const isEdit = !!id;
  const navigate = useNavigate();
  const { user } = useAuth();
  const { showToast } = useToast();

  const [form, setForm] = useState<FormState>({ ...emptyForm, assignedTo: String(user?.id ?? '') });
  // status of the lead when it was loaded - used to work out allowed transitions
  const [originalStatus, setOriginalStatus] = useState('');
  const [errors, setErrors] = useState<Errors>({});
  const [users, setUsers] = useState<UserLookup[]>([]);
  const [loading, setLoading] = useState(isEdit);
  const [saving, setSaving] = useState(false);
  const [serverError, setServerError] = useState('');

  useEffect(() => {
    fetchUserLookup().then(setUsers).catch(() => setUsers([]));
  }, []);

  useEffect(() => {
    if (!isEdit) return;
    api
      .get<Lead>(`/leads/${id}`)
      .then(({ data }) => {
        setOriginalStatus(data.status);
        setForm({
          leadName: data.leadName,
          email: data.email || '',
          phone: data.phone || '',
          companyName: data.companyName || '',
          source: data.source,
          status: data.status,
          priority: data.priority,
          expectedValue: String(data.expectedValue ?? 0),
          notes: data.notes || '',
          assignedTo: String(data.assignedTo),
        });
      })
      .catch((err) => {
        showToast(getErrorMessage(err), 'danger');
        navigate('/leads');
      })
      .finally(() => setLoading(false));
  }, [id, isEdit, navigate, showToast]);

  const isClosed = originalStatus === 'Converted' || originalStatus === 'Lost';
  const statusOptions = isEdit ? [originalStatus, ...(LEAD_STATUS_TRANSITIONS[originalStatus] || [])] : CREATE_STATUSES;

  const handleChange = (e: ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>) => {
    const { name, value } = e.target;
    const next = { ...form, [name]: value };
    setForm(next);
    // email/phone share the "at least one" rule so clear both
    if (name === 'email' || name === 'phone') {
      setErrors({ ...errors, email: '', phone: '' });
    } else if (errors[name]) {
      setErrors({ ...errors, [name]: '' });
    }
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
      expectedValue: form.expectedValue === '' ? 0 : Number(form.expectedValue),
      assignedTo: form.assignedTo ? Number(form.assignedTo) : null,
    };
    try {
      if (isEdit) {
        await api.put(`/leads/${id}`, payload);
        showToast('Lead updated successfully.');
        navigate(`/leads/${id}`);
      } else {
        const { data } = await api.post<Lead>('/leads', payload);
        showToast('Lead created successfully.');
        navigate(`/leads/${data.id}`);
      }
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
      <PageHeader title={isEdit ? 'Edit Lead' : 'Add Lead'} subtitle="Fields marked * are mandatory" />

      <div className="card">
        <div className="card-body">
          {isClosed && (
            <div className="alert alert-warning">
              This lead is <strong>{originalStatus}</strong> and can no longer be edited.
            </div>
          )}
          {serverError && <div className="alert alert-danger">{serverError}</div>}

          <form onSubmit={handleSubmit} noValidate>
            <fieldset disabled={isClosed}>
              <div className="row g-3">
                <FormField label="Lead Name" name="leadName" error={errors.leadName} required>
                  <input id="leadName" name="leadName" className={inputClass('leadName')} maxLength={100} value={form.leadName} onChange={handleChange} />
                </FormField>
                <FormField label="Company Name" name="companyName" error={errors.companyName}>
                  <input
                    id="companyName"
                    name="companyName"
                    className={inputClass('companyName')}
                    maxLength={100}
                    value={form.companyName}
                    onChange={handleChange}
                  />
                </FormField>
                <FormField label="Email" name="email" error={errors.email} hint="Email or phone is required">
                  <input id="email" name="email" type="email" className={inputClass('email')} maxLength={150} value={form.email} onChange={handleChange} />
                </FormField>
                <FormField label="Phone" name="phone" error={errors.phone} hint="10 digit mobile number">
                  <input id="phone" name="phone" type="tel" className={inputClass('phone')} maxLength={10} value={form.phone} onChange={handleChange} />
                </FormField>
                <FormField label="Source" name="source" error={errors.source} required className="col-md-4">
                  <select id="source" name="source" className={selectClass('source')} value={form.source} onChange={handleChange}>
                    {LEAD_SOURCES.map((s) => (
                      <option key={s}>{s}</option>
                    ))}
                  </select>
                </FormField>
                <FormField label="Status" name="status" error={errors.status} required className="col-md-4">
                  <select id="status" name="status" className={selectClass('status')} value={form.status} onChange={handleChange}>
                    {statusOptions.map((s) => (
                      <option key={s}>{s}</option>
                    ))}
                  </select>
                </FormField>
                <FormField label="Priority" name="priority" error={errors.priority} required className="col-md-4">
                  <select id="priority" name="priority" className={selectClass('priority')} value={form.priority} onChange={handleChange}>
                    {LEAD_PRIORITIES.map((p) => (
                      <option key={p}>{p}</option>
                    ))}
                  </select>
                </FormField>
                <FormField label="Expected Value (₹)" name="expectedValue" error={errors.expectedValue}>
                  <input
                    id="expectedValue"
                    name="expectedValue"
                    type="number"
                    min={0}
                    max={LEAD_MAX_EXPECTED_VALUE}
                    step="0.01"
                    className={inputClass('expectedValue')}
                    value={form.expectedValue}
                    onChange={handleChange}
                  />
                </FormField>
                <FormField label="Assigned Sales Executive" name="assignedTo" error={errors.assignedTo}>
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
                <FormField label="Notes" name="notes" error={errors.notes} className="col-12">
                  <textarea id="notes" name="notes" rows={3} className={inputClass('notes')} maxLength={1000} value={form.notes} onChange={handleChange} />
                </FormField>
              </div>
            </fieldset>

            <div className="d-flex gap-2 mt-4">
              {!isClosed && (
                <button type="submit" className="btn btn-primary" disabled={saving}>
                  {saving && <span className="spinner-border spinner-border-sm me-1"></span>}
                  {isEdit ? 'Update Lead' : 'Save Lead'}
                </button>
              )}
              <Link to={isEdit ? `/leads/${id}` : '/leads'} className="btn btn-outline-secondary">
                Cancel
              </Link>
            </div>
          </form>
        </div>
      </div>
    </>
  );
}
