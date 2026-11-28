// Reservation Manager Component
// Displays user's current and past reservations
// Allows users to reserve and return books

import { useCallback, useEffect, useState } from "react";
import "./ReservationManager.css";

const RESERVATION_API_BASE = "/api/reservation";

const MILLISECONDS_PER_DAY = 1000 * 60 * 60 * 24;

function elapsedWholeDays(startDate, endDate = new Date()) {
    const startTime = new Date(startDate).getTime();
    const endTime = new Date(endDate).getTime();

    if (!Number.isFinite(startTime) || !Number.isFinite(endTime)) {
        return 0;
    }

    // A clock/time-zone mismatch should never show a negative duration to a member.
    return Math.max(0, Math.floor((endTime - startTime) / MILLISECONDS_PER_DAY));
}

function ReservationManager({ userId, children }) {
    const [reservations, setReservations] = useState([]);
    const [activeReservations, setActiveReservations] = useState([]);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState(null);
    const [success, setSuccess] = useState(null);
    const [showCheckoutForm, setShowCheckoutForm] = useState(false);
    const [bookIdInput, setBookIdInput] = useState("");
    const [clearingHistory, setClearingHistory] = useState(false);

    // Fetch user's active reservations
    const fetchActiveReservations = useCallback(async () => {
        setLoading(true);
        setError(null);
        try {
            const response = await fetch(
                `${RESERVATION_API_BASE}/user/${userId}/active`
            );
            if (!response.ok) {
                throw new Error("Failed to fetch active reservations");
            }
            const data = await response.json();
            setActiveReservations(data || []);
        } catch (err) {
            setError(err.message);
            console.error("Error fetching active reservations:", err);
        } finally {
            setLoading(false);
        }
    }, [userId]);

    // Fetch all user reservations (including returned)
    const fetchAllReservations = useCallback(async () => {
        setLoading(true);
        setError(null);
        try {
            const response = await fetch(
                `${RESERVATION_API_BASE}/user/${userId}`
            );
            if (!response.ok) {
                throw new Error("Failed to fetch reservations");
            }
            const data = await response.json();
            setReservations(data || []);
        } catch (err) {
            setError(err.message);
            console.error("Error fetching reservations:", err);
        } finally {
            setLoading(false);
        }
    }, [userId]);

    // Create a new reservation request for staff fulfillment.
    const handleCheckoutBook = async (e) => {
        e.preventDefault();
        if (!bookIdInput || bookIdInput <= 0) {
            setError("Please enter a valid Book ID");
            return;
        }

        setLoading(true);
        setError(null);
        setSuccess(null);

        try {
            const response = await fetch(
                `${RESERVATION_API_BASE}/create`,
                {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json",
                    },
                    body: JSON.stringify({
                        bookId: parseInt(bookIdInput),
                    }),
                }
            );

            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(
                    errorData.error || "Failed to create reservation"
                );
            }

            const data = await response.json();
            setSuccess(
                `Reservation submitted successfully. Staff will prepare the book for pickup. Reservation ID: ${data.reservationId}`
            );
            setBookIdInput("");
            setShowCheckoutForm(false);

            // Refresh active reservations
            await fetchActiveReservations();
            await fetchAllReservations();
        } catch (err) {
            setError(err.message);
            console.error("Error checking out book:", err);
        } finally {
            setLoading(false);
        }
    };

    // Submit a return request for a checked-out book.
    const handleReturnBook = async (reservationId) => {
        setLoading(true);
        setError(null);
        setSuccess(null);

        try {
            const response = await fetch(
                `${RESERVATION_API_BASE}/${reservationId}/return`,
                {
                    method: "PUT",
                    headers: {
                        "Content-Type": "application/json",
                    },
                }
            );

            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(
                    errorData.error || "Failed to return book"
                );
            }

            setSuccess("Return request submitted. Staff will process the physical return.");

            // Refresh reservations
            await fetchActiveReservations();
            await fetchAllReservations();
        } catch (err) {
            setError(err.message);
            console.error("Error returning book:", err);
        } finally {
            setLoading(false);
        }
    };

    const handleCheckoutReservation = async (reservationId) => {
        setLoading(true);
        setError(null);
        setSuccess(null);

        try {
            const response = await fetch(
                `${RESERVATION_API_BASE}/${reservationId}/checkout`,
                { method: "PUT", headers: { "Content-Type": "application/json" } }
            );
            const result = await response.json().catch(() => ({}));
            if (!response.ok) {
                throw new Error(result.error || "Failed to check out the book");
            }

            setSuccess("Book checked out successfully.");
            await fetchActiveReservations();
            await fetchAllReservations();
        } catch (err) {
            setError(err.message);
            console.error("Error checking out reservation:", err);
        } finally {
            setLoading(false);
        }
    };

    const handleCancelReservation = async (reservationId) => {
        if (!window.confirm("Cancel this reservation? Its cancellation record will be preserved.")) {
            return;
        }

        setLoading(true);
        setError(null);
        setSuccess(null);

        try {
            const response = await fetch(
                `${RESERVATION_API_BASE}/${reservationId}/cancel`,
                { method: "PUT", headers: { "Content-Type": "application/json" } }
            );
            const result = await response.json().catch(() => ({}));
            if (!response.ok) {
                throw new Error(result.error || "Failed to cancel the reservation");
            }

            setSuccess("Reservation cancelled. The cancellation was preserved in your history.");
            await fetchActiveReservations();
            await fetchAllReservations();
        } catch (err) {
            setError(err.message);
            console.error("Error cancelling reservation:", err);
        } finally {
            setLoading(false);
        }
    };

    const returnedReservations = reservations.filter((r) => r.dateReturned !== null);
    const cancelledReservations = reservations.filter(
        (r) => r.dateReturned === null && r.reservationStatus === "Cancelled"
    );
    const pastReservations = [...returnedReservations, ...cancelledReservations];
    const pendingReservations = reservations.filter(
        (r) => r.dateReturned === null && r.reservationStatus === "Pending"
    );
    const readyReservations = activeReservations.filter(
        (r) => r.reservationStatus === "ReadyForPickup"
    );
    const checkedOutReservations = activeReservations.filter(
        (r) => r.reservationStatus === "CheckedOut"
    );
    const returnPendingReservations = reservations.filter(
        (r) => r.dateReturned === null && r.reservationStatus === "ReturnPending"
    );

    const clearReservationHistory = async () => {
        if (returnedReservations.length === 0 || clearingHistory) {
            return;
        }

        if (!window.confirm("Clear all returned reservations from your history? This cannot be undone.")) {
            return;
        }

        setClearingHistory(true);
        setError(null);
        setSuccess(null);

        try {
            const response = await fetch(
                `${RESERVATION_API_BASE}/user/${userId}/history`,
                {
                    method: "DELETE",
                    credentials: "include",
                }
            );
            const result = await response.json().catch(() => ({}));

            if (!response.ok) {
                throw new Error(result.error || "Failed to clear reservation history");
            }

            setReservations((currentReservations) =>
                currentReservations.filter((reservation) => reservation.dateReturned === null)
            );
            setSuccess(
                `${result.deletedCount ?? returnedReservations.length} reservation${
                    (result.deletedCount ?? returnedReservations.length) === 1 ? "" : "s"
                } removed from your history.`
            );
        } catch (err) {
            setError(err.message);
            console.error("Error clearing reservation history:", err);
        } finally {
            setClearingHistory(false);
        }
    };

    // Load reservations on mount
    useEffect(() => {
        const loadReservations = window.setTimeout(() => {
            void fetchActiveReservations();
            void fetchAllReservations();
        }, 0);

        return () => window.clearTimeout(loadReservations);
    }, [fetchActiveReservations, fetchAllReservations]);

    return (
        <div className="reservation-manager">
            <div className="reservation-header">
                <h2>My Reservations</h2>
                <p className="user-id-info">User ID: {userId}</p>
            </div>

            {/* Messages */}
            {error && (
                <div className="alert alert-error" role="alert">
                    {error}
                </div>
            )}
            {success && (
                <div className="alert alert-success" role="alert">
                    {success}
                </div>
            )}

            {pendingReservations.length > 0 && (
                <div className="reservations-section">
                    <h3>Awaiting Staff Fulfillment ({pendingReservations.length})</h3>
                    <div className="reservations-list">
                        {pendingReservations.map((reservation) => (
                            <div
                                key={reservation.reservationId}
                                className="reservation-card"
                            >
                                <div className="reservation-info">
                                    <div className="info-row">
                                        <span className="label">Book:</span>
                                        <span className="value">
                                            {reservation.bookTitle || `Book ${reservation.bookId}`}
                                        </span>
                                    </div>
                                    <div className="info-row">
                                        <span className="label">Reservation ID:</span>
                                        <span className="value">{reservation.reservationId}</span>
                                    </div>
                                    <div className="info-row">
                                        <span className="label">Status:</span>
                                        <span className="value">Waiting for staff to prepare the book</span>
                                    </div>
                                </div>
                                <button
                                    className="btn btn-secondary"
                                    onClick={() => handleCancelReservation(reservation.reservationId)}
                                    disabled={loading}
                                >
                                    Cancel reservation
                                </button>
                            </div>
                        ))}
                    </div>
                </div>
            )}

            {readyReservations.length > 0 && (
                <div className="reservations-section">
                    <h3>Ready for Pickup ({readyReservations.length})</h3>
                    <div className="reservations-list">
                        {readyReservations.map((reservation) => (
                            <div key={reservation.reservationId} className="reservation-card">
                                <div className="reservation-info">
                                    <div className="info-row">
                                        <span className="label">Book:</span>
                                        <span className="value">
                                            {reservation.bookTitle || `Book ${reservation.bookId}`}
                                        </span>
                                    </div>
                                    <div className="info-row">
                                        <span className="label">Reservation ID:</span>
                                        <span className="value">{reservation.reservationId}</span>
                                    </div>
                                    <div className="info-row">
                                        <span className="label">Status:</span>
                                        <span className="value">Staff has located this book for pickup</span>
                                    </div>
                                </div>
                                <div className="form-actions">
                                    <button
                                        className="btn btn-primary"
                                        onClick={() => handleCheckoutReservation(reservation.reservationId)}
                                        disabled={loading}
                                    >
                                        Check out book
                                    </button>
                                    <button
                                        className="btn btn-secondary"
                                        onClick={() => handleCancelReservation(reservation.reservationId)}
                                        disabled={loading}
                                    >
                                        Cancel reservation
                                    </button>
                                </div>
                            </div>
                        ))}
                    </div>
                </div>
            )}

            {/* Checkout Book Section */}
            <div className="checkout-section">
                {!showCheckoutForm ? (
                    <button
                        onClick={() => setShowCheckoutForm(true)}
                        className="btn btn-primary"
                        disabled={loading}
                    >
                        Reserve Book
                    </button>
                ) : (
                    <form
                        onSubmit={handleCheckoutBook}
                        className="checkout-form"
                    >
                        <div className="form-group">
                            <label htmlFor="book-id-input">Book ID:</label>
                            <input
                                id="book-id-input"
                                type="number"
                                value={bookIdInput}
                                onChange={(e) => setBookIdInput(e.target.value)}
                                placeholder="Enter Book ID"
                                min="1"
                                disabled={loading}
                            />
                        </div>
                        <div className="form-actions">
                            <button
                                type="submit"
                                className="btn btn-primary"
                                disabled={loading}
                            >
                                {loading ? "Processing..." : "Reserve"}
                            </button>
                            <button
                                type="button"
                                className="btn btn-secondary"
                                onClick={() => {
                                    setShowCheckoutForm(false);
                                    setBookIdInput("");
                                }}
                                disabled={loading}
                            >
                                Cancel
                            </button>
                        </div>
                    </form>
                )}
            </div>

            {/* Active Reservations Section */}
            <div className="reservations-section">
                <h3>Currently Checked Out ({checkedOutReservations.length})</h3>
                {loading && checkedOutReservations.length === 0 ? (
                    <p className="loading-text">Loading...</p>
                ) : checkedOutReservations.length === 0 ? (
                    <p className="empty-message">
                        You have no books currently checked out.
                    </p>
                ) : (
                    <div className="reservations-list">
                        {checkedOutReservations.map((reservation) => {
                            const daysCheckedOut = elapsedWholeDays(reservation.dateReserved);
                            return (
                                <div
                                    key={reservation.reservationId}
                                    className="reservation-card"
                                >
                                    <div className="reservation-info">
                                        <div className="info-row">
                                            <span className="label">
                                                User:
                                            </span>
                                            <span className="value">
                                                {reservation.userName ||
                                                    `User ${reservation.userId}`}
                                            </span>
                                        </div>
                                        <div className="info-row">
                                            <span className="label">
                                                Book:
                                            </span>
                                            <span className="value">
                                                {reservation.bookTitle ||
                                                    `Book ${reservation.bookId}`}
                                            </span>
                                        </div>
                                        <div className="info-row">
                                            <span className="label">
                                                Reservation ID:
                                            </span>
                                            <span className="value">
                                                {reservation.reservationId}
                                            </span>
                                        </div>
                                        <div className="info-row">
                                            <span className="label">
                                                Book ID:
                                            </span>
                                            <span className="value">
                                                {reservation.bookId}
                                            </span>
                                        </div>
                                        <div className="info-row">
                                            <span className="label">
                                                Date Checked Out:
                                            </span>
                                            <span className="value">
                                                {new Date(
                                                    reservation.dateReserved
                                                ).toLocaleDateString()}
                                            </span>
                                        </div>
                                        <div className="info-row">
                                            <span className="label">
                                                Days Checked Out:
                                            </span>
                                            <span className="value">
                                                {daysCheckedOut}
                                            </span>
                                        </div>
                                    </div>
                                    <button
                                        className="btn btn-danger"
                                        onClick={() =>
                                            handleReturnBook(
                                                reservation.reservationId
                                            )
                                        }
                                        disabled={loading}
                                    >
                                        Return Book
                                    </button>
                                </div>
                            );
                        })}
                    </div>
                )}
            </div>

            {returnPendingReservations.length > 0 && (
                <div className="reservations-section">
                    <h3>Return Pending ({returnPendingReservations.length})</h3>
                    <div className="reservations-list">
                        {returnPendingReservations.map((reservation) => (
                            <div key={reservation.reservationId} className="reservation-card">
                                <div className="reservation-info">
                                    <div className="info-row">
                                        <span className="label">Book:</span>
                                        <span className="value">
                                            {reservation.bookTitle || `Book ${reservation.bookId}`}
                                        </span>
                                    </div>
                                    <div className="info-row">
                                        <span className="label">Status:</span>
                                        <span className="value">Staff is processing the physical return</span>
                                    </div>
                                </div>
                            </div>
                        ))}
                    </div>
                </div>
            )}

            {/* Other member features stay between active reservations and history. */}
            {children}

            {/* Past Reservations Section */}
            <div className="past-reservations-section">
                <div className="reservation-history-heading">
                    <div>
                        <h3>Reservation History ({pastReservations.length})</h3>
                        <p className="history-description">
                            Returned and cancelled reservations are kept here for reference.
                        </p>
                    </div>
                    <button
                        className="btn btn-danger"
                        type="button"
                        onClick={clearReservationHistory}
                        disabled={clearingHistory || returnedReservations.length === 0}
                    >
                        {clearingHistory ? "Clearing..." : "Clear reservation history"}
                    </button>
                </div>
                {pastReservations.length === 0 ? (
                    <p className="empty-message">No returned or cancelled reservations in your history.</p>
                ) : (
                    <div className="reservations-list past-reservations">
                        {pastReservations.map((reservation) => {
                            const isCancelled = reservation.reservationStatus === "Cancelled";
                            const duration = elapsedWholeDays(
                                reservation.dateReserved,
                                reservation.dateReturned
                            );
                            return (
                                <div
                                    key={reservation.reservationId}
                                    className="reservation-card past"
                                >
                                    <div className="reservation-info">
                                        <div className="info-row">
                                            <span className="label">
                                                User:
                                            </span>
                                            <span className="value">
                                                {reservation.userName ||
                                                    `User ${reservation.userId}`}
                                            </span>
                                        </div>
                                        <div className="info-row">
                                            <span className="label">
                                                Book:
                                            </span>
                                            <span className="value">
                                                {reservation.bookTitle ||
                                                    `Book ${reservation.bookId}`}
                                            </span>
                                        </div>
                                        <div className="info-row">
                                            <span className="label">
                                                Reservation ID:
                                            </span>
                                            <span className="value">
                                                {reservation.reservationId}
                                            </span>
                                        </div>
                                        <div className="info-row">
                                            <span className="label">
                                                Book ID:
                                            </span>
                                            <span className="value">
                                                {reservation.bookId}
                                            </span>
                                        </div>
                                        <div className="info-row">
                                            <span className="label">
                                                Checked Out:
                                            </span>
                                            <span className="value">
                                                {new Date(
                                                    reservation.dateReserved
                                                ).toLocaleDateString()}
                                            </span>
                                        </div>
                                        <div className="info-row">
                                            <span className="label">Status:</span>
                                            <span className="value">
                                                {isCancelled
                                                    ? "Cancelled"
                                                    : "Returned"}
                                            </span>
                                        </div>
                                        {!isCancelled && (
                                            <>
                                                <div className="info-row">
                                                    <span className="label">Returned:</span>
                                                    <span className="value">
                                                        {new Date(
                                                            reservation.dateReturned
                                                        ).toLocaleDateString()}
                                                    </span>
                                                </div>
                                                <div className="info-row">
                                                    <span className="label">Duration:</span>
                                                    <span className="value">{duration} days</span>
                                                </div>
                                            </>
                                        )}
                                    </div>
                                </div>
                            );
                        })}
                    </div>
                )}
            </div>
        </div>
    );
}

export default ReservationManager;
