interface Props {
  label: string;
  field: string;
  sortBy: string;
  sortOrder: 'asc' | 'desc';
  onSort: (field: string) => void;
}

export default function SortableHeader({ label, field, sortBy, sortOrder, onSort }: Props) {
  const active = sortBy === field;
  return (
    <th role="button" className="text-nowrap user-select-none" onClick={() => onSort(field)}>
      {label}{' '}
      <i className={`bi ${active ? (sortOrder === 'asc' ? 'bi-caret-up-fill' : 'bi-caret-down-fill') : 'bi-chevron-expand text-muted'}`}></i>
    </th>
  );
}
