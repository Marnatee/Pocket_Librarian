// Presentation layer / UI
// Displays the frontend, backend, and database connection status.

import { useState } from "react";

const statusLabels = {
    online: "Online",
    offline: "Unavailable",
    checking: "Checking...",
    error: "Error",
};

function ConnectionStatusDashboard({
    statuses,
    isRefreshing,
    lastChecked,
    onRefresh,
    onManualDatabaseConnect,
}) {
    const [openDetails, setOpenDetails] = useState({});
    const [isManualFormOpen, setIsManualFormOpen] = useState(false);
    const [manualConnectionString, setManualConnectionString] = useState("");
    const [manualConnectionMessage, setManualConnectionMessage] = useState("");

    function toggleDetails(key) {
        setOpenDetails((currentDetails) => ({
            ...currentDetails,
            [key]: !currentDetails[key],
        }));
    }

    async function submitManualConnection(event) {
        event.preventDefault();

        if (!manualConnectionString.trim()) {
            setManualConnectionMessage("Enter a connection string to test.");
            return;
        }

        const connected = await onManualDatabaseConnect(
            manualConnectionString.trim()
        );

        setManualConnectionMessage(
            connected
                ? "Manual database connection accepted for this running session."
                : "Manual database connection failed."
        );
    }

    return (
        <section
            className="connection-dashboard"
            aria-labelledby="connection-dashboard-heading"
        >
            <div className="connection-dashboard-heading-row">
                <div>
                    <p className="dashboard-eyebrow">System check</p>
                    <h2 id="connection-dashboard-heading">Connection status</h2>
                </div>

                <button
                    className="dashboard-refresh-button"
                    type="button"
                    onClick={onRefresh}
                    disabled={isRefreshing}
                >
                    {isRefreshing ? "Refreshing..." : "Refresh"}
                </button>
            </div>

            <div className="status-card-list">
                {Object.entries(statuses).map(([key, status]) => (
                    <article className="status-card" key={key}>
                        <div className="status-card-topline">
                            <span className="status-card-name">{status.name}</span>
                            <span className={`status-pill status-${status.state}`}>
                                <span aria-hidden="true" className="status-dot" />
                                {statusLabels[status.state] ?? status.state}
                            </span>
                        </div>

                        <div className="status-button-row">
                            <button
                                className="status-details-button"
                                type="button"
                                onClick={() => toggleDetails(key)}
                                aria-expanded={Boolean(openDetails[key])}
                            >
                                {openDetails[key] ? "Hide details" : "Details"}
                            </button>

                            {key === "database" && (
                                <button
                                    className="status-details-button"
                                    type="button"
                                    onClick={() => setIsManualFormOpen((isOpen) => !isOpen)}
                                    aria-expanded={isManualFormOpen}
                                >
                                    {isManualFormOpen ? "Hide manual" : "Manual connect"}
                                </button>
                            )}
                        </div>

                        {openDetails[key] && (
                            <div className="status-details">
                                {status.details.map((detail) => (
                                    <p key={detail}>{detail}</p>
                                ))}
                            </div>
                        )}

                        {key === "database" && isManualFormOpen && (
                            <form
                                className="manual-database-form"
                                onSubmit={submitManualConnection}
                            >
                                <label htmlFor="manual-database-connection">
                                    Connection string
                                </label>
                                <input
                                    id="manual-database-connection"
                                    type="password"
                                    value={manualConnectionString}
                                    onChange={(event) => setManualConnectionString(event.target.value)}
                                    placeholder="Server=...;Database=..."
                                    autoComplete="off"
                                />
                                <button
                                    className="manual-database-submit"
                                    type="submit"
                                    disabled={isRefreshing}
                                >
                                    {isRefreshing ? "Testing..." : "Test connection"}
                                </button>
                                <p className="manual-database-note">
                                    Demo only. This is not saved to source code or browser storage.
                                </p>
                                {manualConnectionMessage && (
                                    <p className="manual-database-message">
                                        {manualConnectionMessage}
                                    </p>
                                )}
                            </form>
                        )}
                    </article>
                ))}
            </div>

            <p className="last-checked">
                {lastChecked ? `Last checked ${lastChecked}` : "Status has not been checked yet"}
            </p>
        </section>
    );
}

export default ConnectionStatusDashboard;
