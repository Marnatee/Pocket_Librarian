# Pocket Librarian project-file architecture

The solution is organized into the four project-level components requested by
the professor:

```text
Pocket_Librarian
├── BLL       Business logic, domain models, and repository interfaces
├── DAL       SQL Server repositories and data-access implementation
├── Server    ASP.NET Core API, controllers, and application startup
└── Client    React/Vite frontend
```

## Runtime flow

```text
React Client --HTTP/JSON--> Server controllers --> BLL services
                                                     |
                                                     v
                                               DAL repositories --> SQL Server
```

The Client does not connect directly to SQL Server. The Server is the HTTP
boundary, the BLL contains business rules, and the DAL performs database work.
The BLL defines repository interfaces so the business logic does not depend on
SQL implementation details. The Server registers the BLL interfaces with their
DAL implementations through dependency injection.

## Project contents

### BLL

- `BLL/BLL.csproj` - Business-logic class-library project.
- `BLL/Models/` - Request, response, authenticated-user, and reservation models.
- `BLL/Interfaces/` - Repository contracts consumed by business services.
- `BLL/Services/` - Authentication, pipeline, and reservation business services.

### DAL

- `DAL/DAL.csproj` - Data-access class-library project.
- `DAL/Repositories/` - SQL Server implementations of the BLL repository contracts.

### Server

- `Server/Server.csproj` - ASP.NET Core API project.
- `Server/Program.cs` - API startup, authentication, authorization, and dependency injection.
- `Server/Controllers/` - HTTP API endpoints.
- `Server/appsettings.json` - Local database configuration.
- `Server/Properties/launchSettings.json` - Backend launch profiles.

### Client

- `Client/Client.esproj` - Visual Studio JavaScript project definition.
- `Client/src/` - React application code, pages, and components.
- `Client/public/` - Browser assets and book-cover images.
- `Client/vite.config.js` - Vite development server and API proxy.
- `Client/package.json` and `Client/package-lock.json` - Node package definitions.

### Solution and supporting files

- `Pocket_Librarian.slnx` - Lists BLL, DAL, Server, and Client in Solution Explorer.
- `Pocket_Librarian.slnLaunch` - Starts Server and Client together with the green arrow.
- `Start-Client.cmd` - Manual React startup fallback.
- `README.md` - Setup and operating instructions.
- `README_Database.md` - Database design and connection notes.
