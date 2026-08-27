# SampleApi — Modern .NET 8 REST API (No Swagger)

A lightweight, modern, clean‑architecture Web API built with **.NET 8**, **ASP.NET Core**, **Azure SQL**, **EF Core**, and **JWT Authentication**.  
Designed for clarity, testability, and cloud‑ready deployment using **Docker**, **Azure Container Apps**, **Azure DevOps CI/CD**, and **secure secret management**.

---

## ✨ Features

- Modern **.NET 8** hosting model (Minimal Hosting)
- Clean **RESTful** controller structure
- **JWT Authentication** with Bearer tokens
- **EF Core + Azure SQL** (production database)
- **Repository + Service Layer** architecture
- **DTOs + Validation Layer**
- **ServiceResult** pattern for consistent responses
- **Password hashing** for secure credential storage
- **Connection string stored as a secret in Azure Container Apps**
- **Full Azure DevOps CI/CD pipeline** (build → test → Docker → ACR → ACA deploy)
- **Dockerized** application for cloud deployment
- **No Swagger / OpenAPI** (cleaner architecture)
- Fully testable using **Postman**, **Insomnia**, or any REST client

---

## 🛠 Tech Stack

- **.NET 8**
- **ASP.NET Core Web API**
- **Minimal Hosting Model**
- **C# 13**
- **Azure SQL (Production Database)**
- **Azure Container Apps (ACA)**
- **Azure Container Registry (ACR)**
- **Azure DevOps CI/CD Pipeline**
- **Docker (Containerized Deployment)**

---

## 🧱 Architecture Overview

### Repository Pattern  
Encapsulates persistence logic and keeps data access testable.

### Service Layer  
Contains business logic and orchestrates operations between repositories, validators, and DTOs.

### DTOs  
Prevent domain models from leaking to API consumers.

### Validation Layer  
Ensures business rules are consistently enforced.

### ServiceResult Pattern  
Provides standardized responses:  
- Success  
- Data  
- Errors  

### JWT Authentication  
Secures protected endpoints using Bearer tokens.

### Password Hashing + Salting  
Passwords are never stored in plaintext.  
Each user receives a unique salt, and the hashed password is stored securely.

Example stored fields:
Id: seed-user-1
Email: test@example.com
Salt: somesalt
PasswordHash: ef92b778ba5c9c3a5e8f1a9e4f4e8e2b6d5c1f2a3b4c5d6e7f8a9b0c1d2e3f4


---

## 📁 Project Structure

<pre>
SampleApi/
├── SampleApi/
│   ├── Controllers/
│   │     ├── AuthController.cs
│   │     └── MoviesController.cs
│   ├── Data/
│   │     └── AppDbContext.cs
│   ├── DTO/
│   │     ├── Auth/
│   │     │   ├── AuthResponseDTO.cs
│   │     ├── LoginDTO.cs
│   │     ├── MovieDTOAdd.cs
│   │     ├── MovieDTORead.cs
│   │     └── MovieDTOUpdate.cs
│   ├── Extensions/
│   │     └── SwaggerExtensions.cs
│   ├── Interfaces/
│   │      ├── IAuthService.cs
│   │      ├── IMovieRepository.cs
│   │      ├── IMovieService.cs
│   │      ├── IValidator.cs
│   │      ├── IUserRepository.cs
│   │      └── IPasswordHasher.cs
│   ├── Mappings/
│   │      └── MovieMapping.cs
│   ├── Models/
│   │      ├── Movie.cs
│   │      └── User.cs
│   ├── Repositories/
│   │      ├── MovieRepository.cs
│   │      └── UserRepository.cs
│   ├── Results/
│   │      └── ServiceResults.cs
│   ├── Services/
│   │      ├── AuthServices/
│   │      │   ├── AuthService.cs
│   │      │   ├── PasswordHasher.cs
│   │      └── MovieService.cs
│   ├── Validators/
│   │      ├── MovieValidator.cs
│   │      └── ValidationResult.cs
│   ├── Program.cs
│   ├── SampleApi.csproj
│   └── Properties/
│          └── launchSettings.json
└── SampleApi.Test/
</pre>

---

## 🚀 Running the Project

### 1. Restore packages  

dotnet restore


### 2. Run the API  

dotnet run


### 3. API Base URL  
Check the console output for the port, then open:  

http://localhost:<port>


---

## 🔐 Authentication (JWT)

Swagger has been removed, so authentication is performed using Postman or similar tools.

### Register the JWT Signing Key (Required)

#### 1. Navigate to the project folder  

cd SampleApi


#### 2. Initialize User Secrets  

dotnet user-secrets init


#### 3. Add the JWT signing key  

dotnet user-secrets set "Jwt:Key" "your-super-secret-key-here"


#### 4. (Optional) Add issuer and audience  

dotnet user-secrets set "Jwt:Issuer" "YourApi"
dotnet user-secrets set "Jwt:Audience" "YourApiClient"


#### 5. Verify stored secrets  

dotnet user-secrets list


### How the API reads these values  

var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = jwtSettings.GetValue<string>("Key")
?? throw new Exception("JWT Key is missing in configuration");


---

## 🎬 Movies API Endpoints

All movie endpoints require a valid JWT token.

### ➕ Create a Movie  
POST `/api/movies`  
Headers:  
- Authorization: Bearer <token>  
- Content-Type: application/json  

### 📄 Get All Movies  
GET `/api/movies`

### 🔍 Get Movie by ID  
GET `/api/movies/{id}`

### ✏️ Update Movie  
PUT `/api/movies/{id}`  
Headers:  
- Authorization: Bearer <token>  
- Content-Type: application/json  

### ❌ Delete Movie  
DELETE `/api/movies/{id}`  
Headers:  
- Authorization: Bearer <token>

---

## 🗄️ Azure SQL Database Integration

This project uses an **Azure SQL Database** as the production data store.  
The connection string is stored securely in **Azure Container Apps** as a secret.

### Connection String Secret  
Secret name:

defaultconnectionstring

Injected via environment variable:

ConnectionStrings__sampleApi = secretref:defaultconnectionstring


EF Core automatically reads this value through the standard `ConnectionStrings` configuration pattern.

---

## 🧪 Tests

Unit tests are located in the `SampleApi.Test` project.

### ✔ What is tested

- **GetAll**  
- **GetById**  
- **Add**  
- **Update**  
- **Delete**

### ✔ Tools & Patterns Used

- **Moq** for mocking dependencies  
- **Callback capture** for mapping verification  
- **Arrange–Act–Assert** structure  
- **Repository interaction verification**  
- **ServiceResult<T>** assertions  

### ✔ Run the tests  

dotnet test


---

## 🔄 Azure DevOps CI/CD Pipeline

This project includes a full **Azure DevOps CI/CD pipeline** that builds, tests, packages, and deploys the API automatically to **Azure Container Apps**.

### Pipeline Workflow

1. Restore & Build  
2. Run Unit Tests + Coverage  
3. Docker Image Build  
4. Push Image to ACR  
5. Inject Secrets  
6. Deploy to ACA (zero‑downtime rollout)

### Coverage Report  
A coverage report is generated in Azure:  
https://mongolia144.github.io/SampleApi/coverage-report/index.html

---
## Swagger

Swagger is available in **Development** and **Staging (UAT)** environments and disabled in **Production**.

### Environments

- **Development (local)**  
  - URL: `http://localhost:<port>/swagger`  
  - When running via `dotnet run` or Docker.

- **Staging (Azure UAT)**  
  - URL: `https://<your-aca-staging-url>/swagger`  
  - Runs in Azure Container Apps with `ASPNETCORE_ENVIRONMENT=Staging`.

- **Production (Azure)**  
  - Swagger is disabled for security reasons.

### Application configuration

```
if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
```
Azure Container Apps environment
Set the following environment variable in the Staging/UAT container app:

ASPNETCORE_ENVIRONMENT=Staging


# 📘 SampleApi – Recent Updates

## 🧩 User Registration & Application Logging
The API now includes a **user registration endpoint** and structured **application‑level logging** inside the controllers.

### Logged Events
The following events are emitted using ASP.NET Core’s `ILogger<T>`:

- **UserRegistered** — when a new user successfully registers  
- **UserRegistrationFailed** — when registration fails  
- **UserLoggedIn** — when a user successfully authenticates  
- **UserLoginFailed** — when authentication fails  

### Where Logs Appear
- **Local debugging:** logs appear in the Visual Studio *Output* window or terminal  
- **Azure deployment:** logs appear in  
  - Azure Monitor  
  - Log Analytics Workspace  
  - `ContainerAppConsoleLogs` table  

To enable Azure logging, configure diagnostics:

- Container App → Monitoring → Diagnostics  
- Enable **ContainerAppConsoleLogs → Log Analytics Workspace**

---

# 📘 SampleApi – Recent Updates

## 🧩 User Registration & Application Logging
The API now includes a **user registration endpoint** and structured **application‑level logging** inside the controllers.

### Logged Events
The following events are emitted using ASP.NET Core’s `ILogger<T>`:

- **UserRegistered** — when a new user successfully registers  
- **UserRegistrationFailed** — when registration fails  
- **UserLoggedIn** — when a user successfully authenticates  
- **UserLoginFailed** — when authentication fails  

### Where Logs Appear
- **Local debugging:** logs appear in the Visual Studio *Output* window or terminal  
- **Azure deployment:** logs appear in  
  - Azure Monitor  
  - Log Analytics Workspace  
  - `ContainerAppConsoleLogs` table  

To enable Azure logging, configure diagnostics:

- Container App → Monitoring → Diagnostics  
- Enable **ContainerAppConsoleLogs → Log Analytics Workspace**

---

## 🗄 Azure SQL Auditing (Optional)
The project supports viewing SQL‑level audit logs (e.g., SELECT, INSERT, UPDATE) in the **Log Analytics Workspace**.

### How to Enable SQL Auditing
To send SQL audit events to Log Analytics:

### 1. Configure auditing at the **Azure SQL Server** level  
- Azure Portal → SQL Server → Security → Auditing  
- Enable auditing  
- Select your Log Analytics Workspace

### 2. Configure auditing at the **Azure SQL Database** level  
- Azure Portal → SQL Database → Security → Auditing  
- Enable auditing  
- Select the same workspace

Once configured, SQL audit events appear in:

```
SQLSecurityAuditEvents
```

inside the Log Analytics Workspace.

---

## 🛠 EF Core Migrations for Production
The project includes updated **EF Core migrations** to support schema evolution in production environments.

### Migration Workflow
- Generate migrations:  
  `dotnet ef migrations add <Name>`
- Apply migrations:  
  `dotnet ef database update`
- Azure SQL fully supported  
- Latest migration set stored in the `azure-migrations` folder

This ensures production databases can be updated safely and consistently.

---

## 🐳 Local Development with Docker Compose
The repository includes:

- `docker-compose.yml`  
- `docker-compose.override.yml`  
- `.env` file for environment variables  

### Features
- Runs the API locally inside Docker containers  
- Mirrors the Azure Container Apps environment  
- Stores configuration values (connection strings, JWT secrets, etc.) in `.env`  
- Supports debugging through Visual Studio or CLI

This setup ensures your local environment behaves like your cloud environment.

---

## 📈 Future Improvements

- Add role‑based authorization  
- Add registration  
- Expand unit tests  
- Add API versioning  

---

## 📜 License

This project is open‑source and free to use for learning or portfolio purposes.
