// Reservation Manager Component
// Displays user's current and past reservations
// Allows users to checkout and return books

import { useCallback, useEffect, useState } from "react";
import "./ReservationManager.css";

const RESERVATION_API_BASE = "/api/reservation";

function ReservationManager({ userId }) {
    const [reservations, setReservations] = useState([]);
    const [activeReservations, setActiveReservations] = useState([]);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState(null);
    const [success, setSuccess] = useState(null);
    const [showCheckoutForm, setShowCheckoutForm] = useState(false);
    const [bookIdInput, setBookIdInput] = useState("");

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

    // Create a new reservation (checkout book)
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
                `Book checked out successfully! Reservation ID: ${data.reservationId}`
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

    // Return a book
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

            setSuccess("Book returned successfully!");

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

    // Load reservations on mount
    useEffect(() => {
        const loadReservations = window.setTimeout(() => {
            void fetchActiveReservations();
            void fetchAllReservations();
        }, 0);

        return () => window.clearTimeout(loadReservations);
    }, [fetchActiveReservations, fetchAllReservations]);

    const pastReservations = reservations.filter((r) => r.dateReturned !== null);

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

            {/* Checkout Book Section */}
            <div className="checkout-section">
                {!showCheckoutForm ? (
                    <button
                        onClick={() => setShowCheckoutForm(true)}
                        className="btn btn-primary"
                        disabled={loading}
                    >
                        Checkout Book
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
                                {loading ? "Processing..." : "Checkout"}
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
                <h3>Currently Checked Out ({activeReservations.length})</h3>
                {loading && activeReservations.length === 0 ? (
                    <p className="loading-text">Loading...</p>
                ) : activeReservations.length === 0 ? (
                    <p className="empty-message">
                        You have no books currently checked out.
                    </p>
                ) : (
                    <div className="reservations-list">
                        {activeReservations.map((reservation) => {
                            const daysCheckedOut = Math.floor(
                                (new Date() -
                                    new Date(reservation.dateReserved)) /
                                    (1000 * 60 * 60 * 24)
                            );
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

            {/* Past Reservations Section */}
            {pastReservations.length > 0 && (
                <div className="past-reservations-section">
                    <h3>Reservation History ({pastReservations.length})</h3>
                    <div className="reservations-list past-reservations">
                        {pastReservations.map((reservation) => {
                            const duration = Math.floor(
                                (new Date(reservation.dateReturned) -
                                    new Date(reservation.dateReserved)) /
                                    (1000 * 60 * 60 * 24)
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
                                            <span className="label">
                                                Returned:
                                            </span>
                                            <span className="value">
                                                {new Date(
                                                    reservation.dateReturned
                                                ).toLocaleDateString()}
                                            </span>
                                        </div>
                                        <div className="info-row">
                                            <span className="label">
                                                Duration:
                                            </span>
                                            <span className="value">
                                                {duration} days
                                            </span>
                                        </div>
                                    </div>
                                </div>
                            );
                        })}
                    </div>
                </div>
            )}
        </div>
    );
}

export default ReservationManager;
