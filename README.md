# Pocket Librarian

Pocket Librarian is a React frontend with an ASP.NET Core backend.

## First-time setup

Install the following on your computer:

1. Visual Studio Community with the **ASP.NET and web development** workload.
2. Node.js **LTS** from https://nodejs.org/.

Then clone the repository and open `Pocket_Librarian.slnx` in Visual Studio.

## Starting the project

The project has two parts, and both must be running:

### Recommended: Visual Studio green arrow

After the first-time setup, select the shared `Pocket Librarian` launch profile in Visual Studio and click the green arrow. Visual Studio should start the ASP.NET backend, start the React development server, and open the website.

If the profile does not appear immediately, close and reopen the solution. The profile is stored in `Pocket_Librarian.slnLaunch` and is included in the repository for all group members.

If Visual Studio asks to restore or install npm packages, allow it to do so.

### Manual fallback

If Visual Studio does not show the `Pocket Librarian (Edge)` profile, use the two-part procedure below.

### 1. Start the backend

In Visual Studio:

1. In Solution Explorer, right-click `Pocket_Librarian.Server`.
2. Select **Set as Startup Project**.
3. Select the `https` profile beside the green arrow.
4. Click the green arrow.

Leave Visual Studio running. The backend normally listens at:

```text
https://localhost:7080
```

### 2. Start the React frontend

Double-click `Start-Client.cmd` in the repository’s top-level folder.

The first run installs the client packages automatically. The terminal will display a local website address, usually similar to:

```text
https://localhost:62171/
```

Open that address in a browser and leave the terminal window running.

### Important

The green-arrow setup is the intended workflow. `Start-Client.cmd` is only a fallback for a computer whose Visual Studio JavaScript tools are not configured correctly.

## Stopping the project

- Stop the backend by clicking the red square in Visual Studio.
- Stop the frontend by closing its terminal window or pressing `Ctrl+C` in that terminal.

## Troubleshooting

- If the client reports that Node.js is missing, install the Node.js LTS version and double-click `Start-Client.cmd` again.
- If the page says `API request failed`, make sure the backend is still running in Visual Studio.
- Do not commit `node_modules`, `bin`, `obj`, or `.vs` to GitHub. They are generated or computer-specific files.
