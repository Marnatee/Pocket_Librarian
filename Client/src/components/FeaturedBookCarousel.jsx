//Presentation layer / UI
//Displays the rotating Featured Book demonstration from the master branch.

import { useEffect, useState } from "react";
import { getBookCoverPath } from "../utils/bookCoverPath";

const featuredBooks = [
    {
        bookId: 1,
        title: "Dune",
        author: "Frank Herbert",
    },
    {
        bookId: 2,
        title: "Good Omens",
        author: "Terry Pratchett and Neil Gaiman",
    },
    {
        bookId: 3,
        title: "The Hobbit",
        author: "J. R. R. Tolkien",
    },
    {
        bookId: 4,
        title: "The Handmaid's Tale",
        author: "Margaret Atwood",
    },
    {
        bookId: 5,
        title: "One Piece, Volume 1",
        author: "Eiichiro Oda",
    },
    {
        bookId: 6,
        title: "Berserk, Volume 1",
        author: "Kentaro Miura",
    },
    {
        bookId: 7,
        title: "The Eye of the World",
        author: "Robert Jordan",
    },
    {
        bookId: 8,
        title: "The Men Who Started the American Revolution",
        author: "A. J. Langguth",
    },
    {
        bookId: 9,
        title: "Dungeon Crawler Carl, Volume 1",
        author: "Matt Dinniman",
    },
    {
        bookId: 10,
        title: "Unsouled",
        author: "Will Wight",
    },
];

function FeaturedBookCarousel() {
    const [featuredBookIndex, setFeaturedBookIndex] = useState(0);
    const [isFeaturedBookPaused, setIsFeaturedBookPaused] = useState(false);

    useEffect(() => {
        if (isFeaturedBookPaused) {
            return undefined;
        }

        const rotationTimer = window.setInterval(() => {
            setFeaturedBookIndex((currentIndex) =>
                (currentIndex + 1) % featuredBooks.length
            );
        }, 10000);

        return () => window.clearInterval(rotationTimer);
    }, [isFeaturedBookPaused]);

    return (
        <section
            className="featured-book"
            role="region"
            aria-labelledby="featured-book-heading"
            aria-roledescription="carousel"
        >
            <div className="featured-book-heading-row">
                <div>
                    <p className="eyebrow">From the Pocket Librarian collection</p>
                    <h1 id="featured-book-heading">Featured Book</h1>
                </div>

                <button
                    className="featured-book-pause"
                    type="button"
                    onClick={() => setIsFeaturedBookPaused((paused) => !paused)}
                    aria-pressed={isFeaturedBookPaused}
                >
                    {isFeaturedBookPaused ? "Resume rotation" : "Pause rotation"}
                </button>
            </div>

            <div className="display-case">
                <div className="display-case-window">
                    <img
                        key={featuredBooks[featuredBookIndex].bookId}
                        className="featured-book-cover"
                        src={getBookCoverPath(featuredBooks[featuredBookIndex].bookId)}
                        alt={`${featuredBooks[featuredBookIndex].title} book cover`}
                    />
                </div>

                <div className="display-case-details" aria-live="polite">
                    <p className="display-case-label">Now featured</p>
                    <h2>{featuredBooks[featuredBookIndex].title}</h2>
                    <p>by {featuredBooks[featuredBookIndex].author}</p>
                    <p className="display-case-count">
                        Book {featuredBookIndex + 1} of {featuredBooks.length}
                    </p>
                </div>
            </div>

            <div className="featured-book-dots" aria-label="Choose a featured book">
                {featuredBooks.map((book, index) => (
                    <button
                        key={book.bookId}
                        className={index === featuredBookIndex ? "active" : ""}
                        type="button"
                        onClick={() => setFeaturedBookIndex(index)}
                        aria-label={`Show ${book.title}`}
                        aria-current={index === featuredBookIndex ? "true" : undefined}
                    />
                ))}
            </div>
        </section>
    );
}

export default FeaturedBookCarousel;
