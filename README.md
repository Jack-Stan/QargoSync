# QargoSync

A tool for synchronizing unavailability data between Qargo environments.

## Prerequisites
- .NET 8.0 SDK
- Visual Studio 2022 or VS Code

## Setup
1. Clone the repository
2. Run `dotnet restore` in the root folder
3. Configure your API credentials in `appsettings.json`
4. Run `dotnet run --project src/QargoSync.App`

## Project Structure
- `src/QargoSync.App` - Console application
- `src/QargoSync.Core` - Business logic
- `src/QargoSync.Infrastructure` - API clients
- `src/QargoSync.Models` - Data models
- `tests/` - Unit and integration tests

## Design Patterns
- Repository Pattern for data access abstraction
- Dependency Injection for loose coupling
- Options Pattern for configuration