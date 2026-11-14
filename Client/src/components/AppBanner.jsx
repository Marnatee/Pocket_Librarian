// Presentation layer / UI
// Persistent application banner shared by the Home, Member, and Staff pages.

import { useState } from "react";
import ConnectionStatusDashboard from "./ConnectionStatusDashboard";

function AppBanner({
    currentPage,
    onPageChange,
    currentUser,
    authMessage,
    onSignIn,
    onSignUp,
    onSignOut,
    statuses,
    isRefreshing,
    lastChecked,
    onRefresh,
    onManualDatabaseConnect,
}) {
    const [firstName, setFirstName] = useState("");
    const [lastName, setLastName] = useState("");
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");

    function handleSignIn(event) {
        event.preventDefault();
        onSignIn({ email, password });
    }

    function handleSignUp() {
        onSignUp({ firstName, lastName, email, password });
    }

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
                {currentUser && (
                    <button
                        className={currentPage === "member" ? "active" : ""}
                        type="button"
                        onClick={() => onPageChange("member")}
                    >
                        Member page
                    </button>
                )}
                {currentUser?.role === "Staff" && (
                    <button
                        className={currentPage === "staff" ? "active" : ""}
                        type="button"
                        onClick={() => onPageChange("staff")}
                    >
                        Staff page
                    </button>
                )}
            </nav>

            <form className="login-panel" onSubmit={handleSignIn}>
                <p className="login-panel-title">
                    {currentUser ? `${currentUser.email} (${currentUser.role})` : "Staff / member login"}
                </p>
                {!currentUser && <div className="login-fields">
                    <label htmlFor="account-first-name">
                        First name
                        <input
                            id="account-first-name"
                            type="text"
                            value={firstName}
                            onChange={(event) => setFirstName(event.target.value)}
                        />
                    </label>
                    <label htmlFor="account-last-name">
                        Last name
                        <input
                            id="account-last-name"
                            type="text"
                            value={lastName}
                            onChange={(event) => setLastName(event.target.value)}
                        />
                    </label>
                    <label htmlFor="account-email">
                        Email
                        <input
                            id="account-email"
                            type="email"
                            value={email}
                            onChange={(event) => setEmail(event.target.value)}
                        />
                    </label>
                    <label htmlFor="account-password">
                        Password
                        <input
                            id="account-password"
                            type="password"
                            value={password}
                            onChange={(event) => setPassword(event.target.value)}
                        />
                    </label>
                </div>}
                <div className="login-actions">
                    {currentUser ? (
                        <button className="login-button" type="button" onClick={onSignOut}>
                            Sign out
                        </button>
                    ) : (
                        <>
                            <button className="login-button" type="submit">
                                Sign in
                            </button>
                            <button className="login-button" type="button" onClick={handleSignUp}>
                                Sign up
                            </button>
                        </>
                    )}
                </div>
                {authMessage && <p className="login-message" role="status">{authMessage}</p>}
            </form>

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
