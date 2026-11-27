// Presentation layer / UI
// Dedicated page for the API pipeline demonstration.

import PipelineTest from "../components/PipelineTest";

function PipelinePage() {
    return (
        <section className="page-view" aria-labelledby="pipeline-page-heading">
            <div className="page-introduction">
                <p className="page-eyebrow">Developer tools</p>
                <h1 id="pipeline-page-heading">API Pipeline Test</h1>
                <p>
                    Verify the request path from the React client through the API,
                    business logic, and data-access layers.
                </p>
            </div>

            <PipelineTest />
        </section>
    );
}

export default PipelinePage;
