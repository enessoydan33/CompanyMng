# CompanyMng

Multi-tenant project management REST API built with ASP.NET Core, PostgreSQL, JWT authentication, Docker, and automated integration tests.

Companies register with an admin account, invite users, and manage projects, members, tasks, comments, and status history. Tenant data is isolated by `CompanyId` taken from the authenticated JWT, not from the client request body.

## Key Features

- Multi-tenant architecture (user belongs to one company)
- JWT access tokens and hashed refresh tokens
- Refresh token rotation and revocation on reuse and re-login
- ASP.NET Core Identity with Admin and User roles
- Company-level data isolation on projects, tasks, members, and comments
- Project create / update / delete (Admin)
- Project member add / remove (Admin)
- Task create / update / assign / delete (Admin)
- Task assignment limited to project members
- Task status updates by Admin or the assigned user
- Task history / audit trail
- Comments with owner-or-Admin delete
- Task list pagination, filtering, and title search
- FluentValidation on request DTOs
- Global exception handling with ProblemDetails
- PostgreSQL (Neon-compatible)
- Docker image
- xUnit integration tests via `WebApplicationFactory`
- GitHub Actions CI (restore, build, test)

## Tech Stack

**Backend**
- .NET 9
- ASP.NET Core Web API
- Entity Framework Core
- ASP.NET Core Identity

**Database**
- PostgreSQL
- Neon

**Security**
- JWT Bearer authentication
- Refresh token hashing and rotation
- Role-based authorization
- Multi-tenant isolation via JWT `CompanyId`

**Testing / DevOps**
- xUnit
- `WebApplicationFactory<Program>`
- SQLite in-memory integration tests
- Docker
- GitHub Actions

## Architecture / Multi-Tenancy

```text
User ──► Company
              │
              └── Projects
                    ├── Members
                    └── Tasks
                          ├── Comments
                          └── TaskHistory
```

`CompanyId` is not taken from the client. After login, it is issued as a JWT claim and used on resource queries.

Cross-tenant reads and writes are rejected with **404** so another company's data is not confirmed or returned.

## Authentication Flow

```text
Register / Login
        ↓
Access Token + Refresh Token
        ↓
Authenticated requests
        ↓
POST /api/Auth/refresh
        ↓
Old refresh token revoked
        ↓
New access token + new refresh token
```

Refresh tokens are stored as hashes, not plaintext. A second login revokes previously active refresh tokens for that user. Logout revokes the presented refresh token.

## Authorization

Roles: **Admin** (created on register) and **User** (created by an Admin in the same company).

| Action | Admin | User |
|---|---|---|
| Create company users | Yes | No |
| Create / update / delete projects | Yes | No |
| Add / remove project members | Yes | No |
| Create / update / assign / delete tasks | Yes | No |
| List tasks for a project | Yes | No |
| Get a task by id (same company) | Yes | Yes |
| Change task status | Yes | Only if assigned |
| Create / list comments | Yes | Yes |
| Delete a comment | Same-company comments | Own comments only |

Project member listing is available to authenticated users in the same company. It is not a separate membership-only ACL.

## API Modules

Base path: `/api/{controller}`

| Method | Endpoint | Description | Authorization |
|---|---|---|---|
| POST | `/api/Auth/register` | Create company + first Admin | Anonymous |
| POST | `/api/Auth/login` | Issue access and refresh tokens | Anonymous |
| POST | `/api/Auth/refresh` | Rotate refresh token | Anonymous |
| POST | `/api/Auth/logout` | Revoke refresh token | Anonymous |
| POST | `/api/Auth/CreateUser` | Create a User in the current company | Admin |
| POST | `/api/Project/create` | Create project | Admin |
| GET | `/api/Project` | List projects | Authenticated |
| GET | `/api/Project/{id}` | Get project | Authenticated |
| PUT / DELETE | `/api/Project/{id}` | Update / delete project | Admin |
| POST | `/api/ProjectMember/{projectId}` | Add member | Admin |
| GET | `/api/ProjectMember/{projectId}` | List members | Authenticated |
| DELETE | `/api/ProjectMember/{projectId}/{userId}` | Remove member | Admin |
| POST | `/api/Task/{projectId}` | Create task | Admin |
| GET | `/api/Task/project/{projectId}` | List tasks (filter / page / search) | Admin |
| GET | `/api/Task/{id}` | Get task | Authenticated |
| PUT | `/api/Task/{taskId}` | Update task | Admin |
| PATCH | `/api/Task/{taskId}/assign` | Assign task | Admin |
| PATCH | `/api/Task/{taskId}/status` | Change status | Admin or assignee |
| DELETE | `/api/Task/{taskId}` | Delete task | Admin |
| GET | `/api/Task/{taskId}/history` | Status history | Authenticated |
| POST / GET | `/api/Comment/{taskId}` | Create / list comments | Authenticated |
| DELETE | `/api/Comment/{commentId}` | Delete comment | Owner or Admin |

Swagger UI is enabled only when `ASPNETCORE_ENVIRONMENT=Development`.

## Integration Tests

`CompanyMng.IntegrationTests` runs real HTTP requests against the API using xUnit, `WebApplicationFactory<Program>`, and an isolated SQLite in-memory database. Production credentials are not used.

The suite includes **9 tests** covering:

- Register and login
- Admin-only project and task creation
- Project membership and duplicate-member rejection
- Task assignment to members only
- Task status authorization (assignee vs other user vs Admin)
- Task history
- Comment create / list / owner delete / Admin delete
- Cross-tenant isolation (404, no leaked payload)
- Refresh token rotation and re-login revocation

```bash
dotnet test
```

## Docker

```bash
docker build -t companymng-api .
```

Create a local `.env` file (do not commit it):

```env
ConnectionStrings__DefaultConnection=YOUR_CONNECTION_STRING
Jwt__Key=YOUR_JWT_SECRET_KEY
Jwt__Issuer=CompanyMngApi
Jwt__Audience=CompanyMngClient
Jwt__AccessTokenMinutes=30
```

```bash
docker run --name companymng-api \
  -p 8080:8080 \
  --env-file .env \
  companymng-api
```

The published image listens on port **8080**. Swagger is off in Production. For local Swagger:

```bash
docker run --name companymng-api \
  -p 8080:8080 \
  --env-file .env \
  -e ASPNETCORE_ENVIRONMENT=Development \
  companymng-api
```

Then open `http://localhost:8080/swagger`.

## Configuration

Local development: .NET User Secrets.

Docker / production: environment variables.

Connection strings and JWT signing keys are not stored in this repository.

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "YOUR_CONNECTION_STRING" --project CompanyMng
dotnet user-secrets set "Jwt:Key" "YOUR_JWT_SECRET_KEY" --project CompanyMng
```

Issuer, audience, and access-token lifetime can stay in `appsettings.json`; only secrets belong in User Secrets or the environment.

## CI

`.github/workflows/ci.yml` runs on **push** and **pull request** to `main`:

1. Checkout
2. Setup .NET 9
3. Restore
4. Build (Release)
5. Test (Release)

## Getting Started

```bash
git clone <repository-url>
cd CompanyMng
dotnet restore
```

Configure PostgreSQL and JWT secrets (see Configuration), then:

```bash
dotnet run --project CompanyMng
dotnet test
```

In Development, use the Swagger URL printed by `dotnet run`.

## Project Structure

```text
CompanyMng.sln
Dockerfile
.github/workflows/ci.yml
CompanyMng/
├── Controllers/
├── Data/
├── Dto/
├── Entities/
├── Enum/
├── Middleware/
├── Migrations/
├── Services/
├── Validators/
└── Program.cs
CompanyMng.IntegrationTests/
├── CustomWebApplicationFactory.cs
├── ApiClient.cs
├── AuthFlowTests.cs
├── ProjectTaskCommentTests.cs
└── TenantIsolationTests.cs
```

## Security Highlights

- Identity password hashing
- JWT issuer, audience, lifetime, and signing-key validation (`ClockSkew = TimeSpan.Zero`)
- Role-based authorization on mutating project/task/member endpoints
- Tenant isolation from JWT `CompanyId`
- Task assignment only to project members
- Refresh tokens stored as hashes, rotated, and revoked
- DTO + FluentValidation (no entity mass assignment of tenant/role fields)
- Global exception middleware with generic ProblemDetails (no stack traces to clients)

This repository is a backend portfolio project.
