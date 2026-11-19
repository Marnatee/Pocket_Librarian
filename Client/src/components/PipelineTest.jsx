//Presentation layer / UI
//Displays the API pipeline demonstration from the master branch.

import { useState } from "react";

const PIPELINE_TIMEOUT_MS = 5000;

function PipelineTest() {
    const [frontendStatus, setFrontendStatus] =
        useState("Waiting for button click");

    const [apiStatus, setApiStatus] =
        useState("Not called");

    const [backendSteps, setBackendSteps] =
        useState([]);

    const [finalResponse, setFinalResponse] =
        useState("No response yet");

    async function testPipeline() {
        // Reset the displayed information for a new test.
        setFrontendStatus("Button pressed");
        setApiStatus("Sending API request...");
        setBackendSteps([]);
        setFinalResponse("Waiting for response...");

        const abortController = new AbortController();
        const timeoutId = window.setTimeout(
            () => abortController.abort(),
            PIPELINE_TIMEOUT_MS
        );

        try {
            // Send a GET request to the ASP.NET Core controller.
            const response = await fetch("/api/pipeline/test", {
                signal: abortController.signal,
            });

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
            setFinalResponse(
                error.name === "AbortError"
                    ? "The API pipeline test timed out after 5 seconds."
                    : error.message
            );
            setFrontendStatus("Error");
        } finally {
            window.clearTimeout(timeoutId);
        }
    }

    return (
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
    );
}

export default PipelineTest;
