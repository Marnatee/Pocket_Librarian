//Presentation layer / UI
//Displays the button and status fields
//--Currently-- Calls the backend API and displays the response

import { useEffect, useState } from "react";
import "./App.css";

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

function App() {
    const [featuredBookIndex, setFeaturedBookIndex] = useState(0);
    const [isFeaturedBookPaused, setIsFeaturedBookPaused] = useState(false);

    const [frontendStatus, setFrontendStatus] =
        useState("Waiting for button click");

    const [apiStatus, setApiStatus] =
        useState("Not called");

    const [backendSteps, setBackendSteps] =
        useState([]);

    const [finalResponse, setFinalResponse] =
        useState("No response yet");

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

    async function testPipeline() {
        // Reset the displayed information for a new test.
        setFrontendStatus("Button pressed");
        setApiStatus("Sending API request...");
        setBackendSteps([]);
        setFinalResponse("Waiting for response...");

        try {
            // Send a GET request to the ASP.NET Core controller.
            const response = await fetch("/api/pipeline/test");

            setApiStatus(
                `Response received: HTTP ${response.status}`
            );

            // Treat non-successful HTTP responses as errors.
            if (!response.ok) {
                throw new Error(
                    `The server returned HTTP ${response.status}`
                );
            }

            // Convert the JSON response into a JavaScript object.
            const data = await response.json();

            // Display the information returned by the backend.
            setBackendSteps(data.steps);
            setFinalResponse(data.finalMessage);
            setFrontendStatus(
                "Frontend processed the JSON response"
            );
        } catch (error) {
            setApiStatus("API request failed");
            setFinalResponse(error.message);
            setFrontendStatus("Error");
        }
    }

    return (
        <main className="pipeline-page">
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

            <section className="pipeline-test">
                <h2>Pocket Librarian Pipeline Test</h2>

                <p>
                    Press the button to send a request through all
                    three layers.
                </p>

                <button
                    type="button"
                    onClick={testPipeline}
                >
                    Test API Pipeline
                </button>

                <section className="status-section">
                    <h2>Pipeline Status</h2>

                <label htmlFor="frontend-status">
                    Frontend status
                </label>

                <input
                    id="frontend-status"
                    type="text"
                    value={frontendStatus}
                    readOnly
                />

                <label htmlFor="api-status">
                    API status
                </label>

                <input
                    id="api-status"
                    type="text"
                    value={apiStatus}
                    readOnly
                />

                <label htmlFor="backend-steps">
                    Backend steps
                </label>

                <textarea
                    id="backend-steps"
                    value={backendSteps.join("\n")}
                    readOnly
                    rows="8"
                />

                <label htmlFor="final-response">
                    Final response
                </label>

                <input
                    id="final-response"
                    type="text"
                    value={finalResponse}
                    readOnly
                />
                </section>
            </section>
        </main>
    );
}

export default App;
