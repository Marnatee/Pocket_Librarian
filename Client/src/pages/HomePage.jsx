// Presentation layer / UI
// Home page feature placeholders.

import FeaturePlaceholder from "../components/FeaturePlaceholder";
import FeaturedBookCarousel from "../components/FeaturedBookCarousel";
import PipelineTest from "../components/PipelineTest";

function HomePage() {
    return (
        <section className="page-view" aria-labelledby="home-page-heading">
            <div className="page-introduction">
                <p className="page-eyebrow">Welcome to Pocket Librarian</p>
                <h1 id="home-page-heading">Home</h1>
                <p>Choose a page above to explore the member and staff workflows.</p>
            </div>

            <div className="home-feature-layout">
                <FeaturedBookCarousel />
                <FeaturePlaceholder title="Search Catalogue" />
            </div>

            <PipelineTest />
        </section>
    );
}

export default HomePage;
