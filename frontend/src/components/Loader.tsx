export default function Loader({ fullPage = false, text = 'Loading...' }: { fullPage?: boolean; text?: string }) {
  return (
    <div className={`d-flex justify-content-center align-items-center ${fullPage ? 'vh-100' : 'py-5'}`}>
      <div className="spinner-border text-primary me-2" role="status" aria-hidden="true"></div>
      <span className="text-muted">{text}</span>
    </div>
  );
}
