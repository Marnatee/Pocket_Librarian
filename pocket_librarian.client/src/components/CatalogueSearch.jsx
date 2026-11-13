import { useState } from "react";

function CatalogueSearch() {
    const [title, setTitle] = useState("");
    const [isbn, setIsbn] = useState("");
    const [author, setAuthor] = useState("");
    const [genre, setGenre] = useState("");
    const [availableOnly, setAvailableOnly] = useState(false);

    function handleSearch(event) {
        event.preventDefault();

        // Going to connect this to the ASP.NET backend.
    }

    return (
        <section className="catalogue-search">
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