// Presentation layer / UI
// Member page and authenticated member features.

import { useState } from "react";
import MemberNotifications from "../components/MemberNotifications";
import MemberProfileManagement from "../components/MemberProfileManagement";
import ReservationManager from "../components/ReservationManager";
import BookDonationForm from "../components/BookDonationForm";

function MemberPage({ currentUser, onProfileUpdated, onOpenNotifications }) {
    const currentUserId = Number(currentUser?.userId);
    const [activeFeature, setActiveFeature] = useState(null);

    return (
        <section className="page-view" aria-labelledby="member-page-heading">
            <div className="page-introduction">
                <p className="page-eyebrow">Member workspace</p>
                <h1 id="member-page-heading">Member Page</h1>
                <p>Manage your profile, review notifications, and manage your reservations.</p>
            </div>

            {activeFeature === "Book donations" ? (
                <div className="feature-view">
                    <button
                        className="btn-back-to-features"
                        type="button"
                        onClick={() => setActiveFeature(null)}
                    >
                        ← Back to Member Features
                    </button>
                    <BookDonationForm />
                </div>
            ) : (
                <ReservationManager userId={currentUserId}>
                    <div className="member-tools-grid">
                        <MemberProfileManagement onProfileUpdated={onProfileUpdated} />
                        <MemberNotifications onOpenNotifications={onOpenNotifications} />
                    </div>
                    <div className="member-donation-feature-wrap">
                        <button
                            className="feature-placeholder-clickable member-donation-feature"
                            type="button"
                            onClick={() => setActiveFeature("Book donations")}
                        >
                            <h3>Book donations</h3>
                            <p>Submit a book for staff verification and possible addition to the catalogue</p>
                            <span className="member-feature-link">Open donation form →</span>
                        </button>
                    </div>
                </ReservationManager>
            )}
        </section>
    );
}

export default MemberPage;
