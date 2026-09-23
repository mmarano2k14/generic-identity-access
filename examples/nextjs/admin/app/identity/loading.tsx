export default function IdentityLoading() {
  return (
    <div className="ia-page ia-loading-page" aria-busy="true" aria-label="Loading identity administration">
      <div className="ia-skeleton ia-skeleton-eyebrow" />
      <div className="ia-skeleton ia-skeleton-heading" />
      <div className="ia-skeleton ia-skeleton-copy" />
      <div className="ia-skeleton-grid">
        <div className="ia-skeleton ia-skeleton-card" />
        <div className="ia-skeleton ia-skeleton-card" />
        <div className="ia-skeleton ia-skeleton-card" />
        <div className="ia-skeleton ia-skeleton-card" />
      </div>
      <div className="ia-skeleton ia-skeleton-table" />
    </div>
  );
}
