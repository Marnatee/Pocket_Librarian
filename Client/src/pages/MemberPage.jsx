// Presentation layer / UI
// Member page and authenticated member features.

import MemberNotifications from "../components/MemberNotifications";
import MemberProfileManagement from "../components/MemberProfileManagement";
import ReservationManager from "../components/ReservationManager";

function MemberPage({ currentUser, onProfileUpdated }) {
    const currentUserId = Number(currentUser?.userId);

    return (
        <section className="page-view" aria-labelledby="member-page-heading">
            <div className="page-introduction">
                <p className="page-eyebrow">Member workspace</p>
                <h1 id="member-page-heading">Member Page</h1>
                <p>Manage your profile, review notifications, and manage your reservations.</p>
            </div>

            <ReservationManager userId={currentUserId} />
            <div className="member-tools-grid">
                <MemberProfileManagement onProfileUpdated={onProfileUpdated} />
                <MemberNotifications />
            </div>
        </section>
    );
}

export default MemberPage;
