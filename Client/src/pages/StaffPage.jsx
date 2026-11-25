// Presentation layer / UI
// Staff page feature navigation and staff-only tools.

import { useState } from "react";
import ReturnsQueueManager from "../components/ReturnsQueueManager";
import FulfillmentQueueManager from "../components/FulfillmentQueueManager";
import CatalogueAdministrationManager from "../components/CatalogueAdministrationManager";

function StaffPage({ currentUser }) {
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
                        type="button"
                        onClick={() => setActiveFeature(null)}
                    >
                        ← Back to Features
                    </button>
                    <ReturnsQueueManager currentUser={currentUser} />
                </div>
            ) : activeFeature === "Fulfillment queue" ? (
                <div className="feature-view">
                    <button
                        className="btn-back-to-features"
                        type="button"
                        onClick={() => setActiveFeature(null)}
                    >
                        ← Back to Features
                    </button>
                    <FulfillmentQueueManager currentUser={currentUser} />
                </div>
            ) : activeFeature === "Catalogue administration" ? (
                <div className="feature-view">
                    <button
                        className="btn-back-to-features"
                        type="button"
                        onClick={() => setActiveFeature(null)}
                    >
                        ← Back to Features
                    </button>
                    <CatalogueAdministrationManager />
                </div>
            ) : (
                <div className="feature-grid">
                    <button
                        className="feature-placeholder-clickable"
                        type="button"
                        onClick={() => setActiveFeature("Returns queue")}
                    >
                        <h3>Returns queue</h3>
                        <p>Process returned books and assign new tasks</p>
                    </button>
                    <button
                        className="feature-placeholder-clickable"
                        type="button"
                        onClick={() => setActiveFeature("Fulfillment queue")}
                    >
                        <h3>Fulfillment queue</h3>
                        <p>Locate reserved books and prepare for member pickup</p>
                    </button>
                    <button
                        className="feature-placeholder-clickable"
                        type="button"
                        onClick={() => setActiveFeature("Catalogue administration")}
                    >
                        <h3>Catalogue administration</h3>
                        <p>Manage catalogue records, donations, and physical copies</p>
                    </button>
                </div>
            )}
        </section>
    );
}

export default StaffPage;
