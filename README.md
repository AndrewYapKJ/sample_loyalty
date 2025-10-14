# Loyalty Software

A .NET 9 multi-project solution with Blazor Server frontend and Web API backend.

## Architecture

- **App.Frontend** - Blazor Server application (port 7000)
- **App.Api** - Web API backend with authentication (port 7001)
- **App.Database** - Entity Framework Core database context and migrations
- **App.Model** - Shared data models
- **App.Common** - Common utilities and services
- **App.AppHost** - Application orchestrator (startup project)

## Getting Started

### Prerequisites

- .NET 9 SDK
- SQL Server (LocalDB or full instance)

### Running the Application

The easiest way to run the entire solution is through the AppHost:

```bash
# From the solution root
dotnet run --project src/App.AppHost/App.AppHost.csproj
```

This will start both:
- 🌐 **API**: https://localhost:7001 (with Swagger UI at `/swagger`)
- 🖥️ **Frontend**: https://localhost:7000

### Manual Startup (Alternative)

If you prefer to run services individually:

```bash
# Terminal 1 - Start the API
cd src/App.Api
dotnet run --urls "https://localhost:7001"

# Terminal 2 - Start the Frontend
cd src/App.Frontend  
dotnet run --urls "https://localhost:7000"
```

### Database Setup

The application uses Entity Framework Core with SQL Server. To set up the database:

```bash
# From the solution root
dotnet ef database update --startup-project src/App.Frontend --project src/App.Database
```

### Authentication

The application includes JWT-based authentication:
- **Username**: `principaltest`
- **Password**: `admin123`

## Project Structure

```
src/
├── App.Frontend/          # Blazor Server UI
│   ├── Components/        # Razor components
│   ├── Services/         # Frontend services  
│   └── Program.cs        # Frontend startup
├── App.Api/              # Web API backend
│   ├── Controllers/      # API controllers
│   ├── Services/         # Business logic
│   └── Program.cs        # API startup
├── App.Database/         # EF Core context
│   └── Migrations/       # Database migrations
├── App.Model/            # Shared models
├── App.Common/           # Common utilities
└── App.AppHost/          # Application orchestrator
    └── Program.cs        # Starts both API and Frontend
```

## Development

- The AppHost is configured as the startup project
- Both API and Frontend will start automatically when running the solution
- Use `Ctrl+C` to stop all services gracefully
- Swagger UI is available at https://localhost:7001/swagger for API testing

## Technologies

- .NET 9
- Blazor Server
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- JWT Authentication
- BCrypt password hashing