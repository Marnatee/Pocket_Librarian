# Book donation workflow

This version adds the member donation request workflow to the current
Server/BLL/DAL/Client project structure.

## Member behavior

Signed-in members can submit a donation from the Member page. Guests cannot
submit because the API requires the Member role.

Only these fields are mandatory:

- Title
- Number of copies

The number of copies defaults to 1. Author, ISBN, genre, publication year,
description, condition, cover image, and donation comments are optional.

New submissions are stored as Pending Verification and retain the submitting
member's UserID.

## Staff behavior

Staff can open Catalogue administration from the Staff page. Staff can view,
edit, verify, approve, reject, archive, and delete donation requests; manually
create or edit catalogue records; and manage each physical copy separately.

- Approving without an existing Book ID creates a new catalogue book.
- Entering an existing Book ID adds one `BookCopies` row per donated copy.
- The approval and inventory update happen in one SQL transaction.
- Rejecting or archiving a request keeps its history and records the staff review.
- Each copy can have its own condition, availability status, location, and notes.

## Database setup

Run `Database/CatalogueAdministrationFeature.sql` against the existing Pocket
Librarian database before testing the feature. The script is designed to be
rerunnable and uses the current `dbo.Users(UserID)` schema.
