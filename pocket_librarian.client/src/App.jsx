// Presentation layer / UI
// Displays the persistent banner and the current Pocket Librarian page.
// Backend health and database health are checked through separate API routes.

import { useCallback, useEffect, useState } from "react";
import "./App.css";
import AppBanner from "./components/AppBanner";
import HomePage from "./pages/HomePage";
import MemberPage from "./pages/MemberPage";
import StaffPage from "./pages/StaffPage";

const BACKEND_HEALTH_TIMEOUT_MS = 3000;
const DATABASE_CHECK_TIMEOUT_MS = 5000;

const initialStatuses = {
    frontend: {
        name: "Frontend",
        state: "online",
        details: [
            "React/Vite client is running.",
            "Client URL: https://localhost:62•••",
        ],
    },
    backend: {
        name: "Backend",
        state: "checking",
        details: [
            "Endpoint: /api/health",
            "API URL: https://localhost:70•••",
        ],
    },
    database: {
        name: "Database",
        state: "checking",
        details: [
            "Endpoint: /api/pipeline/database-status",
            "Target may be local SQL Server or Azure SQL.",
        ],
    },
};

async function fetchWithTimeout(url, options = {}, timeoutMs) {
    const abortController = new AbortController();
    const timeoutId = window.setTimeout(
        () => abortController.abort(),
        timeoutMs
    );

    try {
        return await fetch(url, {
            ...options,
            signal: abortController.signal,
        });
    } finally {
        window.clearTimeout(timeoutId);
    }
}

function App() {
    const [currentPage, setCurrentPage] = useState("home");
    const [statuses, setStatuses] = useState(initialStatuses);
    const [isRefreshing, setIsRefreshing] = useState(false);
    const [lastChecked, setLastChecked] = useState("");

    const refreshConnectionStatus = useCallback(async () => {
        setIsRefreshing(true);
        setStatuses((currentStatuses) => ({
            ...currentStatuses,
            frontend: {
                ...currentStatuses.frontend,
                state: "online",
            },
            backend: {
                ...currentStatuses.backend,
                state: "checking",
                details: [
                    "Checking the local ASP.NET Core backend.",
                    "Endpoint: /api/health",
                ],
            },
            database: {
                ...currentStatuses.database,
                state: "checking",
                details: [
                    "Waiting for the independent database check.",
                    "Endpoint: /api/pipeline/database-status",
                ],
            },
        }));

        try {
            // This endpoint does not call the business or data-access layers.
            const backendResponse = await fetchWithTimeout(
                "/api/health",
                {},
                BACKEND_HEALTH_TIMEOUT_MS
            );

            if (!backendResponse.ok) {
                throw new Error(`Local API responded with HTTP ${backendResponse.status}.`);
            }

            // Set Backend online as soon as ASP.NET responds, before testing SQL.
            setStatuses((currentStatuses) => ({
                ...currentStatuses,
                frontend: {
                    name: "Frontend",
                    state: "online",
                    details: [
                        "React/Vite client is running.",
                        "Client URL: https://localhost:62•••",
                    ],
                },
                backend: {
                    name: "Backend",
                    state: "online",
                    details: [
                        "Local ASP.NET Core backend responded independently.",
                        "API URL: https://localhost:70•••",
                    ],
                },
            }));

            try {
                // Database status is intentionally checked on a separate route.
                const databaseResponse = await fetchWithTimeout(
                    "/api/pipeline/database-status",
                    {},
                    DATABASE_CHECK_TIMEOUT_MS
                );

                if (!databaseResponse.ok) {
                    throw new Error(`Database status returned HTTP ${databaseResponse.status}.`);
                }

                const data = await databaseResponse.json();
                const databaseConnected = data.databaseConnected === true;

                setStatuses((currentStatuses) => ({
                    ...currentStatuses,
                    database: {
                        name: "Database",
                        state: databaseConnected ? "online" : "offline",
                        details: [
                            data.databaseDetails || "Database target is masked for safety.",
                            data.databaseMessage || "No database message was returned.",
                        ],
                    },
                }));
            } catch (error) {
                const databaseMessage = error.name === "AbortError"
                    ? "The database check timed out after a short probe period."
                    : error.message;

                // A database failure must not change the already-confirmed backend state.
                setStatuses((currentStatuses) => ({
                    ...currentStatuses,
                    database: {
                        name: "Database",
                        state: "offline",
                        details: [
                            databaseMessage || "The database could not be reached.",
                            "The ASP.NET backend is still online.",
                        ],
                    },
                }));
            }
        } catch (error) {
            const message = error.name === "AbortError"
                ? "The backend health check timed out."
                : error.message || "The API server is unavailable or not running.";

            setStatuses({
                frontend: {
                    name: "Frontend",
                    state: "online",
                    details: [
                        "React/Vite client is running.",
                        "Client URL: https://localhost:62•••",
                    ],
                },
                backend: {
                    name: "Backend",
                    state: "offline",
                    details: [
                        message,
                        "API URL: https://localhost:70•••",
                    ],
                },
                database: {
                    name: "Database",
                    state: "offline",
                    details: [
                        "Database status was not checked because the backend did not respond.",
                        "Database target may be local SQL Server or Azure SQL.",
                    ],
                },
            });
        } finally {
            setLastChecked(new Date().toLocaleTimeString());
            setIsRefreshing(false);
        }
    }, []);

    const connectManualDatabase = useCallback(async (connectionString) => {
        setIsRefreshing(true);
        setStatuses((currentStatuses) => ({
            ...currentStatuses,
            database: {
                ...currentStatuses.database,
                state: "checking",
                details: [
                    "Testing the manually supplied database connection.",
                    "The connection string is not saved by the frontend.",
                ],
            },
        }));

        try {
            const response = await fetchWithTimeout(
                "/api/pipeline/database/connect",
                {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json",
                    },
                    body: JSON.stringify({ connectionString }),
                },
                DATABASE_CHECK_TIMEOUT_MS
            );

            if (!response.ok) {
                throw new Error(`The database request returned HTTP ${response.status}.`);
            }

            const data = await response.json();
            const databaseConnected = data.databaseConnected === true;

            setStatuses((currentStatuses) => ({
                ...currentStatuses,
                backend: {
                    name: "Backend",
                    state: "online",
                    details: [
                        "Local ASP.NET Core backend responded to the database request.",
                        "API URL: https://localhost:70•••",
                    ],
                },
                database: {
                    name: "Database",
                    state: databaseConnected ? "online" : "offline",
                    details: [
                        data.databaseDetails || "Database target is masked for safety.",
                        data.databaseMessage || "No database message was returned.",
                    ],
                },
            }));

            return databaseConnected;
        } catch (error) {
            // A failed manual database test does not prove that the backend is offline.
            setStatuses((currentStatuses) => ({
                ...currentStatuses,
                database: {
                    name: "Database",
                    state: "offline",
                    details: [
                        error.name === "AbortError"
                            ? "The database check timed out after a short probe period."
                            : error.message,
                        "The backend status is checked separately.",
                    ],
                },
            }));

            return false;
        } finally {
            setLastChecked(new Date().toLocaleTimeString());
            setIsRefreshing(false);
        }
    }, []);

    useEffect(() => {
        const initialCheck = window.setTimeout(() => {
            refreshConnectionStatus();
        }, 0);

        return () => window.clearTimeout(initialCheck);
    }, [refreshConnectionStatus]);

    function renderCurrentPage() {
        switch (currentPage) {
            case "member":
                return <MemberPage />;
            case "staff":
                return <StaffPage />;
            case "home":
            default:
                return <HomePage />;
        }
    }

    return (
        <div className="app-shell">
            <AppBanner
                currentPage={currentPage}
                onPageChange={setCurrentPage}
                statuses={statuses}
                isRefreshing={isRefreshing}
                lastChecked={lastChecked}
                onRefresh={refreshConnectionStatus}
                onManualDatabaseConnect={connectManualDatabase}
            />
            <main className="page-container">{renderCurrentPage()}</main>
        </div>
    );
}

export default App;
