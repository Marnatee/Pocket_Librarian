import { useCallback, useEffect, useState } from "react";
import "./CatalogueAdministrationManager.css";

const conditions = ["Like New", "Good", "Fair", "Poor"];
const sourceTypes = ["Member donation", "Publisher donation", "Library acquisition", "Inheritance", "Other"];

const blankBook = {
    title: "",
    author: "",
    isbn: "",
    genre: "",
    publicationYear: "",
    description: "",
    totalCopies: 0,
    copiesAvailable: 0,
    sourceType: "Library acquisition",
    coverImage: null,
};

function CatalogueAdministrationManager() {
    const [section, setSection] = useState("donations");
    const [donationStatus, setDonationStatus] = useState("Pending");
    const [donations, setDonations] = useState([]);
    const [donationEdits, setDonationEdits] = useState({});
    const [books, setBooks] = useState([]);
    const [bookForm, setBookForm] = useState(blankBook);
    const [loading, setLoading] = useState(false);
    const [message, setMessage] = useState("");
    const [error, setError] = useState("");

    const request = useCallback(async (url, options = {}) => {
        const response = await fetch(url, { credentials: "include", ...options });
        const data = await response.json().catch(() => ({}));
        if (!response.ok) {
            throw new Error(data.message || "Request failed (" + response.status + ").");
        }
        return data;
    }, []);

    const loadDonations = useCallback(async () => {
        setLoading(true);
        setError("");
        try {
            const data = await request(
                "/api/catalogue-admin/donations?status=" + encodeURIComponent(donationStatus)
            );
            setDonations(data);
            setDonationEdits(Object.fromEntries(data.map((donation) => [
                donation.donationId,
                {
                    title: donation.title || "",
                    author: donation.author || "",
                    isbn: donation.isbn || "",
                    genre: donation.genre || "",
                    publicationYear: donation.publicationYear || "",
                    description: donation.description || "",
                    condition: donation.condition || "Good",
                    copiesDonated: donation.copiesDonated || 1,
                    sourceType: donation.sourceType || "Member donation",
                    donationComments: donation.donationComments || "",
                    reviewNotes: donation.staffNotes || "",
                    existingBookId: "",
                    coverImage: null,
                },
            ])));
        } catch (requestError) {
            setError(requestError.message);
        } finally {
            setLoading(false);
        }
    }, [donationStatus, request]);

    const loadBooks = useCallback(async () => {
        setLoading(true);
        setError("");
        try {
            setBooks(await request("/api/catalogue-admin/books"));
        } catch (requestError) {
            setError(requestError.message);
        } finally {
            setLoading(false);
        }
    }, [request]);

    useEffect(() => {
        const initialLoad = window.setTimeout(() => {
            if (section === "donations") loadDonations();
            else loadBooks();
        }, 0);
        return () => window.clearTimeout(initialLoad);
    }, [section, loadDonations, loadBooks]);

    function setDonationField(donationId, field, value) {
        setDonationEdits((current) => ({
            ...current,
            [donationId]: { ...current[donationId], [field]: value },
        }));
    }

    async function saveDonation(donationId) {
        const edit = donationEdits[donationId];
        const body = new FormData();
        Object.entries(edit).forEach(([key, value]) => {
            if (key !== "reviewNotes" && key !== "existingBookId" && key !== "coverImage" && value !== "") {
                body.append(key, value);
            }
        });
        if (edit.coverImage) body.append("coverImage", edit.coverImage);
        await runAction(
            () => request("/api/catalogue-admin/donations/" + donationId, { method: "PUT", body }),
            "Donation updated.",
            loadDonations
        );
    }

    async function donationAction(donation, action) {
        const edit = donationEdits[donation.donationId] || {};
        if (action === "delete" && !window.confirm("Delete this donation record?")) return;
        const body = action === "approve"
            ? {
                existingBookId: Number(edit.existingBookId) > 0 ? Number(edit.existingBookId) : null,
                notes: edit.reviewNotes || null,
            }
            : { notes: edit.reviewNotes || null };
        await runAction(
            () => request("/api/catalogue-admin/donations/" + donation.donationId + "/" + action, {
                method: action === "delete" ? "DELETE" : "POST",
                ...(action === "delete" ? {} : {
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify(body),
                }),
            }),
            "Donation " + action + "d.",
            loadDonations
        );
    }

    async function createBook(event) {
        event.preventDefault();
        const body = new FormData();
        Object.entries(bookForm).forEach(([key, value]) => {
            if (value !== null && value !== "") body.append(key, value);
        });
        await runAction(
            () => request("/api/catalogue-admin/books", { method: "POST", body }),
            "Catalogue record created.",
            async () => {
                setBookForm(blankBook);
                await loadBooks();
            }
        );
    }

    async function updateBook(book, event) {
        event.preventDefault();
        const form = new FormData(event.currentTarget);
        const file = event.currentTarget.coverImage.files?.[0];
        if (file) form.set("coverImage", file);
        await runAction(
            () => request("/api/catalogue-admin/books/" + book.bookId, { method: "PUT", body: form }),
            "Catalogue record updated.",
            loadBooks
        );
    }

    async function deleteBook(book) {
        if (!window.confirm("Delete \"" + book.title + "\" from the catalogue?")) return;
        await runAction(
            () => request("/api/catalogue-admin/books/" + book.bookId, { method: "DELETE" }),
            "Catalogue record deleted.",
            loadBooks
        );
    }

    async function runAction(action, successMessage, after) {
        setError("");
        setMessage("");
        try {
            await action();
            setMessage(successMessage);
            await after();
        } catch (requestError) {
            setError(requestError.message);
        }
    }

    return (
        <section className="catalogue-admin">
            <div className="catalogue-admin-heading">
                <div>
                    <p className="page-eyebrow">Staff feature</p>
                    <h2>Catalogue administration</h2>
                    <p>Review donations, maintain catalogue records, and manage aggregate availability.</p>
                </div>
                <div className="catalogue-admin-tabs" role="tablist">
                    <button type="button" className={section === "donations" ? "active" : ""} onClick={() => setSection("donations")}>Donations</button>
                    <button type="button" className={section === "books" ? "active" : ""} onClick={() => setSection("books")}>Catalogue</button>
                </div>
            </div>

            {message && <p className="catalogue-admin-success" role="status">{message}</p>}
            {error && <p className="catalogue-admin-error" role="alert">{error}</p>}

            {section === "donations" ? (
                <div>
                    <div className="catalogue-admin-toolbar">
                        <label>
                            Donation status
                            <select value={donationStatus} onChange={(event) => setDonationStatus(event.target.value)}>
                                <option value="Pending">Pending</option>
                                <option value="Verified">Verified</option>
                                <option value="Approved">Approved</option>
                                <option value="Rejected">Rejected</option>
                                <option value="Archived">Archived</option>
                            </select>
                        </label>
                        <button type="button" onClick={loadDonations} disabled={loading}>Refresh</button>
                    </div>
                    {loading && <p>Loading donations...</p>}
                    {!loading && donations.length === 0 && <p>No donations match this status.</p>}
                    <div className="catalogue-admin-list">
                        {donations.map((donation) => {
                            const edit = donationEdits[donation.donationId] || {};
                            return (
                                <article className="catalogue-admin-card" key={donation.donationId}>
                                    <div className="catalogue-admin-card-header">
                                        <h3>{donation.title}</h3>
                                        <span>{donation.verificationStatus}</span>
                                    </div>
                                    <p>Submitted by {donation.donorName || "Unknown member"}{donation.donorEmail ? " (" + donation.donorEmail + ")" : ""}</p>
                                    <div className="catalogue-admin-form-grid">
                                        <label>Title<input value={edit.title || ""} onChange={(event) => setDonationField(donation.donationId, "title", event.target.value)} /></label>
                                        <label>Author<input value={edit.author || ""} onChange={(event) => setDonationField(donation.donationId, "author", event.target.value)} /></label>
                                        <label>ISBN<input value={edit.isbn || ""} onChange={(event) => setDonationField(donation.donationId, "isbn", event.target.value)} /></label>
                                        <label>Genre<input value={edit.genre || ""} onChange={(event) => setDonationField(donation.donationId, "genre", event.target.value)} /></label>
                                        <label>Publication year<input type="number" value={edit.publicationYear || ""} onChange={(event) => setDonationField(donation.donationId, "publicationYear", event.target.value)} /></label>
                                        <label>Copies<input type="number" min="1" max="1000" value={edit.copiesDonated || 1} onChange={(event) => setDonationField(donation.donationId, "copiesDonated", event.target.value)} /></label>
                                        <label>Condition<select value={edit.condition || "Good"} onChange={(event) => setDonationField(donation.donationId, "condition", event.target.value)}>{conditions.map((value) => <option key={value}>{value}</option>)}</select></label>
                                        <label>Source type<select value={edit.sourceType || "Member donation"} onChange={(event) => setDonationField(donation.donationId, "sourceType", event.target.value)}>{sourceTypes.map((value) => <option key={value}>{value}</option>)}</select></label>
                                        <label className="wide">Cover image<input type="file" accept="image/jpeg,image/png,image/webp" onChange={(event) => setDonationField(donation.donationId, "coverImage", event.target.files?.[0] || null)} /></label>
                                        <label className="wide">Description<textarea value={edit.description || ""} onChange={(event) => setDonationField(donation.donationId, "description", event.target.value)} /></label>
                                        <label className="wide">Review notes<textarea value={edit.reviewNotes || ""} onChange={(event) => setDonationField(donation.donationId, "reviewNotes", event.target.value)} /></label>
                                        <label>Existing Book ID for approval<input type="number" min="1" value={edit.existingBookId || ""} onChange={(event) => setDonationField(donation.donationId, "existingBookId", event.target.value)} /></label>
                                    </div>
                                    <div className="catalogue-admin-actions">
                                        <button type="button" onClick={() => saveDonation(donation.donationId)}>Save edits</button>
                                        {donation.verificationStatus === "Pending" && <button type="button" onClick={() => donationAction(donation, "verify")}>Verify</button>}
                                        {(donation.verificationStatus === "Pending" || donation.verificationStatus === "Verified") && <button type="button" className="primary" onClick={() => donationAction(donation, "approve")}>Approve</button>}
                                        {(donation.verificationStatus === "Pending" || donation.verificationStatus === "Verified") && <button type="button" onClick={() => donationAction(donation, "reject")}>Reject</button>}
                                        {donation.verificationStatus !== "Approved" && donation.verificationStatus !== "Archived" && <button type="button" onClick={() => donationAction(donation, "archive")}>Archive</button>}
                                        {donation.verificationStatus !== "Approved" && <button type="button" className="danger" onClick={() => donationAction(donation, "delete")}>Delete</button>}
                                    </div>
                                </article>
                            );
                        })}
                    </div>
                </div>
            ) : (
                <div>
                    <form className="catalogue-admin-card catalogue-admin-form-grid" onSubmit={createBook}>
                        <h3 className="wide">Add catalogue record</h3>
                        <label>Title<input required value={bookForm.title} onChange={(event) => setBookForm({ ...bookForm, title: event.target.value })} /></label>
                        <label>Author<input value={bookForm.author} onChange={(event) => setBookForm({ ...bookForm, author: event.target.value })} /></label>
                        <label>ISBN<input value={bookForm.isbn} onChange={(event) => setBookForm({ ...bookForm, isbn: event.target.value })} /></label>
                        <label>Genre<input value={bookForm.genre} onChange={(event) => setBookForm({ ...bookForm, genre: event.target.value })} /></label>
                        <label>Publication year<input type="number" value={bookForm.publicationYear} onChange={(event) => setBookForm({ ...bookForm, publicationYear: event.target.value })} /></label>
                        <label>Total copies<input name="totalCopies" type="number" min="0" value={bookForm.totalCopies} onChange={(event) => setBookForm({ ...bookForm, totalCopies: event.target.value, copiesAvailable: event.target.value })} /></label>
                        <label>Available copies<input name="copiesAvailable" type="number" min="0" max={bookForm.totalCopies} value={bookForm.copiesAvailable} onChange={(event) => setBookForm({ ...bookForm, copiesAvailable: event.target.value })} /></label>
                        <label>Source type<select value={bookForm.sourceType} onChange={(event) => setBookForm({ ...bookForm, sourceType: event.target.value })}>{sourceTypes.map((value) => <option key={value}>{value}</option>)}</select></label>
                        <label className="wide">Description<textarea value={bookForm.description} onChange={(event) => setBookForm({ ...bookForm, description: event.target.value })} /></label>
                        <label className="wide">Cover image<input type="file" accept="image/jpeg,image/png,image/webp" onChange={(event) => setBookForm({ ...bookForm, coverImage: event.target.files?.[0] || null })} /></label>
                        <div className="catalogue-admin-actions wide"><button className="primary" type="submit">Add record</button></div>
                    </form>
                    <div className="catalogue-admin-list">
                        {books.map((book) => (
                            <article className="catalogue-admin-card" key={book.bookId}>
                                <form className="catalogue-admin-form-grid" onSubmit={(event) => updateBook(book, event)}>
                                    <h3 className="wide">{book.title} <small>Book #{book.bookId}</small></h3>
                                    <label>Title<input name="title" defaultValue={book.title} required /></label>
                                    <label>Author<input name="author" defaultValue={book.authors?.join(", ") || ""} /></label>
                                    <label>ISBN<input name="isbn" defaultValue={book.isbn || ""} /></label>
                                    <label>Genre<input name="genre" defaultValue={book.genres?.join(", ") || ""} /></label>
                                    <label>Publication year<input name="publicationYear" type="number" defaultValue={book.publicationYear || ""} /></label>
                                    <label>Total copies<input name="totalCopies" type="number" min="0" defaultValue={book.totalCopies} /></label>
                                    <label>Available copies<input name="copiesAvailable" type="number" min="0" max={book.totalCopies} defaultValue={book.copiesAvailable} /></label>
                                    <label>Source type<select name="sourceType" defaultValue={book.sourceType || "Library acquisition"}>{sourceTypes.map((value) => <option key={value}>{value}</option>)}</select></label>
                                    <label className="wide">Description<textarea name="description" defaultValue={book.description || ""} /></label>
                                    <label className="wide">Replace cover image<input name="coverImage" type="file" accept="image/jpeg,image/png,image/webp" /></label>
                                    <div className="catalogue-admin-actions wide">
                                        <button className="primary" type="submit">Save record</button>
                                        <button type="button" className="danger" onClick={() => deleteBook(book)}>Delete record</button>
                                    </div>
                                </form>
                            </article>
                        ))}
                    </div>
                </div>
            )}
        </section>
    );
}

export default CatalogueAdministrationManager;
