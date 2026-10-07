import { useEffect, useState } from 'react';
import { Link, useLocation } from 'react-router-dom';
import api from '../api/client';
import type { FollowUp, PagedResult } from '../types';
import { formatDate } from '../utils/format';

// Header notifications: overdue and today's follow-ups of the logged in user's scope
export default function NotificationBell() {
  const location = useLocation();
  const [open, setOpen] = useState(false);
  const [overdue, setOverdue] = useState<FollowUp[]>([]);
  const [today, setToday] = useState<FollowUp[]>([]);
  const [count, setCount] = useState(0);

  // reload when the user moves to another page
  useEffect(() => {
    const load = async () => {
      try {
        const [overdueRes, todayRes] = await Promise.all([
          api.get<PagedResult<FollowUp>>('/followups', { params: { view: 'overdue', pageSize: 5 } }),
          api.get<PagedResult<FollowUp>>('/followups', { params: { view: 'today', pageSize: 5 } }),
        ]);
        setOverdue(overdueRes.data.data);
        setToday(todayRes.data.data);
        setCount(overdueRes.data.pagination.total + todayRes.data.pagination.total);
      } catch {
        // notifications are not critical, ignore errors
      }
    };
    load();
  }, [location.pathname]);

  return (
    <div className="position-relative">
      <button
        className="btn btn-link text-white position-relative p-1"
        onClick={() => setOpen(!open)}
        aria-label={`Notifications (${count})`}
        aria-expanded={open}
      >
        <i className="bi bi-bell fs-5"></i>
        {count > 0 && (
          <span className="position-absolute top-0 start-100 translate-middle badge rounded-pill bg-danger" style={{ fontSize: '0.65rem' }}>
            {count > 99 ? '99+' : count}
          </span>
        )}
      </button>
      {open && (
        <div className="dropdown-menu dropdown-menu-end show mt-2 p-0 shadow" style={{ right: 0, left: 'auto', width: 320 }} onMouseLeave={() => setOpen(false)}>
          <div className="px-3 py-2 border-bottom fw-semibold">Notifications</div>
          {count === 0 && <div className="px-3 py-3 small text-muted">No pending follow-ups for today.</div>}
          {overdue.map((f) => (
            <NotificationItem key={`o${f.id}`} item={f} label="Overdue" color="danger" onClick={() => setOpen(false)} />
          ))}
          {today.map((f) => (
            <NotificationItem key={`t${f.id}`} item={f} label="Today" color="primary" onClick={() => setOpen(false)} />
          ))}
          <Link to="/followups" className="dropdown-item text-center small border-top py-2" onClick={() => setOpen(false)}>
            View all follow-ups
          </Link>
        </div>
      )}
    </div>
  );
}

function NotificationItem({ item, label, color, onClick }: { item: FollowUp; label: string; color: string; onClick: () => void }) {
  return (
    <Link to="/followups" className="dropdown-item py-2 border-bottom" onClick={onClick}>
      <div className="d-flex justify-content-between">
        <span className="text-truncate me-2 small fw-semibold">{item.subject}</span>
        <span className={`badge bg-${color}`}>{label}</span>
      </div>
      <div className="small text-muted text-truncate">
        {item.customerName || item.leadName || item.opportunityName || '-'} · {formatDate(item.followUpDate)}
      </div>
    </Link>
  );
}
