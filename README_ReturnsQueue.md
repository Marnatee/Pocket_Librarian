# Returns queue

The returns queue is a staff-only workflow for processing books that have been
handed back to the library.

1. `PUT /api/reservation/{id}/return` records the reservation return and creates
   an unassigned `ReturnTasks` row in the same database transaction.
2. A staff member claims the next available task through
   `POST /api/returnsqueue/claim-next`.
3. Staff completes the task, records that the book was damaged, creates a new
   found-book task, or opens a support ticket.
4. Each action and its inventory effect are committed together.

The queue endpoints require the authenticated user to have the `Staff` role.
The API obtains the staff ID from the authentication cookie; clients must not
send an arbitrary staff ID.

Run `Database/FulfillmentQueue.sql` and `Database/ReturnsQueue.sql` after the
core database tables exist. The fulfillment script adds the reservation status
used to prevent a member from returning a book before staff has fulfilled it.
The returns script creates the return-task, action-history, and support-ticket
tables. The
current implementation uses the project’s aggregate `Books` inventory fields
(`TotalCopies` and `CopiesAvailable`). If the database is later changed to
track individual `BookCopies`, the queue should be changed to store `CopyID`
and update that physical copy instead.
