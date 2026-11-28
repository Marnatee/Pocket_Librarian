# Returns queue

The returns queue is a staff-only workflow for processing books that have been
handed back to the library.

1. `PUT /api/reservation/{id}/return` changes a checked-out reservation to
   `ReturnPending` and creates an unassigned `ReturnTasks` row in the same
   database transaction. It does not mark the book returned yet.
2. A staff member claims the next available task through
   `POST /api/returnsqueue/claim-next`.
3. Staff completes the task, records that the book was damaged, creates a new
   found-book task, or opens a support ticket.
4. Each action, its inventory effect, and the final `Returned` reservation
   timestamp are committed together.

The queue endpoints require the authenticated user to have the `Staff` role.
The API obtains the staff ID from the authentication cookie; clients must not
send an arbitrary staff ID.

Run `Database/WorkflowAndInventoryMigration.sql` after the core database tables
exist. It creates and updates the fulfillment, return-task, action-history,
support-ticket, notification, and reservation workflow tables in one maintained,
repeatable migration. The current implementation uses the project’s aggregate `Books`
inventory fields (`TotalCopies` and `CopiesAvailable`). Individual
physical-copy tracking is outside the current project scope.
