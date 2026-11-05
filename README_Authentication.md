# Pocket Librarian authentication

The authentication flow is integrated into the ASP.NET Core API and React client.

- `POST /api/auth/register` creates a new `Member` account in `dbo.Users`.
- `POST /api/auth/login` validates the password and issues an HTTP-only cookie.
- `GET /api/auth/me` returns the current signed-in user.
- `POST /api/auth/logout` clears the cookie.

The password hashing follows Owen's PBKDF2 approach. Because Jack's `Users` table
has only one `PasswordHash` column, the generated salt and hash are stored together
in that column as `base64Salt.base64Hash`.

Jack's creation script contains placeholder values such as `MockPasswordHash1`,
not real PBKDF2 hashes. Those seed accounts cannot be used to sign in until they
are given real password hashes. Registering a new account through the application
creates a compatible hash and assigns the `Member` role.

The database constraint supports `Member` and `Staff`; the application therefore
does not use Owen's `Administrator` role. To test a staff account, register a new
account and change its `Role` to `Staff` in SQL Server, or use a future staff-management
feature protected with `[Authorize(Roles = "Staff")]`.
