// Presentation layer / UI for staff returns processing.

import { useCallback, useEffect, useState } from "react";
import "./ReturnsQueueManager.css";

function ReturnsQueueManager({ currentUser }) {
    const [tasks, setTasks] = useState([]);
    const [currentTask, setCurrentTask] = useState(null);
    const [loading, setLoading] = useState(true);
    const [busy, setBusy] = useState(false);
    const [error, setError] = useState("");
    const [successMessage, setSuccessMessage] = useState("");
    const [showAnotherBookForm, setShowAnotherBookForm] = useState(false);
    const [showOtherForm, setShowOtherForm] = useState(false);
    const [foundBookId, setFoundBookId] = useState("");
    const [foundBookTitle, setFoundBookTitle] = useState("");
    const [foundBookShelfLocation, setFoundBookShelfLocation] = useState("");
    const [ticketDescription, setTicketDescription] = useState("");

    const loadPendingTasks = useCallback(async () => {
        setLoading(true);
        setError("");

        try {
            const response = await fetch("/api/returnsqueue/my-tasks", {
                credentials: "include",
            });
            const data = await response.json().catch(() => []);

            if (!response.ok) {
                throw new Error(data.message || "Failed to load pending return tasks.");
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
            const response = await fetch("/api/returnsqueue/claim-next", {
                method: "POST",
                credentials: "include",
            });
            const task = await response.json().catch(() => null);

            if (response.status === 204) {
                setSuccessMessage("There are no unassigned return tasks.");
                return;
            }

            if (!response.ok) {
                throw new Error(task?.message || "The next return task could not be claimed.");
            }

            setTasks((existingTasks) => [task, ...existingTasks]);
            setCurrentTask(task);
            setSuccessMessage("The next return task was assigned to you.");
        } catch (requestError) {
            setError(requestError.message);
        } finally {
            setBusy(false);
        }
    };

    const processAction = async (endpoint, body, successMessage) => {
        if (!currentTask) return;

        setBusy(true);
        setError("");
        setSuccessMessage("");

        try {
            const response = await fetch(`/api/returnsqueue/action/${endpoint}`, {
                method: "POST",
                credentials: "include",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ returnTaskId: currentTask.returnTaskId, ...body }),
            });
            const result = await response.json().catch(() => ({}));

            if (!response.ok || !result.success) {
                throw new Error(result.message || "The return action could not be completed.");
            }

            setSuccessMessage(result.message || successMessage);
            setShowAnotherBookForm(false);
            setShowOtherForm(false);
            setFoundBookId("");
            setFoundBookTitle("");
            setFoundBookShelfLocation("");
            setTicketDescription("");
            await loadPendingTasks();
        } catch (requestError) {
            setError(requestError.message);
        } finally {
            setBusy(false);
        }
    };

    const handleBadCondition = () => {
        if (window.confirm("Mark this copy as damaged and remove it from inventory?")) {
            processAction("bad-condition", {}, "The damaged copy was removed from inventory.");
        }
    };

    if (loading) {
        return <div className="returns-queue-container"><div className="loading">Loading returns queue...</div></div>;
    }

    return (
        <div className="returns-queue-container">
            <div className="returns-header">
                <h2>Returns Queue</h2>
                <p className="task-count">
                    {tasks.length} assigned task{tasks.length === 1 ? "" : "s"}
                    {currentUser?.firstName ? ` for ${currentUser.firstName}` : ""}
                </p>
            </div>

            <div className="queue-message" aria-live="polite">
                {error && <div className="alert alert-error">{error}</div>}
                {successMessage && <div className="alert alert-success">{successMessage}</div>}
            </div>

            {currentTask ? (
                <div className="task-container">
                    <div className="task-card">
                        <div className="task-details">
                            <div className="detail-row"><label>Book Title:</label><span className="book-title">{currentTask.bookTitle}</span></div>
                            <div className="detail-row"><label>Shelf Location:</label><span className="shelf-location">{currentTask.shelfLocation}</span></div>
                            <div className="detail-row"><label>Task ID:</label><span>#{currentTask.returnTaskId}</span></div>
                        </div>

                        <div className="actions-section">
                            <fieldset className="action-fieldset">
                                <legend>Process this return</legend>
                                <button className="action-btn complete-btn" type="button" onClick={() => processAction("complete", {}, "Return completed.")} disabled={busy}>Complete and restock</button>
                                <button className="action-btn another-book-btn" type="button" onClick={() => { setShowAnotherBookForm((visible) => !visible); setShowOtherForm(false); }} disabled={busy}>Another book was in this location</button>
                                <button className="action-btn bad-condition-btn" type="button" onClick={handleBadCondition} disabled={busy}>Bad condition</button>
                                <button className="action-btn other-btn" type="button" onClick={() => { setShowOtherForm((visible) => !visible); setShowAnotherBookForm(false); }} disabled={busy}>Other / create support ticket</button>
                            </fieldset>

                            {showAnotherBookForm && (
                                <form className="form-container" onSubmit={(event) => { event.preventDefault(); processAction("another-book", { foundBookId: Number(foundBookId), foundBookTitle, foundBookShelfLocation }, "New return task created."); }}>
                                    <div className="form-group"><label htmlFor="found-book-id">Found book ID</label><input id="found-book-id" type="number" min="1" required value={foundBookId} onChange={(event) => setFoundBookId(event.target.value)} disabled={busy} /></div>
                                    <div className="form-group"><label htmlFor="found-book-title">Found book title</label><input id="found-book-title" type="text" required value={foundBookTitle} onChange={(event) => setFoundBookTitle(event.target.value)} disabled={busy} /></div>
                                    <div className="form-group"><label htmlFor="found-book-location">Found book shelf location</label><input id="found-book-location" type="text" required value={foundBookShelfLocation} onChange={(event) => setFoundBookShelfLocation(event.target.value)} disabled={busy} /></div>
                                    <div className="form-actions"><button className="btn-primary" type="submit" disabled={busy}>Create task</button><button className="btn-secondary" type="button" onClick={() => setShowAnotherBookForm(false)} disabled={busy}>Cancel</button></div>
                                </form>
                            )}

                            {showOtherForm && (
                                <form className="form-container" onSubmit={(event) => { event.preventDefault(); processAction("other", { ticketDescription }, "Support ticket created."); }}>
                                    <div className="form-group"><label htmlFor="ticket-description">Issue description</label><textarea id="ticket-description" required value={ticketDescription} onChange={(event) => setTicketDescription(event.target.value)} rows="4" disabled={busy} /></div>
                                    <div className="form-actions"><button className="btn-primary" type="submit" disabled={busy}>Create support ticket</button><button className="btn-secondary" type="button" onClick={() => setShowOtherForm(false)} disabled={busy}>Cancel</button></div>
                                </form>
                            )}
                        </div>
                    </div>
                </div>
            ) : (
                <div className="no-tasks">
                    <p>No return task is currently assigned to you.</p>
                    <button onClick={claimNextTask} className="btn-refresh" type="button" disabled={busy}>Claim next available task</button>
                    <button onClick={loadPendingTasks} className="btn-refresh" type="button" disabled={busy}>Refresh</button>
                </div>
            )}
        </div>
    );
}

export default ReturnsQueueManager;
