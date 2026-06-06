import { Link } from "react-router-dom";

export const NotFoundPage = () => {
  return (
    <section className="card stack-sm">
      <p className="eyebrow">404</p>
      <h3>Page not found</h3>
      <p className="muted">The requested route does not exist in this tenant console.</p>
      <Link className="btn primary" to="/dashboard">
        Return to dashboard
      </Link>
    </section>
  );
};
