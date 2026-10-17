# Form Builder

A web application for creating, publishing, and managing forms and submissions.

## Technology Stack

### Frontend
- React
- TypeScript
- Vite
- ESLint

### Backend
- ASP.NET Core Web API
- C# and .NET 10
- Controller-based API

### Database
- PostgreSQL
- Entity Framework Core (planned)

## Project Structure

```text
Form Builder/
├── client/                 # React frontend
├── server/
│   └── FormBuilder.Api/    # ASP.NET Core Web API
├── docs/                   # Project documentation
├── .gitignore
└── README.md
```

## Development

### Frontend

```powershell
cd client
npm run dev
```

### Backend

From the project root:

```powershell
dotnet run --project .\server\FormBuilder.Api
```

## Project Status

Initial project setup is complete. Application features, database integration, authentication, and testing will be implemented incrementally.