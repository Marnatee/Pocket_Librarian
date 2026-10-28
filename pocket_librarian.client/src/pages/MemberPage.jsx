// Presentation layer / UI
// Member page feature placeholders.

import FeaturePlaceholder from "../components/FeaturePlaceholder";

const memberFeatures = [
    "Member notifications",
    "Reservation Queue",
    "Member Profile Management",
];

function MemberPage() {
    return (
        <section className="page-view" aria-labelledby="member-page-heading">
            <div className="page-introduction">
                <p className="page-eyebrow">Member workspace</p>
                <h1 id="member-page-heading">Member Page</h1>
                <p>Member tools will be connected to the login flow as features are completed.</p>
            </div>

            <div className="feature-grid">
                {memberFeatures.map((feature) => (
                    <FeaturePlaceholder key={feature} title={feature} />
                ))}
            </div>
        </section>
    );
}

export default MemberPage;
