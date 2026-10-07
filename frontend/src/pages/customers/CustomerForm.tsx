import { useEffect, useState, type ChangeEvent, type FormEvent } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import api, { getErrorMessage, getFieldErrors } from '../../api/client';
import { fetchUserLookup } from '../../api/services';
import FormField from '../../components/FormField';
import Loader from '../../components/Loader';
import PageHeader from '../../components/PageHeader';
import { useAuth } from '../../context/AuthContext';
import { useToast } from '../../context/ToastContext';
import type { Customer, UserLookup } from '../../types';
import { CUSTOMER_STATUSES } from '../../utils/constants';
import { collectErrors, email, maxLength, phone, required, type Errors } from '../../utils/validation';

const emptyForm = {
  customerName: '',
  email: '',
  phone: '',
  companyName: '',
  address: '',
  city: '',
  state: '',
  status: 'Active',
  notes: '',
  assignedTo: '',
};

type FormState = typeof emptyForm;

function validateForm(form: FormState): Errors {
  return collectErrors({
    customerName: [required(form.customerName, 'Customer Name'), maxLength(form.customerName, 100, 'Customer Name')],
    email: [required(form.email, 'Email'), email(form.email), maxLength(form.email, 150, 'Email')],
    phone: [required(form.phone, 'Phone'), phone(form.phone)],
    companyName: [maxLength(form.companyName, 100, 'Company Name')],
    address: [maxLength(form.address, 250, 'Address')],
    city: [maxLength(form.city, 50, 'City')],
    state: [maxLength(form.state, 50, 'State')],
    status: [required(form.status, 'Status')],
    notes: [maxLength(form.notes, 1000, 'Notes')],
  });
}

export default function CustomerForm() {
  const { id } = useParams();
  const isEdit = !!id;
  const navigate = useNavigate();
  const { user } = useAuth();
  const { showToast } = useToast();

  const [form, setForm] = useState<FormState>({ ...emptyForm, assignedTo: String(user?.id ?? '') });
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
      .get<Customer>(`/customers/${id}`)
      .then(({ data }) => {
        setForm({
          customerName: data.customerName,
          email: data.email,
          phone: data.phone,
          companyName: data.companyName || '',
          address: data.address || '',
          city: data.city || '',
          state: data.state || '',
          status: data.status,
          notes: data.notes || '',
          assignedTo: String(data.assignedTo),
        });
      })
      .catch((err) => {
        showToast(getErrorMessage(err), 'danger');
        navigate('/customers');
      })
      .finally(() => setLoading(false));
  }, [id, isEdit, navigate, showToast]);

  const handleChange = (e: ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>) => {
    const { name, value } = e.target;
    setForm({ ...form, [name]: value });
    // clear the error of this field while typing
    if (errors[name]) setErrors({ ...errors, [name]: '' });
  };

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setServerError('');
    const validationErrors = validateForm(form);
    setErrors(validationErrors);
    if (Object.keys(validationErrors).length > 0) return;

    setSaving(true);
    const payload = { ...form, assignedTo: form.assignedTo ? Number(form.assignedTo) : null };
    try {
      if (isEdit) {
        await api.put(`/customers/${id}`, payload);
        showToast('Customer updated successfully.');
        navigate(`/customers/${id}`);
      } else {
        const { data } = await api.post<Customer>('/customers', payload);
        showToast('Customer created successfully.');
        navigate(`/customers/${data.id}`);
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

  return (
    <>
      <PageHeader title={isEdit ? 'Edit Customer' : 'Add Customer'} subtitle="Fields marked * are mandatory" />

      <div className="card">
        <div className="card-body">
          {serverError && <div className="alert alert-danger">{serverError}</div>}

          <form onSubmit={handleSubmit} noValidate>
            <div className="row g-3">
              <FormField label="Customer Name" name="customerName" error={errors.customerName} required>
                <input id="customerName" name="customerName" className={inputClass('customerName')} maxLength={100} value={form.customerName} onChange={handleChange} />
              </FormField>
              <FormField label="Company Name" name="companyName" error={errors.companyName}>
                <input id="companyName" name="companyName" className={inputClass('companyName')} maxLength={100} value={form.companyName} onChange={handleChange} />
              </FormField>
              <FormField label="Email" name="email" error={errors.email} required>
                <input id="email" name="email" type="email" className={inputClass('email')} maxLength={150} value={form.email} onChange={handleChange} />
              </FormField>
              <FormField label="Phone" name="phone" error={errors.phone} required hint="10 digit mobile number">
                <input id="phone" name="phone" type="tel" className={inputClass('phone')} maxLength={10} value={form.phone} onChange={handleChange} />
              </FormField>
              <FormField label="Address" name="address" error={errors.address} className="col-12">
                <input id="address" name="address" className={inputClass('address')} maxLength={250} value={form.address} onChange={handleChange} />
              </FormField>
              <FormField label="City" name="city" error={errors.city} className="col-md-4">
                <input id="city" name="city" className={inputClass('city')} maxLength={50} value={form.city} onChange={handleChange} />
              </FormField>
              <FormField label="State" name="state" error={errors.state} className="col-md-4">
                <input id="state" name="state" className={inputClass('state')} maxLength={50} value={form.state} onChange={handleChange} />
              </FormField>
              <FormField label="Status" name="status" error={errors.status} required className="col-md-4">
                <select id="status" name="status" className={`form-select ${errors.status ? 'is-invalid' : ''}`} value={form.status} onChange={handleChange}>
                  {CUSTOMER_STATUSES.map((s) => (
                    <option key={s}>{s}</option>
                  ))}
                </select>
              </FormField>
              <FormField label="Assigned Sales Executive" name="assignedTo" error={errors.assignedTo}>
                <select
                  id="assignedTo"
                  name="assignedTo"
                  className={`form-select ${errors.assignedTo ? 'is-invalid' : ''}`}
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

            <div className="d-flex gap-2 mt-4">
              <button type="submit" className="btn btn-primary" disabled={saving}>
                {saving && <span className="spinner-border spinner-border-sm me-1"></span>}
                {isEdit ? 'Update Customer' : 'Save Customer'}
              </button>
              <Link to={isEdit ? `/customers/${id}` : '/customers'} className="btn btn-outline-secondary">
                Cancel
              </Link>
            </div>
          </form>
        </div>
      </div>
    </>
  );
}
