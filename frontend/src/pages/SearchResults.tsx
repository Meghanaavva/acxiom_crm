import { useEffect, useState, type ReactNode } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import api, { getErrorMessage } from '../api/client';
import Loader from '../components/Loader';
import PageHeader from '../components/PageHeader';
import StatusBadge from '../components/StatusBadge';
import { useToast } from '../context/ToastContext';
import type { Customer, Lead, Opportunity, PagedResult } from '../types';
import { formatCurrency } from '../utils/format';

interface Results {
  customers: PagedResult<Customer>;
  leads: PagedResult<Lead>;
  opportunities: PagedResult<Opportunity>;
}

export default function SearchResults() {
  const [searchParams] = useSearchParams();
  const query = searchParams.get('q') || '';
  const { showToast } = useToast();
  const [results, setResults] = useState<Results | null>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (!query) return;
    const params = { search: query, pageSize: 10 };
    setLoading(true);
    Promise.all([
      api.get<PagedResult<Customer>>('/customers', { params }),
      api.get<PagedResult<Lead>>('/leads', { params }),
      api.get<PagedResult<Opportunity>>('/opportunities', { params }),
    ])
      .then(([customers, leads, opportunities]) =>
        setResults({ customers: customers.data, leads: leads.data, opportunities: opportunities.data }),
      )
      .catch((err) => showToast(getErrorMessage(err), 'danger'))
      .finally(() => setLoading(false));
  }, [query, showToast]);

  const total = results
    ? results.customers.pagination.total + results.leads.pagination.total + results.opportunities.pagination.total
    : 0;

  return (
    <>
      <PageHeader title="Search Results" subtitle={query ? `${total} result(s) for "${query}"` : 'Type something in the search box'} />
      {loading && <Loader />}
      {!loading && results && (
        <div className="row g-3">
          <div className="col-lg-4">
            <ResultCard title="Customers" icon="bi-people" count={results.customers.pagination.total} viewAll="/customers">
              {results.customers.data.map((c) => (
                <Link key={c.id} to={`/customers/${c.id}`} className="list-group-item list-group-item-action">
                  <div className="d-flex justify-content-between">
                    <span className="fw-semibold">{c.customerName}</span>
                    <StatusBadge status={c.status} />
                  </div>
                  <div className="small text-muted">
                    {c.companyName || '-'} · {c.email}
                  </div>
                </Link>
              ))}
            </ResultCard>
          </div>
          <div className="col-lg-4">
            <ResultCard title="Leads" icon="bi-person-plus" count={results.leads.pagination.total} viewAll="/leads">
              {results.leads.data.map((l) => (
                <Link key={l.id} to={`/leads/${l.id}`} className="list-group-item list-group-item-action">
                  <div className="d-flex justify-content-between">
                    <span className="fw-semibold">{l.leadName}</span>
                    <StatusBadge status={l.status} />
                  </div>
                  <div className="small text-muted">
                    {l.companyName || '-'} · {l.source}
                  </div>
                </Link>
              ))}
            </ResultCard>
          </div>
          <div className="col-lg-4">
            <ResultCard title="Opportunities" icon="bi-briefcase" count={results.opportunities.pagination.total} viewAll="/opportunities">
              {results.opportunities.data.map((o) => (
                <Link key={o.id} to={`/opportunities/${o.id}`} className="list-group-item list-group-item-action">
                  <div className="d-flex justify-content-between">
                    <span className="fw-semibold">{o.opportunityName}</span>
                    <StatusBadge status={o.stage} />
                  </div>
                  <div className="small text-muted">
                    {o.customerName} · {formatCurrency(o.amount)}
                  </div>
                </Link>
              ))}
            </ResultCard>
          </div>
        </div>
      )}
    </>
  );
}

function ResultCard({ title, icon, count, viewAll, children }: { title: string; icon: string; count: number; viewAll: string; children: ReactNode }) {
  return (
    <div className="card h-100">
      <div className="card-header bg-white d-flex justify-content-between align-items-center">
        <span className="fw-semibold">
          <i className={`bi ${icon} me-2`}></i>
          {title} <span className="badge bg-secondary ms-1">{count}</span>
        </span>
        <Link to={viewAll} className="small">
          View all
        </Link>
      </div>
      <div className="list-group list-group-flush">
        {count === 0 ? <div className="list-group-item text-muted small">No matches.</div> : children}
      </div>
    </div>
  );
}
