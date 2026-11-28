# Book donation workflow

This version adds the member donation request workflow to the current
Server/BLL/DAL/Client project structure.

## Member behavior

Signed-in members and staff can submit a donation from the Member page. Guests
cannot submit because the API requires either the Member or Staff role.

Only these fields are mandatory:

- Title
- Number of copies

The number of copies defaults to 1. Author, ISBN, genre, publication year,
description, condition, cover image, and donation comments are optional.

New submissions are stored as Pending Verification and retain the submitting
user's UserID.

## Staff behavior

Staff can open Catalogue administration from the Staff page. Staff can view,
edit, verify, approve, reject, archive, and delete donation requests; manually
create or edit catalogue records, including aggregate copy counts.

- Approving without an existing Book ID creates a new catalogue book.
- Entering an existing Book ID increases that book's total and available copy counts.
- The approval and inventory update happen in one SQL transaction.
- Rejecting or archiving a request keeps its history and records the staff review.

## Database setup

Run the current Pocket Librarian database setup script against the existing
database before testing the feature. The script is designed to be rerunnable
and uses the current `dbo.Users(UserID)` schema.
