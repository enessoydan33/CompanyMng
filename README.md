\# CompanyMng



CompanyMng is a multi-tenant project and task management REST API built with ASP.NET Core.



The application allows companies to manage users, projects, project members, tasks, comments, and task workflows while keeping each company's data isolated.



\## Features



\- JWT authentication and refresh token rotation

\- ASP.NET Core Identity

\- Role-based authorization (Admin / User)

\- Multi-tenant company data isolation

\- Project management

\- Project member management

\- Task creation and assignment

\- Task status workflow

\- Task status history / audit trail

\- Task comments

\- Task filtering and pagination

\- FluentValidation

\- Global exception handling with Problem Details

\- PostgreSQL database

\- Integration tests

\- Docker support

\- GitHub Actions CI



\## Tech Stack



\- ASP.NET Core Web API (.NET 9)

\- Entity Framework Core

\- PostgreSQL

\- Neon PostgreSQL

\- ASP.NET Core Identity

\- JWT Bearer Authentication

\- FluentValidation

\- xUnit

\- SQLite In-Memory (Integration Tests)

\- Docker

\- GitHub Actions

\- Swagger / OpenAPI



\## Architecture



Each user belongs to a company.



Company data is isolated using the `CompanyId` claim contained in the authenticated user's JWT.



Resources such as projects, tasks, project members, and comments are validated against the authenticated user's company to prevent cross-tenant access.



Example:



```text

Company A

&#x20;├── Users

&#x20;├── Projects

&#x20;│    ├── Members

&#x20;│    └── Tasks

&#x20;│         ├── Comments

&#x20;│         └── Task History



Company B

&#x20;├── Users

&#x20;└── Projects

```



A user from Company B cannot access resources belonging to Company A.



\## Authentication



The API uses JWT access tokens together with refresh tokens.



Authentication flow:



```text

Register / Login

&#x20;      ↓

Access Token + Refresh Token

&#x20;      ↓

Authenticated API Requests

&#x20;      ↓

Access Token expires

&#x20;      ↓

Refresh Token

&#x20;      ↓

New Access Token + New Refresh Token

```



Refresh tokens are stored as hashes in the database.



Refresh token rotation is implemented, meaning a used refresh token is revoked when a new token is generated.



Logging in again also revokes previous active refresh tokens for that user.



\## Authorization



The application contains two roles:



\### Admin



Admins can:



\- Create and manage projects

\- Create users within their company

\- Manage project members

\- Create, update, assign, and delete tasks

\- Manage task workflows



\### User



Users can:



\- Access authorized company resources

\- Update tasks assigned to them

\- Create comments

\- Manage their own comments



Task status updates can be performed by the assigned user or an Admin.



\## Task Management



Tasks support:



\- Assignment to project members

\- Priority levels

\- Status workflow

\- Due dates

\- Filtering

\- Search

\- Pagination

\- Status history



Task assignment is restricted to users who are members of the corresponding project.



\## Multi-Tenant Security



Tenant isolation is enforced on the backend.



The API does not trust `CompanyId` values supplied by clients. Instead, CompanyId is retrieved from the authenticated JWT.



Cross-company resource requests return a safe response without exposing another company's data.



Integration tests verify this behavior.



\## Integration Tests



The project contains an integration test project:



```text

CompanyMng.IntegrationTests

```



Tests use:



\- xUnit

\- `WebApplicationFactory<Program>`

\- SQLite in-memory database

\- Real HTTP requests against the API



Current integration scenarios include:



\- Register and login

\- Admin authorization

\- Project creation

\- Project membership

\- Task assignment

\- Task status authorization

\- Task history

\- Comment authorization

\- Cross-tenant isolation

\- Refresh token rotation

\- Previous refresh token revocation



Run the tests with:



```bash

dotnet test

```



\## Docker



Build the image:



```bash

docker build -t companymng-api .

```



Create a local `.env` file:



```env

ConnectionStrings\_\_DefaultConnection=YOUR\_CONNECTION\_STRING

Jwt\_\_Key=YOUR\_JWT\_SECRET\_KEY

Jwt\_\_Issuer=CompanyMngApi

Jwt\_\_Audience=CompanyMngClient

Jwt\_\_AccessTokenMinutes=30

```



> Do not commit the `.env` file. It is excluded from Git.



Run the container:



```bash

docker run --name companymng-api \\

&#x20; -p 8080:8080 \\

&#x20; --env-file .env \\

&#x20; companymng-api

```



For local Swagger testing:



```bash

docker run --name companymng-api \\

&#x20; -p 8080:8080 \\

&#x20; --env-file .env \\

&#x20; -e ASPNETCORE\_ENVIRONMENT=Development \\

&#x20; companymng-api

```



Swagger:



```text

http://localhost:8080/swagger

```



\## Configuration



Sensitive configuration values are not stored in the repository.



For local development, use .NET User Secrets.



Example:



```bash

dotnet user-secrets set "ConnectionStrings:DefaultConnection" "YOUR\_CONNECTION\_STRING"

dotnet user-secrets set "Jwt:Key" "YOUR\_JWT\_SECRET\_KEY"

```



For containers and deployment environments, use environment variables.



\## CI



GitHub Actions automatically runs the following pipeline on pushes and pull requests to `main`:



```text

Checkout

&#x20;  ↓

Setup .NET 9

&#x20;  ↓

Restore

&#x20;  ↓

Build

&#x20;  ↓

Integration Tests

```



The workflow is located at:



```text

.github/workflows/ci.yml

```



\## Running Locally



Clone the repository and restore dependencies:



```bash

dotnet restore

```



Configure the required secrets:



```bash

dotnet user-secrets set "ConnectionStrings:DefaultConnection" "YOUR\_CONNECTION\_STRING" --project CompanyMng

dotnet user-secrets set "Jwt:Key" "YOUR\_JWT\_SECRET\_KEY" --project CompanyMng

```



Run the API:



```bash

dotnet run --project CompanyMng

```



Then open the Swagger URL shown in the terminal.



\## Project Structure



```text

CompanyMng/

├── Controllers/

├── Data/

├── Dto/

├── Entities/

├── Enum/

├── Middleware/

├── Migrations/

├── Services/

└── Validators/



CompanyMng.IntegrationTests/

├── ApiClient.cs

├── AuthFlowTests.cs

├── ProjectTaskCommentTests.cs

└── TenantIsolationTests.cs



.github/

└── workflows/

&#x20;   └── ci.yml



Dockerfile

CompanyMng.sln

```



\## Security Highlights



\- Password hashing through ASP.NET Core Identity

\- JWT signature and lifetime validation

\- Role-based authorization

\- Tenant-level data isolation

\- Server-side ownership and membership validation

\- Refresh tokens stored as hashes

\- Refresh token rotation and revocation

\- DTO-based input models

\- FluentValidation

\- Global exception handling

\- Secrets excluded from source control



\## License



This project was developed as a backend portfolio project.

