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

    // Loads the available catalogue genres when the component first appears.
    useEffect(() => {
        async function loadGenres() {
            try {
                const response = await fetch("/api/catalogue/genres");

                if (!response.ok) {
                    throw new Error("Failed to load catalogue genres.");
                }

                const data = await response.json();
                setGenres(data);
            }
            catch (error) {
                console.error("Error loading catalogue genres:", error);
            }
        }

        loadGenres();
    }, []);

    // Sends the current catalogue filters to the backend and stores
    // the matching books returned by the search endpoint.
    async function handleSearch(event) {
        event.preventDefault();

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
                throw new Error("Catalogue search failed.");
            }

            const data = await response.json();
            setResults(data);
        }
        catch (error) {
            console.error("Error searching catalogue:", error);
            setResults([]);
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
                            onChange={(event) => setTitle(event.target.value)}
                        />
                    </label>

                    <label>
                        ISBN
                        <input
                            type="text"
                            value={isbn}
                            onChange={(event) => setIsbn(event.target.value)}
                        />
                    </label>

                    <label>
                        Author
                        <input
                            type="text"
                            value={author}
                            onChange={(event) => setAuthor(event.target.value)}
                        />
                    </label>

                    <label>
                        Genre
                        <select
                            value={genre}
                            onChange={(event) => setGenre(event.target.value)}
                        >
                            <option value="">All Genres</option>

                            {genres.map((catalogueGenre) => (
                                <option
                                    key={catalogueGenre.genreId}
                                    value={catalogueGenre.genreId}
                                >
                                    {catalogueGenre.genreName}
                                </option>
                            ))}
                        </select>
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
        </section>
    );
}

export default CatalogueSearch;