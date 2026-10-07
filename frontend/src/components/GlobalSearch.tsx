import { useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';

// Search box in the header. Results page searches customers, leads and opportunities together.
export default function GlobalSearch() {
  const navigate = useNavigate();
  const [query, setQuery] = useState('');

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault();
    const text = query.trim();
    if (text) navigate(`/search?q=${encodeURIComponent(text)}`);
  };

  return (
    <form className="d-none d-md-block mx-3 flex-grow-1" style={{ maxWidth: 420 }} onSubmit={handleSubmit} role="search">
      <div className="input-group input-group-sm">
        <span className="input-group-text bg-white border-0">
          <i className="bi bi-search"></i>
        </span>
        <input
          type="search"
          className="form-control border-0"
          placeholder="Search customers, leads, opportunities..."
          aria-label="Global search"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
        />
      </div>
    </form>
  );
}
