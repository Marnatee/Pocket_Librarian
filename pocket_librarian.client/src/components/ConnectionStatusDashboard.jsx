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
}) {
    const [openDetails, setOpenDetails] = useState({});

    function toggleDetails(key) {
        setOpenDetails((currentDetails) => ({
            ...currentDetails,
            [key]: !currentDetails[key],
        }));
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

                        <button
                            className="status-details-button"
                            type="button"
                            onClick={() => toggleDetails(key)}
                            aria-expanded={Boolean(openDetails[key])}
                        >
                            {openDetails[key] ? "Hide details" : "Details"}
                        </button>

                        {openDetails[key] && (
                            <div className="status-details">
                                {status.details.map((detail) => (
                                    <p key={detail}>{detail}</p>
                                ))}
                            </div>
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
