import { useState } from "react";

const initialForm = {
    title: "",
    author: "",
    isbn: "",
    genre: "",
    publicationYear: "",
    description: "",
    bookCondition: "",
    numberOfCopies: "1",
    donationComments: "",
};

function BookDonationForm() {
    const [form, setForm] = useState(initialForm);
    const [coverImage, setCoverImage] = useState(null);
    const [message, setMessage] = useState("");
    const [error, setError] = useState("");
    const [isSubmitting, setIsSubmitting] = useState(false);

    function updateField(event) {
        const { name, value } = event.target;
        setForm((current) => ({ ...current, [name]: value }));
    }

    async function submitDonation(event) {
        event.preventDefault();
        setMessage("");
        setError("");
        setIsSubmitting(true);

        const body = new FormData();
        Object.entries(form).forEach(([key, value]) => {
            if (value !== "") {
                body.append(key, value);
            }
        });

        if (coverImage) {
            body.append("coverImage", coverImage);
        }

        try {
            const response = await fetch("/api/donations", {
                method: "POST",
                credentials: "include",
                body,
            });
            const data = await response.json().catch(() => ({}));

            if (!response.ok) {
                setError(data.message || "The donation could not be submitted.");
                return;
            }

            setForm(initialForm);
            setCoverImage(null);
            event.target.reset();
            setMessage(
                "Donation #" + data.donationId +
                " submitted. It is pending staff verification."
            );
        } catch {
            setError("The donation service could not be reached.");
        } finally {
            setIsSubmitting(false);
        }
    }

    return (
        <section className="member-feature-card donation-form-card">
            <div className="member-feature-heading">
                <div>
                    <p className="page-eyebrow">Member contribution</p>
                    <h2>Book donation request</h2>
                </div>
            </div>
            <p className="member-muted-text">
                Title and number of copies are required. All other details are optional
                and can be completed by staff during verification.
            </p>

            <form className="member-form donation-form" onSubmit={submitDonation}>
                <label>
                    Title *
                    <input
                        name="title"
                        value={form.title}
                        onChange={updateField}
                        required
                        maxLength={255}
                    />
                </label>
                <label>
                    Author
                    <input name="author" value={form.author} onChange={updateField} maxLength={255} />
                </label>
                <label>
                    ISBN
                    <input name="isbn" value={form.isbn} onChange={updateField} maxLength={20} />
                </label>
                <label>
                    Genre
                    <input name="genre" value={form.genre} onChange={updateField} maxLength={100} />
                </label>
                <label>
                    Publication year
                    <input
                        name="publicationYear"
                        type="number"
                        min="0"
                        max="9999"
                        value={form.publicationYear}
                        onChange={updateField}
                    />
                </label>
                <label>
                    Number of copies *
                    <input
                        name="numberOfCopies"
                        type="number"
                        min="1"
                        max="1000"
                        value={form.numberOfCopies}
                        onChange={updateField}
                        required
                    />
                </label>
                <label>
                    Book condition
                    <input
                        name="bookCondition"
                        value={form.bookCondition}
                        onChange={updateField}
                        maxLength={100}
                    />
                </label>
                <label>
                    Cover image
                    <input
                        name="coverImage"
                        type="file"
                        accept="image/jpeg,image/png,image/webp"
                        onChange={(event) => setCoverImage(event.target.files?.[0] ?? null)}
                    />
                </label>
                <label className="donation-field-wide">
                    Description or notes
                    <textarea
                        name="description"
                        value={form.description}
                        onChange={updateField}
                        maxLength={4000}
                        rows="3"
                    />
                </label>
                <label className="donation-field-wide">
                    Donation comments
                    <textarea
                        name="donationComments"
                        value={form.donationComments}
                        onChange={updateField}
                        maxLength={4000}
                        rows="3"
                    />
                </label>
                <div className="member-form-actions donation-field-wide">
                    <button
                        className="member-primary-button"
                        type="submit"
                        disabled={isSubmitting}
                    >
                        {isSubmitting ? "Submitting..." : "Submit donation"}
                    </button>
                </div>
            </form>
            {message && <p className="member-success" role="status">{message}</p>}
            {error && <p className="member-error" role="alert">{error}</p>}
        </section>
    );
}

export default BookDonationForm;
