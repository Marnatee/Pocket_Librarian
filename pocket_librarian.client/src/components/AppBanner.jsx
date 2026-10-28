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
    onManualDatabaseConnect,
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

            <div className="login-panel">
                <p className="login-panel-title">Staff / member login</p>
                <div className="login-fields">
                    <label htmlFor="account-name">
                        Account name
                        <input id="account-name" type="text" />
                    </label>
                    <label htmlFor="account-password">
                        Password
                        <input id="account-password" type="password" />
                    </label>
                </div>
                <div className="login-actions">
                    {/* OWEN HANDOFF: Add sign-in behavior for staff and members here. */}
                    <button className="login-button" type="button">
                        Sign in
                    </button>
                    {/* OWEN HANDOFF: Add account-registration behavior for staff and members here. */}
                    <button className="login-button" type="button">
                        Sign up
                    </button>
                </div>
            </div>

            <ConnectionStatusDashboard
                statuses={statuses}
                isRefreshing={isRefreshing}
                lastChecked={lastChecked}
                onRefresh={onRefresh}
                onManualDatabaseConnect={onManualDatabaseConnect}
            />
        </header>
    );
}

export default AppBanner;
