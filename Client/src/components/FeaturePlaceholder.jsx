// Presentation layer / UI
// Reusable placeholder for a feature that has not been implemented yet.

function FeaturePlaceholder({ title }) {
    return (
        <article className="feature-placeholder">
            <h2>{title}</h2>
            <img
                src="/under-construction.jpg"
                alt={`${title} is under construction`}
            />
        </article>
    );
}

export default FeaturePlaceholder;
