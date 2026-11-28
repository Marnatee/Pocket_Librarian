// Presentation layer / UI for staff fulfillment queue processing.

import { useCallback, useEffect, useState } from "react";
import "./FulfillmentQueueManager.css";

function FulfillmentQueueManager() {
    const [tasks, setTasks] = useState([]);
    const [currentTask, setCurrentTask] = useState(null);
    const [loading, setLoading] = useState(true);
    const [busy, setBusy] = useState(false);
    const [error, setError] = useState("");
    const [successMessage, setSuccessMessage] = useState("");

    const loadPendingTasks = useCallback(async () => {
        setLoading(true);
        setError("");

        try {
            const response = await fetch("/api/fulfillmentqueue/my-tasks", {
                credentials: "include",
            });
            const data = await response.json().catch(() => []);

            if (!response.ok) {
                throw new Error(data.message || "Failed to load pending fulfillment tasks.");
            }

            setTasks(data);
            setCurrentTask(data[0] || null);
        } catch (requestError) {
            setError(requestError.message);
            setTasks([]);
            setCurrentTask(null);
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => {
        const initialLoad = window.setTimeout(() => {
            loadPendingTasks();
        }, 0);

        return () => window.clearTimeout(initialLoad);
    }, [loadPendingTasks]);

    const claimNextTask = async () => {
        setBusy(true);
        setError("");
        setSuccessMessage("");

        try {
            const response = await fetch("/api/fulfillmentqueue/claim-next", {
                method: "POST",
                credentials: "include",
            });
            const task = await response.json().catch(() => null);

            if (response.status === 204) {
                setSuccessMessage("There are no unassigned fulfillment tasks.");
                return;
            }

            if (!response.ok) {
                throw new Error(task?.message || "The next fulfillment task could not be claimed.");
            }

            setTasks((existingTasks) => [task, ...existingTasks]);
            setCurrentTask(task);
            setSuccessMessage("The next fulfillment task was assigned to you.");
        } catch (requestError) {
            setError(requestError.message);
        } finally {
            setBusy(false);
        }
    };

    const fulfillTask = async () => {
        if (!currentTask) return;

        setBusy(true);
        setError("");
        setSuccessMessage("");

        try {
            const response = await fetch(
                `/api/fulfillmentqueue/fulfill/${currentTask.fulfillmentTaskId}`,
                {
                    method: "POST",
                    credentials: "include",
                    headers: { "Content-Type": "application/json" },
                }
            );
            const result = await response.json().catch(() => ({}));

            if (!response.ok) {
                throw new Error(result.message || "The fulfillment task could not be marked as fulfilled.");
            }

            // Update the current task to reflect the changes
            setCurrentTask(result);

            // Reload tasks to refresh the UI
            await loadPendingTasks();

            setSuccessMessage("The fulfillment task has been marked as completed. The member has been notified.");
        } catch (requestError) {
            setError(requestError.message);
        } finally {
            setBusy(false);
        }
    };

    const selectTask = (task) => {
        setCurrentTask(task);
        setError("");
        setSuccessMessage("");
    };

    if (loading) {
        return (
            <div className="fulfillment-queue-container">
                <p>Loading fulfillment tasks...</p>
            </div>
        );
    }

    return (
        <div className="fulfillment-queue-container">
            <div className="queue-section">
                <h2>Fulfillment Queue Manager</h2>

                {error && <div className="alert alert-error">{error}</div>}
                {successMessage && <div className="alert alert-success">{successMessage}</div>}

                <div className="queue-controls">
                    <button
                        className="btn btn-primary"
                        onClick={claimNextTask}
                        disabled={busy}
                    >
                        {busy ? "Processing..." : "Claim Next Task"}
                    </button>
                </div>

                <div className="task-list-section">
                    <h3>Your Fulfillment Tasks</h3>
                    {tasks.length === 0 ? (
                        <p className="empty-message">You have no pending fulfillment tasks.</p>
                    ) : (
                        <ul className="task-list">
                            {tasks.map((task) => (
                                <li
                                    key={task.fulfillmentTaskId}
                                    className={`task-item ${
                                        currentTask?.fulfillmentTaskId === task.fulfillmentTaskId
                                            ? "active"
                                            : ""
                                    }`}
                                    onClick={() => selectTask(task)}
                                >
                                    <div className="task-item-header">
                                        <span className="task-book-title">{task.bookTitle}</span>
                                        <span className={`task-status status-${task.status.toLowerCase()}`}>
                                            {task.status}
                                        </span>
                                    </div>
                                    <div className="task-item-details">
                                        <span className="task-member-name">For: {task.memberName}</span>
                                        <span className="task-date">
                                            Created: {new Date(task.dateCreated).toLocaleDateString()}
                                        </span>
                                    </div>
                                </li>
                            ))}
                        </ul>
                    )}
                </div>

                {currentTask && (
                    <div className="task-detail-section">
                        <h3>Current Task Details</h3>
                        <div className="task-details-grid">
                            <div className="detail-row">
                                <span className="label">Book Title:</span>
                                <span className="value">{currentTask.bookTitle}</span>
                            </div>
                            <div className="detail-row">
                                <span className="label">Member Name:</span>
                                <span className="value">{currentTask.memberName}</span>
                            </div>
                            <div className="detail-row">
                                <span className="label">Status:</span>
                                <span className="value">{currentTask.status}</span>
                            </div>
                            <div className="detail-row">
                                <span className="label">Book ID:</span>
                                <span className="value">{currentTask.bookId}</span>
                            </div>
                            <div className="detail-row">
                                <span className="label">Created:</span>
                                <span className="value">
                                    {new Date(currentTask.dateCreated).toLocaleString()}
                                </span>
                            </div>
                            {currentTask.dateAssigned && (
                                <div className="detail-row">
                                    <span className="label">Assigned:</span>
                                    <span className="value">
                                        {new Date(currentTask.dateAssigned).toLocaleString()}
                                    </span>
                                </div>
                            )}
                        </div>

                        <div className="task-actions">
                            <button
                                className="btn btn-success"
                                onClick={fulfillTask}
                                disabled={busy || currentTask.status === "Completed"}
                            >
                                {busy
                                    ? "Processing..."
                                    : currentTask.status === "Completed"
                                    ? "Task Completed"
                                    : "Mark ready for pickup"}
                            </button>
                            <p className="action-hint">
                                Click "Mark ready for pickup" when you have found the book and set it aside for pickup.
                                The member will be notified to collect their book.
                            </p>
                        </div>
                    </div>
                )}
            </div>
        </div>
    );
}

export default FulfillmentQueueManager;
