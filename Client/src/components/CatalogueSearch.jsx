import { useEffect, useState } from "react";
import "./CatalogueSearch.css";

function CatalogueSearch() {
    // Stores the current catalogue search criteria entered by the user.
    const [title, setTitle] = useState("");
    const [isbn, setIsbn] = useState("");
    const [author, setAuthor] = useState("");
    const [genre, setGenre] = useState("");
    const [availableOnly, setAvailableOnly] = useState(false);

    // Stores the genres retrieved from the backend for the filter dropdown.
    const [genres, setGenres] = useState([]);

    // Stores the books returned by the catalogue search.
    const [results, setResults] = useState([]);

    // Tracks whether the user has performed a search so an empty result
    // can be distinguished from the initial page state.
    const [hasSearched, setHasSearched] = useState(false);

    // Stores an error message if the catalogue search cannot be completed.
    const [searchError, setSearchError] = useState("");

    // Stores an error message if the catalogue genre list cannot be loaded.
    const [genreError, setGenreError] = useState("");

    // Loads the available catalogue genres when the component first appears.
    // Retries temporary failures so a delayed backend startup does not leave
    // the genre dropdown empty for the rest of the page session.
    useEffect(() => {
        async function loadGenres() {
            const maxAttempts = 10;
            const retryDelay = 500;

            for (let attempt = 1; attempt <= maxAttempts; attempt++) {
                try {
                    const response = await fetch("/api/catalogue/genres");

                    if (!response.ok) {
                        throw new Error("Failed to load catalogue genres.");
                    }

                    const data = await response.json();
                    setGenres(data);
                    setGenreError("");
                    return;
                }
                catch (error) {
                    if (attempt === maxAttempts) {
                        console.error(
                            "Error loading catalogue genres after multiple attempts:",
                            error
                        );

                        setGenreError("Genres could not be loaded.");
                        return;
                    }

                    await new Promise((resolve) =>
                        setTimeout(resolve, retryDelay)
                    );
                }
            }
        }

        loadGenres();
    }, []);

    // Sends the current catalogue filters to the backend and stores
    // the matching books returned by the search endpoint.
    async function handleSearch(event) {
        event.preventDefault();
        setSearchError("");

        const searchParameters = new URLSearchParams();

        if (title.trim() !== "") {
            searchParameters.append("title", title.trim());
        }

        if (isbn.trim() !== "") {
            searchParameters.append("isbn", isbn.trim());
        }

        if (author.trim() !== "") {
            searchParameters.append("author", author.trim());
        }

        if (genre !== "") {
            searchParameters.append("genreId", genre);
        }

        searchParameters.append("availableOnly", availableOnly);

        try {
            const response = await fetch(
                `/api/catalogue/search?${searchParameters.toString()}`
            );

            if (!response.ok) {
                let errorMessage = "Catalogue search failed.";

                try {
                    const errorData = await response.json();

                    if (errorData.message) {
                        errorMessage = errorData.message;
                    }
                }
                catch {
                    // Keep the generic message if the server did not return JSON.
                }

                throw new Error(errorMessage);
            }

            const data = await response.json();
            setResults(data);
            setHasSearched(true);
        }
        catch (error) {
            console.error("Error searching catalogue:", error);
            setResults([]);
            setHasSearched(true);
            setSearchError(
                error.message || "The catalogue search could not be completed."
            );
        }
    }

    return (
        <section className="feature-placeholder catalogue-search">
            <h2>Search Catalogue</h2>

            <form onSubmit={handleSearch}>
                <div className="catalogue-search-fields">
                    <label>
                        Title
                        <input
                            type="text"
                            value={title}
                            maxLength={255}
                            onChange={(event) => setTitle(event.target.value)}
                        />
                    </label>

                    <label>
                        ISBN
                        <input
                            type="text"
                            value={isbn}
                            maxLength={13}
                            onChange={(event) => setIsbn(event.target.value)}
                        />
                    </label>

                    <label>
                        Author
                        <input
                            type="text"
                            value={author}
                            maxLength={101}
                            onChange={(event) => setAuthor(event.target.value)}
                        />
                    </label>

                    <label>
                        Genre
                        <select
                            value={genre}
                            onChange={(event) => setGenre(event.target.value)}
                            disabled={genreError !== ""}
                        >
                            <option value="">
                                {genreError !== "" ? "Unable to load genres" : "All Genres"}
                            </option>

                            {genres.map((catalogueGenre) => (
                                <option
                                    key={catalogueGenre.genreId}
                                    value={catalogueGenre.genreId}
                                >
                                    {catalogueGenre.genreName}
                                </option>
                            ))}
                        </select>

                        {genreError !== "" && (
                            <span className="catalogue-genre-error">
                                {genreError}
                            </span>
                        )}
                    </label>
                </div>

                <label className="catalogue-availability-filter">
                    <input
                        type="checkbox"
                        checked={availableOnly}
                        onChange={(event) =>
                            setAvailableOnly(event.target.checked)
                        }
                    />
                    Available books only
                </label>

                <button type="submit" className="catalogue-search-button">
                    Search
                </button>
            </form>
            {/* Displays the outcome of the most recent catalogue search. */}
            {hasSearched && (
                <div className="catalogue-results">
                    <h3>Search Results</h3>

                    {searchError !== "" ? (
                        <p className="catalogue-search-error">
                            {searchError}
                        </p>
                    ) : results.length === 0 ? (
                        <p>No books matched your search.</p>
                    ) : (
                        results.map((book) => (
                            <article
                                className="catalogue-result"
                                key={book.bookId}
                            >
                                <h4>{book.title}</h4>

                                <p>
                                    <strong>Author:</strong>{" "}
                                    {book.authors.join(", ")}
                                </p>

                                <p>
                                    <strong>ISBN:</strong>{" "}
                                    {book.isbn || "Not available"}
                                </p>

                                <p>
                                    <strong>Genre:</strong>{" "}
                                    {book.genres.join(", ")}
                                </p>

                                <p>{book.description}</p>

                                <p>
                                    <strong>Availability:</strong>{" "}
                                    {book.copiesAvailable} of {book.totalCopies} copies are available.
                                </p>
                            </article>
                        ))
                    )}
                </div>
            )}
        </section>
    );
}

export default CatalogueSearch;