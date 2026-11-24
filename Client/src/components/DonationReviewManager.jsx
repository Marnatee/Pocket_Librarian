import { useCallback, useEffect, useState } from "react";

function DonationReviewManager() {
    const [donations, setDonations] = useState([]);
    const [isLoading, setIsLoading] = useState(true);
    const [message, setMessage] = useState("");
    const [error, setError] = useState("");
    const [reviewState, setReviewState] = useState({});

    const loadDonations = useCallback(async () => {
        setIsLoading(true);
        setError("");

        try {
            const response = await fetch("/api/donations/pending", {
                credentials: "include",
            });
            const data = await response.json().catch(() => []);

            if (!response.ok) {
                setError(data.message || "Pending donations could not be loaded.");
                return;
            }

            setDonations(data);
        } catch {
            setError("The donation service could not be reached.");
        } finally {
            setIsLoading(false);
        }
    }, []);

    useEffect(() => {
        const initialLoad = window.setTimeout(() => {
            loadDonations();
        }, 0);

        return () => window.clearTimeout(initialLoad);
    }, [loadDonations]);

    function updateReview(donationId, field, value) {
        setReviewState((current) => ({
            ...current,
            [donationId]: {
                ...current[donationId],
                [field]: value,
            },
        }));
    }

    async function reviewDonation(donation, action) {
        const review = reviewState[donation.donationId] || {};
        const existingBookId = Number(review.existingBookId);
        setMessage("");
        setError("");

        try {
            const response = await fetch(
                "/api/donations/" + donation.donationId + "/" + action,
                {
                    method: "POST",
                    credentials: "include",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({
                        existingBookId: existingBookId > 0 ? existingBookId : null,
                        reviewNotes: review.reviewNotes || null,
                    }),
                }
            );
            const data = await response.json().catch(() => ({}));

            if (!response.ok) {
                setError(data.message || "The donation review could not be completed.");
                return;
            }

            setMessage(data.message);
            await loadDonations();
        } catch {
            setError("The donation service could not be reached.");
        }
    }

    return (
        <section className="member-feature-card donation-review-card">
            <div className="member-feature-heading">
                <div>
                    <p className="page-eyebrow">Staff verification</p>
                    <h2>Pending book donations</h2>
                </div>
                <button
                    className="member-secondary-button"
                    type="button"
                    onClick={loadDonations}
                    disabled={isLoading}
                >
                    Refresh
                </button>
            </div>
            <p className="member-muted-text">
                Approving without a catalogue Book ID creates a new book. Enter an
                existing Book ID to add the donated copies to that catalogue record.
            </p>
            {isLoading && <p className="member-muted-text">Loading donations...</p>}
            {!isLoading && !error && donations.length === 0 && (
                <p className="member-muted-text">There are no pending donations.</p>
            )}
            <div className="donation-review-list">
                {donations.map((donation) => {
                    const review = reviewState[donation.donationId] || {};
                    return (
                        <article className="donation-review-item" key={donation.donationId}>
                            <div className="donation-review-heading">
                                <h3>{donation.title}</h3>
                                <span className="donation-status">{donation.status}</span>
                            </div>
                            <p>
                                Submitted by {donation.donorName} ({donation.donorEmail})
                            </p>
                            <dl className="donation-details">
                                <div><dt>Author</dt><dd>{donation.author || "Not provided"}</dd></div>
                                <div><dt>ISBN</dt><dd>{donation.isbn || "Not provided"}</dd></div>
                                <div><dt>Genre</dt><dd>{donation.genre || "Not provided"}</dd></div>
                                <div><dt>Copies</dt><dd>{donation.numberOfCopies}</dd></div>
                                <div><dt>Cover image</dt><dd>{donation.hasCoverImage ? "Provided" : "Not provided"}</dd></div>
                            </dl>
                            {donation.description && <p><strong>Notes:</strong> {donation.description}</p>}
                            {donation.donationComments && <p><strong>Comments:</strong> {donation.donationComments}</p>}
                            <div className="donation-review-controls">
                                <label>
                                    Existing Book ID (optional)
                                    <input
                                        type="number"
                                        min="1"
                                        value={review.existingBookId || ""}
                                        onChange={(event) => updateReview(
                                            donation.donationId,
                                            "existingBookId",
                                            event.target.value
                                        )}
                                    />
                                </label>
                                <label>
                                    Review notes
                                    <textarea
                                        rows="2"
                                        value={review.reviewNotes || ""}
                                        onChange={(event) => updateReview(
                                            donation.donationId,
                                            "reviewNotes",
                                            event.target.value
                                        )}
                                    />
                                </label>
                                <div className="member-form-actions">
                                    <button
                                        className="member-primary-button"
                                        type="button"
                                        onClick={() => reviewDonation(donation, "approve")}
                                    >
                                        Approve
                                    </button>
                                    <button
                                        className="member-secondary-button"
                                        type="button"
                                        onClick={() => reviewDonation(donation, "reject")}
                                    >
                                        Reject
                                    </button>
                                </div>
                            </div>
                        </article>
                    );
                })}
            </div>
            {message && <p className="member-success" role="status">{message}</p>}
            {error && <p className="member-error" role="alert">{error}</p>}
        </section>
    );
}

export default DonationReviewManager;
