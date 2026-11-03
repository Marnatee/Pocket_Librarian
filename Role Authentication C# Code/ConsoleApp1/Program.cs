using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Cryptography;
using System.Text;

class Program
{
    // Connection information for the SQL Server database
    static string connectionString =
        @"Server=OLL\SQLEXPRESS;
          Database=Pocket-Librarian-DB;
          Integrated Security=True;
          TrustServerCertificate=True;";

    static void Main()
    {
        try
        {
            // Open a connection to the database
            using var connection =
                new SqlConnection(connectionString);

            connection.Open();

            Console.WriteLine(
                "Connected to Pocket-Librarian-DB.");

            // Make sure the Role column exists and existing users have default Member role
            EnsureRoleColumn(connection);

            /*
             TEMPORARY FIRST-ADMIN SETUP:

             If no administrator account exists yet, temporarily
             uncomment the next line. Run the program, enter a
             username, and choose Administrator.

             Then comment the line out again.
            */

            //ChangeUserRole(connection);

            // Stores the currently logged-in user's username and role
            (string Username, string Role)? loggedInUser = null;

            bool running = true;

            // Main application menu
            while (running)
            {
                Console.WriteLine();
                Console.WriteLine(
                    "===== Pocket Librarian =====");

                // A user who is not logged in is a Guest
                if (loggedInUser == null)
                {
                    Console.WriteLine("Current role: Guest");
                }
                else
                {
                    Console.WriteLine(
                        $"Logged in as {loggedInUser.Value.Username} " +
                        $"({loggedInUser.Value.Role})");
                }

                Console.WriteLine();
                Console.WriteLine("1. Register");
                Console.WriteLine("2. Log in");
                Console.WriteLine("3. Log out");
                Console.WriteLine("4. View available options");
                Console.WriteLine("5. Change a user's role");
                Console.WriteLine("6. Exit");
                Console.Write("Choose an option: ");

                string choice = Console.ReadLine() ?? "";

                switch (choice)
                {
                    // Register a new user
                    case "1":
                        RegisterUser(connection);
                        break;

                    // Log in an existing user
                    case "2":
                        if (loggedInUser != null)
                        {
                            Console.WriteLine(
                                "A user is already logged in.");
                        }
                        else
                        {
                            loggedInUser =
                                LoginUser(connection);
                        }
                        break;

                    // Log out the current user
                    case "3":
                        if (loggedInUser == null)
                        {
                            Console.WriteLine(
                                "No user is logged in.");
                        }
                        else
                        {
                            Console.WriteLine(
                                $"{loggedInUser.Value.Username} " +
                                "logged out.");

                            loggedInUser = null;
                        }
                        break;

                    // Display options based on the user's role
                    case "4":
                        ShowAvailableOptions(loggedInUser);
                        break;

                    // Only administrators can change roles
                    case "5":
                        if (loggedInUser?.Role ==
                            "Administrator")
                        {
                            ChangeUserRole(connection);
                        }
                        else
                        {
                            Console.WriteLine(
                                "Only administrators can " +
                                "change user roles.");
                        }
                        break;

                    // Close the program
                    case "6":
                        running = false;
                        Console.WriteLine("Goodbye.");
                        break;

                    default:
                        Console.WriteLine(
                            "Invalid option.");
                        break;
                }
            }
        }
        catch (SqlException ex)
        {
            // Display database-related errors
            Console.WriteLine(
                "Database error: " + ex.Message);
        }
    }

    // Adds the Role column if it does not exist and assigns existing users the Member role
    static void EnsureRoleColumn(
        SqlConnection connection)
    {
        string addRoleSql = @"
            IF COL_LENGTH(
                'dbo.RegisteredUsers',
                'Role'
            ) IS NULL
            BEGIN
                ALTER TABLE dbo.RegisteredUsers
                ADD Role NVARCHAR(20) NULL;
            END;";

        using var addRoleCommand =
            new SqlCommand(
                addRoleSql,
                connection);

        addRoleCommand.ExecuteNonQuery();

        string updateRoleSql = @"
            UPDATE dbo.RegisteredUsers
            SET Role = 'Member'
            WHERE Role IS NULL;";

        using var updateRoleCommand =
            new SqlCommand(
                updateRoleSql,
                connection);

        updateRoleCommand.ExecuteNonQuery();
    }

    // Collects registration information and stores the new user in the RegisteredUsers table
    static void RegisterUser(
        SqlConnection connection)
    {
        Console.WriteLine();
        Console.WriteLine("===== Register =====");

        Console.Write("First name: ");
        string firstName =
            Console.ReadLine() ?? "";

        Console.Write("Last name: ");
        string lastName =
            Console.ReadLine() ?? "";

        Console.Write("Email: ");
        string email =
            Console.ReadLine() ?? "";

        Console.Write("Username: ");
        string username =
            Console.ReadLine() ?? "";

        Console.Write("Password: ");
        string password =
            ReadPassword();

        // Make sure no required field is empty
        if (string.IsNullOrWhiteSpace(firstName) ||
            string.IsNullOrWhiteSpace(lastName) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password))
        {
            Console.WriteLine(
                "All fields are required.");

            return;
        }

        // Check whether the username already exists
        string checkSql = @"
            SELECT COUNT(*)
            FROM dbo.RegisteredUsers
            WHERE Username = @Username;";

        using var checkCommand =
            new SqlCommand(
                checkSql,
                connection);

        checkCommand.Parameters.Add(
            "@Username",
            SqlDbType.NVarChar,
            50).Value = username;

        int usernameCount =
            (int)checkCommand.ExecuteScalar()!;

        if (usernameCount > 0)
        {
            Console.WriteLine(
                "That username is already taken.");

            return;
        }

        // Hash the password before storing it
        var passwordResult =
            HashPassword(password);

        // New users automatically receive the Member role
        string insertSql = @"
            INSERT INTO dbo.RegisteredUsers
            (
                FirstName,
                LastName,
                Email,
                Username,
                PasswordHash,
                PasswordSalt,
                Role
            )
            VALUES
            (
                @FirstName,
                @LastName,
                @Email,
                @Username,
                @PasswordHash,
                @PasswordSalt,
                'Member'
            );";

        using var insertCommand =
            new SqlCommand(
                insertSql,
                connection);

        insertCommand.Parameters.Add(
            "@FirstName",
            SqlDbType.NVarChar,
            50).Value = firstName;

        insertCommand.Parameters.Add(
            "@LastName",
            SqlDbType.NVarChar,
            50).Value = lastName;

        insertCommand.Parameters.Add(
            "@Email",
            SqlDbType.NVarChar,
            255).Value = email;

        insertCommand.Parameters.Add(
            "@Username",
            SqlDbType.NVarChar,
            50).Value = username;

        insertCommand.Parameters.Add(
            "@PasswordHash",
            SqlDbType.NVarChar,
            200).Value =
                passwordResult.Hash;

        insertCommand.Parameters.Add(
            "@PasswordSalt",
            SqlDbType.NVarChar,
            100).Value =
                passwordResult.Salt;

        insertCommand.ExecuteNonQuery();

        Console.WriteLine(
            "Registration successful.");

        Console.WriteLine(
            "Your role is Member.");
    }

    // Finds a user and verifies their password
    static (string Username, string Role)? LoginUser(
        SqlConnection connection)
    {
        Console.WriteLine();
        Console.WriteLine("===== Log In =====");

        Console.Write("Username: ");
        string username =
            Console.ReadLine() ?? "";

        Console.Write("Password: ");
        string password =
            ReadPassword();

        string loginSql = @"
            SELECT PasswordHash,
                   PasswordSalt,
                   Role
            FROM dbo.RegisteredUsers
            WHERE Username = @Username;";

        using var loginCommand =
            new SqlCommand(
                loginSql,
                connection);

        loginCommand.Parameters.Add(
            "@Username",
            SqlDbType.NVarChar,
            50).Value = username;

        using var reader =
            loginCommand.ExecuteReader();

        if (!reader.Read())
        {
            Console.WriteLine(
                "Username not found.");

            return null;
        }

        string storedHash =
            reader["PasswordHash"].ToString()!;

        string storedSalt =
            reader["PasswordSalt"].ToString()!;

        string role =
            reader["Role"].ToString()!;

        // Compare the entered password with the stored hash
        if (VerifyPassword(
            password,
            storedHash,
            storedSalt))
        {
            Console.WriteLine(
                "Login successful.");

            Console.WriteLine(
                $"Your role is: {role}");

            return (username, role);
        }

        Console.WriteLine(
            "Incorrect password.");

        return null;
    }

    // Allows an administrator to change another user's role
    static void ChangeUserRole(
        SqlConnection connection)
    {
        Console.WriteLine();
        Console.WriteLine(
            "===== Change User Role =====");

        Console.Write("Username: ");
        string username =
            Console.ReadLine() ?? "";

        Console.Write(
            "New role (Member, Staff, Administrator): ");

        string role =
            Console.ReadLine() ?? "";

        // Only allow valid roles
        if (role != "Member" &&
            role != "Staff" &&
            role != "Administrator")
        {
            Console.WriteLine(
                "Invalid role.");

            return;
        }

        string updateSql = @"
            UPDATE dbo.RegisteredUsers
            SET Role = @Role
            WHERE Username = @Username;";

        using var command =
            new SqlCommand(
                updateSql,
                connection);

        command.Parameters.Add(
            "@Role",
            SqlDbType.NVarChar,
            20).Value = role;

        command.Parameters.Add(
            "@Username",
            SqlDbType.NVarChar,
            50).Value = username;

        int rowsUpdated =
            command.ExecuteNonQuery();

        if (rowsUpdated > 0)
        {
            Console.WriteLine(
                "User role updated successfully.");
        }
        else
        {
            Console.WriteLine(
                "Username not found.");
        }
    }

    // Displays the features available to each role
    static void ShowAvailableOptions(
        (string Username, string Role)? loggedInUser)
    {
        Console.WriteLine();

        if (loggedInUser == null)
        {
            Console.WriteLine("Guest options:");
            Console.WriteLine("- Search books");
        }
        else if (loggedInUser.Value.Role ==
                 "Member")
        {
            Console.WriteLine("Member options:");
            Console.WriteLine("- Search books");
            Console.WriteLine("- Make reservations");
        }
        else if (loggedInUser.Value.Role ==
                 "Staff")
        {
            Console.WriteLine("Staff options:");
            Console.WriteLine("- Search books");
            Console.WriteLine("- Manage books");
            Console.WriteLine("- Manage reservations");
        }
        else if (loggedInUser.Value.Role ==
                 "Administrator")
        {
            Console.WriteLine(
                "Administrator options:");

            Console.WriteLine("- Manage users");
            Console.WriteLine("- Manage roles");
            Console.WriteLine("- Manage books");
            Console.WriteLine("- Manage reservations");
        }
    }

    // Creates a random salt and password hash so the actual password is not stored
    static (string Hash, string Salt) HashPassword(
        string password)
    {
        byte[] salt =
            RandomNumberGenerator.GetBytes(16);

        byte[] hash =
            Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                100000,
                HashAlgorithmName.SHA256,
                32);

        return
        (
            Convert.ToBase64String(hash),
            Convert.ToBase64String(salt)
        );
    }

    // Verifies a login password against the stored password hash
    static bool VerifyPassword(
        string password,
        string storedHash,
        string storedSalt)
    {
        byte[] salt =
            Convert.FromBase64String(
                storedSalt);

        byte[] expectedHash =
            Convert.FromBase64String(
                storedHash);

        byte[] actualHash =
            Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                100000,
                HashAlgorithmName.SHA256,
                32);

        return CryptographicOperations
            .FixedTimeEquals(
                actualHash,
                expectedHash);
    }

    // Reads the password without displaying characters on the screen
    static string ReadPassword()
    {
        StringBuilder password =
            new StringBuilder();

        while (true)
        {
            ConsoleKeyInfo key =
                Console.ReadKey(
                    intercept: true);

            if (key.Key ==
                ConsoleKey.Enter)
            {
                Console.WriteLine();
                break;
            }

            if (key.Key ==
                    ConsoleKey.Backspace &&
                password.Length > 0)
            {
                password.Length--;
            }
            else if (!char.IsControl(
                         key.KeyChar))
            {
                password.Append(
                    key.KeyChar);
            }
        }

        return password.ToString();
    }
}