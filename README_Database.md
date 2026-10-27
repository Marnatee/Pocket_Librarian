# Pocket Librarian database design

This document records the database design shown in `pocket_librarian.client/Documentation/Pocket_Librarian_Database.png`.

![Pocket Librarian database diagram](pocket_librarian.client/Documentation/Pocket_Librarian_Database.png)

## Tables

| Table | Columns shown in the diagram | Purpose |
| --- | --- | --- |
| `Users` | `UserID` (PK), `FirstName`, `LastName`, `Email`, `PasswordHash`, `Role` | Stores member and staff accounts. |
| `Reservations` | `ReservationID` (PK), `UserID` (FK), `BookID` (FK), `DateReserved`, `DateReturned` | Stores the reservation history and connects members to books. |
| `Books` | `BookID` (PK), `ISBN`, `Title`, `Description`, `TotalCopies`, `CopiesAvailable` | Stores catalogue records and current copy availability. |
| `Authors` | `AuthorID` (PK), `FirstName`, `LastName` | Stores author records. |
| `BookAuthors` | `BookID` (PK/FK), `AuthorID` (PK/FK) | Junction table for the many-to-many book/author relationship. |
| `Genres` | `GenreID` (PK), `GenreName` | Stores genre records. |
| `BookGenres` | `BookID` (PK/FK), `GenreID` (PK/FK) | Junction table for the many-to-many book/genre relationship. |

## Relationships

- One `Users` row can have many `Reservations`; each reservation belongs to one user.
- One `Books` row can have many `Reservations`; each reservation belongs to one book.
- `Books` and `Authors` have a many-to-many relationship through `BookAuthors`.
- `Books` and `Genres` have a many-to-many relationship through `BookGenres`.
- The primary keys in the junction tables are composite keys made from their two foreign-key columns.

## Application architecture notes

The React frontend must call the ASP.NET Core API. It must not connect directly to SQL Server. The API's business-logic layer calls the data-access repository, and the repository performs the SQL query.

The current pipeline health check queries `dbo.Books` and returns a separate database status. If the database is unavailable, the local API still returns a safe health-check response so the frontend can show the database as unavailable without crashing.

The backend always runs locally through ASP.NET Core. The database connection may target local SQL Server or Azure SQL. The repository reads `ConnectionStrings:PocketLibrarianDatabase`; an environment variable named `ConnectionStrings__PocketLibrarianDatabase` can override the local development value without changing source code. The connection-status dashboard intentionally masks the server and database names when it displays connection details.

The dashboard also includes a demo-only manual connection form. It sends a supplied connection string to the local API over the running HTTPS connection, tests `dbo.Books`, and keeps a successful manual target in server memory for the current process only. It is not written to source code, browser storage, or the repository. The user must supply the full connection information required by the chosen local or Azure SQL authentication method.
