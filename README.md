<p align="center">
  <img src="library-management-system-banner.png"
       alt="Library Management System"
       width="650"
       height="366">
</p>

<h1 align="center">Library Management System</h1>

<p align="center">
  A clean Windows desktop Library Management System built with C# and Windows Forms.
</p>

## Main Features

## Main Features




# Library Management System

A clean Windows desktop Library Management System built with C# and Windows Forms.

## Main features

- Secure login with a hashed password
- Change-password screen with current-password verification
- Professional dashboard with live library statistics
- Book catalogue: add, edit, delete, search, copy counts and availability
- Member directory: add, edit, delete, search and active/inactive status
- Issue and return workflow with validation
- Automatic availability updates when books are issued or returned
- Maximum of five active loans per member
- Due-date and overdue tracking
- Automatic fine calculation at Rs. 10 per overdue day
- CSV reports for books, members and loan history
- One-click database backups
- Portable per-user data storage; no SQL Server and no hard-coded database path

## Requirements

- Windows 10 or Windows 11
- Visual Studio 2022 version 17.8 or later
- The `.NET desktop development` workload
- .NET 8 SDK

## Open and run

1. Extract the ZIP file.
2. Open `LibraryManagementSystem.sln` in Visual Studio 2022.
3. Wait until Visual Studio finishes loading the project.
4. Select `Debug` and `Any CPU`.
5. Press `F5` or click the green Start button.

First login:

- Username: `admin`
- Password: `admin123`

## Build an EXE

Use Visual Studio:

1. Select `Build` > `Build Solution`.
2. The normal EXE is created in `LibraryManagementSystem\bin\Release\net8.0-windows\` when built in Release mode.

For a portable self-contained Windows x64 build, run `BUILD-PORTABLE-EXE.bat`. The output is placed in the `Publish` folder. This build command downloads the official .NET runtime files the first time it runs.

## Data location

The application automatically creates its data file here:

`%LOCALAPPDATA%\LibraryManagementSystem\library-data.json`

Use `Reports & Backup` inside the app to create backup copies. This design fixes the original project's main error: it used a database path belonging to another computer (`C:\Users\WINDOWS 10\Documents\library.mdf`).

## Reset the demo database

1. Close the application.
2. Open `%LOCALAPPDATA%\LibraryManagementSystem`.
3. Rename `library-data.json` to keep it as a backup, or delete it if it is no longer needed.
4. Start the application again. A clean demo database is created automatically.

## Project structure

- `AppDatabase.cs` — persistence, validation and business rules
- `Models.cs` — book, member, user and loan models
- `LoginForm.cs` / `MainForm.cs` — application shell and authentication
- `DashboardPage.cs` — statistics and recent circulation
- `BooksPage.cs` / `BookDialog.cs` — catalogue management
- `MembersPage.cs` / `MemberDialog.cs` — member management
- `LoansPage.cs` — issue, return, overdue and fine workflow
- `ReportsPage.cs` — CSV exports and backups
- `Theme.cs` — common professional UI styles
