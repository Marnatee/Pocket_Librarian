// Presentation layer / UI
// Staff page feature placeholders.

import FeaturePlaceholder from "../components/FeaturePlaceholder";

const staffFeatures = [
    "Fulfillment queue",
    "Returns queue",
    "Catalogue administration",
    "Book donation requests",
];

function StaffPage() {
    return (
        <section className="page-view" aria-labelledby="staff-page-heading">
            <div className="page-introduction">
                <p className="page-eyebrow">Staff workspace</p>
                <h1 id="staff-page-heading">Staff Page</h1>
                <p>Staff tools will be connected to staff authentication as features are completed.</p>
            </div>

            <div className="feature-grid">
                {staffFeatures.map((feature) => (
                    <FeaturePlaceholder key={feature} title={feature} />
                ))}
            </div>
        </section>
    );
}

export default StaffPage;
