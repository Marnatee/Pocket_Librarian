// Presentation layer / UI
// Staff page feature placeholders and active features.

import { useState } from "react";
import FeaturePlaceholder from "../components/FeaturePlaceholder";
import ReturnsQueueManager from "../components/ReturnsQueueManager";

const otherStaffFeatures = [
    "Fulfillment queue",
    "Catalogue administration",
    "Book donation requests",
];

function StaffPage() {
    // For now, using a placeholder staff ID. This should come from authentication context.
    const staffId = 1; // TODO: Get from auth context
    const [activeFeature, setActiveFeature] = useState(null);

    return (
        <section className="page-view" aria-labelledby="staff-page-heading">
            <div className="page-introduction">
                <p className="page-eyebrow">Staff workspace</p>
                <h1 id="staff-page-heading">Staff Page</h1>
                <p>Staff tools to manage library operations.</p>
            </div>

            {activeFeature === "Returns queue" ? (
                <div className="feature-view">
                    <button 
                        className="btn-back-to-features"
                        onClick={() => setActiveFeature(null)}
                    >
                        ← Back to Features
                    </button>
                    <ReturnsQueueManager staffId={staffId} />
                </div>
            ) : (
                <div className="feature-grid">
                    <div 
                        className="feature-placeholder-clickable"
                        onClick={() => setActiveFeature("Returns queue")}
                        role="button"
                        tabIndex={0}
                        onKeyDown={(e) => e.key === "Enter" && setActiveFeature("Returns queue")}
                    >
                        <h3>Returns queue</h3>
                        <p>Process returned books and assign new tasks</p>
                    </div>
                    {otherStaffFeatures.map((feature) => (
                        <FeaturePlaceholder key={feature} title={feature} />
                    ))}
                </div>
            )}
        </section>
    );
}

export default StaffPage;
