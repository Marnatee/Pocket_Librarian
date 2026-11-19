// Presentation layer / UI
// Returns queue manager for staff to process returned books

import React, { useState, useEffect } from "react";
import "./ReturnsQueueManager.css";

const ReturnsQueueManager = ({ staffId }) => {
  const [tasks, setTasks] = useState([]);
  const [currentTask, setCurrentTask] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [actionInProgress, setActionInProgress] = useState(false);

  // Modal/Form states
  const [showAnotherBookForm, setShowAnotherBookForm] = useState(false);
  const [showOtherForm, setShowOtherForm] = useState(false);
  const [foundBookTitle, setFoundBookTitle] = useState("");
  const [foundBookShelfLocation, setFoundBookShelfLocation] = useState("");
  const [ticketDescription, setTicketDescription] = useState("");

  const [successMessage, setSuccessMessage] = useState("");

  // Load pending tasks on component mount
  useEffect(() => {
    if (staffId) {
      loadPendingTasks();
    }
  }, [staffId]);

  const loadPendingTasks = async () => {
    setLoading(true);
    setError("");
    try {
      const response = await fetch(`/api/returnsqueue/my-tasks/${staffId}`);
      if (!response.ok) {
        throw new Error("Failed to load pending tasks");
      }
      const data = await response.json();
      setTasks(data);
      if (data.length > 0) {
        setCurrentTask(data[0]);
      } else {
        setCurrentTask(null);
      }
    } catch (err) {
      setError(err.message);
      setTasks([]);
      setCurrentTask(null);
    } finally {
      setLoading(false);
    }
  };

  const handleCompleteAction = async () => {
    if (!currentTask) return;

    setActionInProgress(true);
    setError("");
    setSuccessMessage("");

    try {
      const response = await fetch("/api/returnsqueue/action/complete", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          returnTaskId: currentTask.returnTaskId,
          staffId: staffId,
        }),
      });

      if (!response.ok) {
        throw new Error("Failed to process action");
      }

      const result = await response.json();

      if (result.success) {
        setSuccessMessage(result.message);
        // Remove completed task and load the next one
        const updatedTasks = tasks.filter(
          (t) => t.returnTaskId !== currentTask.returnTaskId
        );
        setTasks(updatedTasks);
        setCurrentTask(result.newTask || (updatedTasks.length > 0 ? updatedTasks[0] : null));

        // Clear success message after 3 seconds
        setTimeout(() => setSuccessMessage(""), 3000);
      } else {
        setError(result.message);
      }
    } catch (err) {
      setError("Error processing action: " + err.message);
    } finally {
      setActionInProgress(false);
    }
  };

  const handleAnotherBookSubmit = async () => {
    if (!currentTask || !foundBookTitle.trim() || !foundBookShelfLocation.trim()) {
      setError("Please enter both book title and shelf location");
      return;
    }

    setActionInProgress(true);
    setError("");
    setSuccessMessage("");

    try {
      const response = await fetch("/api/returnsqueue/action/another-book", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          returnTaskId: currentTask.returnTaskId,
          staffId: staffId,
          foundBookTitle: foundBookTitle,
          foundBookShelfLocation: foundBookShelfLocation,
        }),
      });

      if (!response.ok) {
        throw new Error("Failed to process action");
      }

      const result = await response.json();

      if (result.success) {
        setSuccessMessage(result.message);
        // Remove completed task and add the new task
        const updatedTasks = tasks.filter(
          (t) => t.returnTaskId !== currentTask.returnTaskId
        );
        if (result.newTask) {
          updatedTasks.push(result.newTask);
        }
        setTasks(updatedTasks);
        setCurrentTask(result.newTask || (updatedTasks.length > 0 ? updatedTasks[0] : null));

        // Reset form
        setShowAnotherBookForm(false);
        setFoundBookTitle("");
        setFoundBookShelfLocation("");

        setTimeout(() => setSuccessMessage(""), 3000);
      } else {
        setError(result.message);
      }
    } catch (err) {
      setError("Error processing action: " + err.message);
    } finally {
      setActionInProgress(false);
    }
  };

  const handleBadConditionAction = async () => {
    if (!currentTask) return;

    setActionInProgress(true);
    setError("");
    setSuccessMessage("");

    try {
      const response = await fetch("/api/returnsqueue/action/bad-condition", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          returnTaskId: currentTask.returnTaskId,
          staffId: staffId,
        }),
      });

      if (!response.ok) {
        throw new Error("Failed to process action");
      }

      const result = await response.json();

      if (result.success) {
        setSuccessMessage(result.message);
        // Remove completed task
        const updatedTasks = tasks.filter(
          (t) => t.returnTaskId !== currentTask.returnTaskId
        );
        setTasks(updatedTasks);
        setCurrentTask(updatedTasks.length > 0 ? updatedTasks[0] : null);

        setTimeout(() => setSuccessMessage(""), 3000);
      } else {
        setError(result.message);
      }
    } catch (err) {
      setError("Error processing action: " + err.message);
    } finally {
      setActionInProgress(false);
    }
  };

  const handleOtherSubmit = async () => {
    if (!currentTask || !ticketDescription.trim()) {
      setError("Please enter a description for the support ticket");
      return;
    }

    setActionInProgress(true);
    setError("");
    setSuccessMessage("");

    try {
      const response = await fetch("/api/returnsqueue/action/other", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          returnTaskId: currentTask.returnTaskId,
          staffId: staffId,
          ticketDescription: ticketDescription,
        }),
      });

      if (!response.ok) {
        throw new Error("Failed to process action");
      }

      const result = await response.json();

      if (result.success) {
        setSuccessMessage(result.message);
        // Remove completed task
        const updatedTasks = tasks.filter(
          (t) => t.returnTaskId !== currentTask.returnTaskId
        );
        setTasks(updatedTasks);
        setCurrentTask(updatedTasks.length > 0 ? updatedTasks[0] : null);

        // Reset form
        setShowOtherForm(false);
        setTicketDescription("");

        setTimeout(() => setSuccessMessage(""), 3000);
      } else {
        setError(result.message);
      }
    } catch (err) {
      setError("Error processing action: " + err.message);
    } finally {
      setActionInProgress(false);
    }
  };

  if (loading) {
    return (
      <div className="returns-queue-container">
        <div className="loading">Loading returns queue...</div>
      </div>
    );
  }

  return (
    <div className="returns-queue-container">
      <div className="returns-header">
        <h2>Returns Queue</h2>
        <p className="task-count">
          {tasks.length} pending task{tasks.length !== 1 ? "s" : ""}
        </p>
      </div>

      {error && <div className="alert alert-error">{error}</div>}
      {successMessage && <div className="alert alert-success">{successMessage}</div>}

      {currentTask ? (
        <div className="task-container">
          <div className="task-card">
            <div className="task-details">
              <div className="detail-row">
                <label>Book Title:</label>
                <span className="book-title">{currentTask.bookTitle}</span>
              </div>
              <div className="detail-row">
                <label>Shelf Location:</label>
                <span className="shelf-location">{currentTask.shelfLocation}</span>
              </div>
              <div className="detail-row">
                <label>Task ID:</label>
                <span>#{currentTask.returnTaskId}</span>
              </div>
            </div>

            <div className="actions-section">
              <fieldset className="action-fieldset">
                <legend>What do you want to do?</legend>

                <button
                  className="action-btn complete-btn"
                  onClick={handleCompleteAction}
                  disabled={actionInProgress}
                >
                  ✓ Complete
                </button>

                <button
                  className="action-btn another-book-btn"
                  onClick={() => {
                    setShowAnotherBookForm(!showAnotherBookForm);
                    setShowOtherForm(false);
                  }}
                  disabled={actionInProgress}
                >
                  📚 Another book was in this location
                </button>

                <button
                  className="action-btn bad-condition-btn"
                  onClick={handleBadConditionAction}
                  disabled={actionInProgress}
                >
                  ⚠️ Bad Condition
                </button>

                <button
                  className="action-btn other-btn"
                  onClick={() => {
                    setShowOtherForm(!showOtherForm);
                    setShowAnotherBookForm(false);
                  }}
                  disabled={actionInProgress}
                >
                  🎫 Other
                </button>
              </fieldset>

              {showAnotherBookForm && (
                <div className="form-container">
                  <div className="form-group">
                    <label htmlFor="found-book-title">Book Title</label>
                    <input
                      id="found-book-title"
                      type="text"
                      placeholder="Enter the title of the book found in this location"
                      value={foundBookTitle}
                      onChange={(e) => setFoundBookTitle(e.target.value)}
                      disabled={actionInProgress}
                    />
                  </div>
                  <div className="form-group">
                    <label htmlFor="found-book-location">Shelf Location</label>
                    <input
                      id="found-book-location"
                      type="text"
                      placeholder="Enter the shelf location for this book"
                      value={foundBookShelfLocation}
                      onChange={(e) => setFoundBookShelfLocation(e.target.value)}
                      disabled={actionInProgress}
                    />
                  </div>
                  <div className="form-actions">
                    <button
                      className="btn-primary"
                      onClick={handleAnotherBookSubmit}
                      disabled={actionInProgress}
                    >
                      Create New Task
                    </button>
                    <button
                      className="btn-secondary"
                      onClick={() => {
                        setShowAnotherBookForm(false);
                        setFoundBookTitle("");
                        setFoundBookShelfLocation("");
                      }}
                      disabled={actionInProgress}
                    >
                      Cancel
                    </button>
                  </div>
                </div>
              )}

              {showOtherForm && (
                <div className="form-container">
                  <div className="form-group">
                    <label htmlFor="ticket-description">Issue Description</label>
                    <textarea
                      id="ticket-description"
                      placeholder="Describe the issue requiring support..."
                      value={ticketDescription}
                      onChange={(e) => setTicketDescription(e.target.value)}
                      rows="4"
                      disabled={actionInProgress}
                    />
                  </div>
                  <div className="form-actions">
                    <button
                      className="btn-primary"
                      onClick={handleOtherSubmit}
                      disabled={actionInProgress}
                    >
                      Create Support Ticket
                    </button>
                    <button
                      className="btn-secondary"
                      onClick={() => {
                        setShowOtherForm(false);
                        setTicketDescription("");
                      }}
                      disabled={actionInProgress}
                    >
                      Cancel
                    </button>
                  </div>
                </div>
              )}
            </div>
          </div>
        </div>
      ) : (
        <div className="no-tasks">
          <p>🎉 No pending return tasks!</p>
          <button onClick={loadPendingTasks} className="btn-refresh">
            Refresh
          </button>
        </div>
      )}
    </div>
  );
};

export default ReturnsQueueManager;
