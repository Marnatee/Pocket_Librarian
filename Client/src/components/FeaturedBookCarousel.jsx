//Presentation layer / UI
//Displays the rotating Featured Book demonstration from the master branch.

import { useEffect, useState } from "react";

const featuredBooks = [
    {
        title: "One Piece, Volume 1",
        author: "Eiichiro Oda",
        image: "/featured-books/81IgOp0fOiL._UF1000,1000_QL80_.jpg",
    },
    {
        title: "Berserk, Volume 1",
        author: "Kentaro Miura",
        image: "/featured-books/Berserk_Vol_1.webp",
    },
    {
        title: "The Eye of the World",
        author: "Robert Jordan",
        image: "/featured-books/WoT01_TheEyeOfTheWorld.jpg",
    },
    {
        title: "The Men Who Started the American Revolution",
        author: "A. J. Langguth",
        image: "/featured-books/cvr9780671675622_9780671675622_hr.jpg",
    },
    {
        title: "Dungeon Crawler Carl, Volume 1",
        author: "Matt Dinniman",
        image: "/featured-books/dungeon-crawler-carl-vol-1-graphic-novel-9781638493655_hr.jpg",
    },
    {
        title: "Unsouled",
        author: "Will Wight",
        image: "/featured-books/unsouled-cover-art-by-evan-teuteberg-v0-q1f8r6d4pz881.webp",
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
                        key={featuredBooks[featuredBookIndex].image}
                        className="featured-book-cover"
                        src={featuredBooks[featuredBookIndex].image}
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
                        key={book.image}
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
