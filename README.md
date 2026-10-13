# Event Management App

A web app for browsing events and registering as an attendee. Organizers create and manage events; attendees see live updates when someone registers.

Course project for CST8359 (.NET Enterprise Applications), Algonquin College. No longer hosted (Azure costs)

## Features

- Public event list; event details and attendee list require login
- Two roles: **Organizer** (create, edit, delete events; manage attendees) and **Attendee** (browse, register, unregister)
- Banner image upload to Azure Blob Storage (the database stores only the URL)
- Real-time attendee count and organizer notifications with SignalR
- Role-based authorization and anti-forgery tokens on all form posts

## Tech stack

ASP.NET Core 8 MVC, Entity Framework Core, SQL Server (Azure SQL), ASP.NET Core Identity, Azure Blob Storage, SignalR. Deployed on Azure App Service.

## Data model

One `Event` has many `Attendees`. An `Attendee` holds an `EventId` and an optional `UserId` (the logged-in account).

## Running locally

1. Install the .NET 8 SDK and have a SQL Server instance available.
2. Set your settings with user secrets:
```
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<your connection string>"
   dotnet user-secrets set "AzureStorage:ConnectionString" "<your storage connection string>"
   dotnet user-secrets set "AzureStorage:ContainerName" "<your container>"
```
3. Apply the migrations: `dotnet ef database update`
4. Run: `dotnet run`

On first run the app seeds sample events, the two roles, and two demo accounts: `organizer@example.com` and `attendee@example.com`.

## Known limitations / what I'd improve

- Any Organizer can edit or delete any event (add an ownership check)
- Add a unique index on EventId + UserId to prevent duplicate registrations
- Add automated tests and model validation
- Restrict who can see attendee emails
