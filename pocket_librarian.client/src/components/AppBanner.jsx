// Presentation layer / UI
// Persistent application banner shared by the Home, Member, and Staff pages.

import ConnectionStatusDashboard from "./ConnectionStatusDashboard";

function AppBanner({
    currentPage,
    onPageChange,
    statuses,
    isRefreshing,
    lastChecked,
    onRefresh,
}) {
    return (
        <header className="app-banner">
            <div className="app-banner-brand">
                <p className="brand-kicker">Pocket Librarian</p>
                <p className="brand-subtitle">A calmer way to manage your library</p>
            </div>

            <nav className="page-navigation" aria-label="Main navigation">
                <button
                    className={currentPage === "home" ? "active" : ""}
                    type="button"
                    onClick={() => onPageChange("home")}
                >
                    Home
                </button>
                <button
                    className={currentPage === "member" ? "active" : ""}
                    type="button"
                    onClick={() => onPageChange("member")}
                >
                    Member page
                </button>
                <button
                    className={currentPage === "staff" ? "active" : ""}
                    type="button"
                    onClick={() => onPageChange("staff")}
                >
                    Staff page
                </button>
            </nav>

            <div className="banner-actions">
                {/* OWEN HANDOFF: Add member authentication behavior here when that feature is implemented. */}
                <button className="login-button" type="button">
                    Member login
                </button>
                {/* OWEN HANDOFF: Add staff authentication behavior here when that feature is implemented. */}
                <button className="login-button" type="button">
                    Staff login
                </button>
            </div>

            <ConnectionStatusDashboard
                statuses={statuses}
                isRefreshing={isRefreshing}
                lastChecked={lastChecked}
                onRefresh={onRefresh}
            />
        </header>
    );
}

export default AppBanner;
