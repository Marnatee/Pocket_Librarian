//Presentation layer / UI
//Displays the persistent banner and the current Pocket Librarian page.
//--Currently-- Calls the backend API to display frontend/backend/database status.

import { useCallback, useEffect, useState } from "react";
import "./App.css";
import AppBanner from "./components/AppBanner";
import HomePage from "./pages/HomePage";
import MemberPage from "./pages/MemberPage";
import StaffPage from "./pages/StaffPage";

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
            "Endpoint: /api/pipeline/test",
            "API URL: https://localhost:70•••",
        ],
    },
    database: {
        name: "Database",
        state: "checking",
        details: [
            "Database target is masked for safety.",
            "Source: Jack's database pipeline configuration.",
        ],
    },
};

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
            },
            database: {
                ...currentStatuses.database,
                state: "checking",
            },
        }));

        const abortController = new AbortController();
        const timeoutId = window.setTimeout(() => abortController.abort(), 5000);

        try {
            // Send a GET request to the ASP.NET Core controller.
            const response = await fetch("/api/pipeline/test", {
                signal: abortController.signal,
            });

            // Treat non-successful HTTP responses as backend errors.
            if (!response.ok) {
                throw new Error(`The server returned HTTP ${response.status}`);
            }

            // Convert the JSON response into a JavaScript object.
            const data = await response.json();
            const databaseConnected = data.databaseConnected === true;

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
                    state: "online",
                    details: [
                        "API responded to /api/pipeline/test.",
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
            });
        } catch (error) {
            const message = error.name === "AbortError"
                ? "The API health check timed out."
                : "The API server is unavailable or not running.";

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
                        "Database status is unavailable because the API did not respond.",
                        "Database target is masked for safety.",
                    ],
                },
            });
        } finally {
            window.clearTimeout(timeoutId);
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
            />
            <main className="page-container">{renderCurrentPage()}</main>
        </div>
    );
}

export default App;
