# DevOps Task - .NET CI/CD Pipeline

This repository contains my solution for the DevOps task. The objective was to design and implement a CI/CD (Continuous Integration and Continuous Deployment) process for a Windows-hosted .NET application, define an appropriate branching strategy, and support separate Test and Production deployments.

The main focus of my work was the release and deployment process rather than changing the business functionality of the sample API.

HELLO

## Table of Contents

- [Source Project and Attribution](#source-project-and-attribution)
- [Solution Summary](#solution-summary)
- [Repository Structure](#repository-structure)
- [Branching Strategy](#branching-strategy)
- [CI/CD Flow](#cicd-flow)
- [GitHub Actions Workflows](#github-actions-workflows)
- [Environment Strategy](#environment-strategy)
- [Configuration and Secrets](#configuration-and-secrets)
- [Deployment Process](#deployment-process)
- [Validation and Deployment Precautions](#validation-and-deployment-precautions)
- [Local Setup](#local-setup)
- [GitHub Environment Configuration](#github-environment-configuration)
- [Windows Deployment Host Setup](#windows-deployment-host-setup)
- [Challenges Encountered](#challenges-encountered)
- [Assumptions and Limitations](#assumptions-and-limitations)
- [Potential Improvements](#potential-improvements)

## Source Project and Attribution

This repository was forked from:

- Original repository: [kawser2133/web-api-project](https://github.com/kawser2133/web-api-project)
- Assessment repository: [Kryptic869/web-api-project](https://github.com/Kryptic869/web-api-project)

The original repository (by kawser2133) was an ASP.NET Core Web API using a layered structure, Entity Framework Core, SQL Server, JWT authentication, API versioning, and automated tests.

For this assessment, I retained the sample application's existing business functionality and focused on:

- Upgrading and configuring the solution for .NET 10;
- Creating environment-specific application settings;
- Enabling Windows Service hosting;
- Adding a health endpoint;
- Defining a branching strategy;
- Implementing CI validation;
- Implementing Test and Production deployment workflows;
- Creating database migration bundles;
- Using a self-hosted Windows runner as the deployment agent;
- Validating the deployed service and a database-backed endpoint;
- Separating environment secrets and variables through GitHub Environments.

## Solution Summary

The solution uses **GitHub Actions** for automation and a **self-hosted Windows runner** for local deployment.

Two environments are hosted on the same Windows machine as separate applications:

| Environment | Branch    | Windows Service     | Deployment Path             | URL                     |
| ----------- | --------- | ------------------- | --------------------------- | ----------------------- |
| Test        | `develop` | `WebApi-Test`       | `C:\Apps\WebApi\Test`       | `http://localhost:5100` |
| Production  | `main`    | `WebApi-Production` | `C:\Apps\WebApi\Production` | `http://localhost:5200` |

The GitHub-hosted runner performs the compilation, tests, publishing, and migration-bundle creation. The self-hosted runner only receives the completed release artifact and deploys it to the appropriate Windows Service.

This separation means that compilation is performed in a clean build environment, while the deployment agent is responsible only for controlled access to the target machine.

## Repository Structure

```text
.
├── .github/
│   └── workflows/
│       ├── ci.yml
│       ├── deploy-test.yml
│       ├── deploy-production.yml
│       └── runner-smoke-test.yml
├── Project.API/
├── Project.Core/
├── Project.Infrastructure/
├── Project.UnitTest/
├── docs/
├── WebApiProject.sln
└── README.md
```

### Application projects

| Project                  | Responsibility                                                                                         |
| ------------------------ | ------------------------------------------------------------------------------------------------------ |
| `Project.API`            | HTTP API, controllers, middleware, startup configuration, health endpoint, and Windows Service hosting |
| `Project.Core`           | Domain models, interfaces, services, and application logic                                             |
| `Project.Infrastructure` | Entity Framework Core, SQL Server access, repositories, migrations, and infrastructure services        |
| `Project.UnitTest`       | Automated tests for the API, Core, and Infrastructure layers                                           |

## Branching Strategy

I adopted a lightweight environment-based branching strategy:

- `feature/*` and `fix/*` branches are created from `develop`;
- Developers work independently on separate changes;
- Each change is submitted through a pull request into `develop`;
- The CI workflow validates the pull request before merging;
- A successful merge into `develop` automatically deploys to Test;
- After the Test environment has been validated, `develop` is merged into `main` through a pull request;
- A successful merge into `main` automatically deploys to Production.

This approach supports multiple developers or work streams without allowing unfinished work to move directly into Production.

In an organisational setup, both `develop` and `main` should be protected branches. Pull requests, successful CI checks, and reviewer approval should be required before merging. These may not have all been enabled in this repository as I needed to merge myself to `main`. 8

## CI/CD Flow

<img width="511" height="1461" alt="CICD Pipeline Flowchart - Vertical" src="https://github.com/user-attachments/assets/fe61fbcf-ca3e-4d66-8d04-6882ac1c7d84" />

## GitHub Actions Workflows

### `ci.yml`

The CI workflow runs on:

- pull requests targeting `develop` or `main`;
- pushes to `develop` or `main`.

It performs the following steps:

1. Checks out the repository;
2. Installs the .NET 10 SDK;
3. Restores NuGet dependencies;
4. Builds the solution in Release configuration;
5. Runs the automated tests;
6. Publishes the API;
7. Uploads the published application as a workflow artifact.

This provides early validation before code is merged or deployed.

### `deploy-test.yml`

The Test deployment workflow runs when code is pushed to `develop`. It can also be started manually through `workflow_dispatch`.

The workflow:

1. Restores, builds, and tests the solution on a GitHub-hosted Windows runner;
2. Publishes the API;
3. Generates an Entity Framework Core migration bundle;
4. Uploads a single Test release artifact;
5. Downloads the artifact on the self-hosted Windows runner;
6. Validates all required deployment variables and secrets;
7. Checks that the Test Windows Service exists;
8. Stops the Test service and waits for its process to exit;
9. Creates a timestamped backup of the existing deployment;
10. Replaces the deployed application files;
11. Injects the Test database connection string and JWT secret;
12. Applies database migrations using the migration bundle;
13. Starts the Test Windows Service;
14. Retries the health check until the application is ready;
15. Validates a database-backed endpoint.

### `deploy-production.yml`

The Production workflow follows the same controlled process as Test but runs when code is pushed to `main`.

The environment-specific values ensure that it deploys to the Production path, uses the Production service, loads `appsettings.Production.json`, and listens on the Production port.

Using comparable Test and Production workflows makes the deployment behaviour predictable between environments.

### `runner-smoke-test.yml`

This manually triggered workflow validates the self-hosted deployment runner before relying on it for a release.

It checks:

- The Windows account used by the runner;
- The computer name;
- The PowerShell version;
- Installed .NET SDKs;
- Read and write access to the Test deployment directory.

## Environment Strategy

ASP.NET Core configuration is separated into the following files:

| File                           | Purpose                                                                    |
| ------------------------------ | -------------------------------------------------------------------------- |
| `appsettings.json`             | Shared non-sensitive defaults                                              |
| `appsettings.Development.json` | Local development configuration                                            |
| `appsettings.Test.json`        | Test logging, service, JWT issuer/audience, and environment defaults       |
| `appsettings.Production.json`  | Production logging, service, JWT issuer/audience, and environment defaults |

The application is started with a specific ASP.NET Core environment:

```text
Development
Test
Production
```

The Test and Production applications use separate:

- Windows Service names;
- Deployment directories;
- Ports;
- Configuration files;
- Databases or database connection strings;
- GitHub Environment variables and secrets.

This prevents a deployment to one environment from overwriting or reconfiguring the other.

## Configuration and Secrets

Sensitive values are not stored in the Git repository.

The deployment workflows retrieve the following values from GitHub Environments:

### Secrets

| Secret                 | Purpose                                                   |
| ---------------------- | --------------------------------------------------------- |
| `DB_CONNECTION_STRING` | SQL Server connection string for the selected environment |
| `JWT_SECRET`           | Secret used to sign and validate JWT tokens               |

### Variables

| Variable                 | Purpose                                                               |
| ------------------------ | --------------------------------------------------------------------- |
| `ASPNETCORE_ENVIRONMENT` | Selects `Test` or `Production` configuration                          |
| `ASPNETCORE_URLS`        | Defines the URL and port used by the service                          |
| `DEPLOY_PATH`            | Target folder on the Windows deployment machine                       |
| `SERVICE_NAME`           | Windows Service to stop and start                                     |
| `HEALTH_URL`             | URL used for post-deployment health validation                        |
| `DB_CHECK_URL`           | URL of a database-backed endpoint used for post-deployment validation |

During deployment, the workflow writes the environment's database connection string and JWT secret into the environment-specific configuration file on the target machine. The deployment directories should therefore be accessible only to the service account, runner account, and authorised administrators.

For local development, .NET User Secrets can be used instead of storing sensitive values in `appsettings.Development.json`.

## Deployment Process

A release is built before the deployment job starts.

The release artifact contains:

```text
release/
├── app/
│   └── published API files
└── database/
    └── efBundle.exe
```

The deployment order is intentionally controlled:

```text
Validate configuration
        ↓
Stop Windows Service
        ↓
Wait for process to exit
        ↓
Back up current deployment
        ↓
Replace application files
        ↓
Inject environment configuration
        ↓
Apply database migrations
        ↓
Start Windows Service
        ↓
Validate health endpoint
        ↓
Validate database-backed endpoint
```

Database migrations are packaged as an executable bundle during the build job. This avoids requiring the full source code or the Entity Framework CLI on the deployment machine.

## Validation and Deployment Precautions

The workflows include several checks to reduce the chance of an incomplete or unsafe deployment:

- The application is not deployed unless restore, build, and tests succeed;
- Required environment variables and secrets are checked before the service is stopped;
- The target Windows Service must exist before deployment begins;
- The service is stopped and its process is confirmed to have exited before files are replaced;
- The previous deployment is backed up using a timestamped folder;
- File deletion includes retry logic to handle temporary Windows file locks;
- The expected artifact directories are validated;
- The environment-specific configuration file must exist;
- Database migration failures stop the deployment;
- The service must reach the Running state within a timeout;
- The health endpoint is retried to allow for application startup time;
- A database-backed endpoint confirms that the application can communicate with SQL Server;
- Concurrency groups prevent two deployments to the same environment from running simultaneously.

## Local Setup

### Prerequisites

- Windows, macOS, or Linux for local development;
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0);
- SQL Server or SQL Server Express;
- PowerShell for the Windows deployment-host commands;
- Git.

### 1. Clone the repository

```powershell
git clone https://github.com/Kryptic869/web-api-project.git
cd web-api-project
```

### 2. Restore and build

```powershell
dotnet restore
dotnet build
```

### 3. Configure local secrets

The API project already contains a User Secrets identifier.

```powershell
dotnet user-secrets set `
  "ConnectionStrings:PrimaryDbConnection" `
  "Server=localhost\SQLEXPRESS;Database=WebApiProject;Trusted_Connection=True;TrustServerCertificate=True;" `
  --project .\Project.API\Project.API.csproj

dotnet user-secrets set `
  "AppSettings:JwtConfig:Secret" `
  "replace-with-a-long-random-development-secret" `
  --project .\Project.API\Project.API.csproj
```

Adapt the SQL Server instance and database name to the local machine.

### 4. Install the Entity Framework CLI

```powershell
dotnet tool install --global dotnet-ef --version 7.0.9
```

When it is already installed:

```powershell
dotnet tool update --global dotnet-ef --version 7.0.9
```

### 5. Apply database migrations

```powershell
dotnet ef database update `
  --project .\Project.Infrastructure\Project.Infrastructure.csproj `
  --startup-project .\Project.API\Project.API.csproj
```

### 6. Run the application

```powershell
dotnet run --project .\Project.API\Project.API.csproj
```

The Development launch profile uses:

- API: `http://localhost:1100`
- Swagger UI: `http://localhost:1100/swagger`
- API Health endpoint: `http://localhost:1100/health`

### 7. Run automated tests

```powershell
dotnet test --configuration Release
```

## GitHub Environment Configuration

Create two GitHub Environments:

```text
Test
Production
```

Configure the following values.

### Test

```text
Secrets
DB_CONNECTION_STRING = <Test SQL Server connection string>
JWT_SECRET           = <Test JWT secret>

Variables
ASPNETCORE_ENVIRONMENT = Test
ASPNETCORE_URLS        = http://localhost:5100
DEPLOY_PATH             = C:\Apps\WebApi\Test
SERVICE_NAME            = WebApi-Test
HEALTH_URL               = http://localhost:5100/health
DB_CHECK_URL             = http://localhost:5100/api/v1/Product
```

### Production

```text
Secrets
DB_CONNECTION_STRING = <Production SQL Server connection string>
JWT_SECRET           = <Production JWT secret>

Variables
ASPNETCORE_ENVIRONMENT = Production
ASPNETCORE_URLS        = http://localhost:5200
DEPLOY_PATH             = C:\Apps\WebApi\Production
SERVICE_NAME            = WebApi-Production
HEALTH_URL               = http://localhost:5200/health
DB_CHECK_URL             = http://localhost:5200/api/v1/Product
```

For a real Production repository, the `Production` environment should use required reviewers so that a deployment cannot proceed without approval. For testing purposes, in this repository, I added myself as a reviewer.

## Windows Deployment Host Setup

The following setup is required once on the Windows machine.

### 1. Create deployment and backup directories

Run PowerShell as Administrator:

```powershell
New-Item -ItemType Directory -Force -Path "C:\Apps\WebApi\Test"
New-Item -ItemType Directory -Force -Path "C:\Apps\WebApi\Production"
New-Item -ItemType Directory -Force -Path "C:\Apps\WebApi\Backups\Test"
New-Item -ItemType Directory -Force -Path "C:\Apps\WebApi\Backups\Production"
```

### 2. Register a self-hosted GitHub Actions runner

Register the runner with the repository and assign the custom label:

```text
web-api-deploy
```

The runner must have permission to:

- Read and write the deployment and backup directories;
- Stop and start `WebApi-Test` and `WebApi-Production`;
- Execute the migration bundle;
- Access the configured SQL Server instances.

### 3. Create the Windows Services

The services require an initial published executable. After placing the application files in each deployment directory, create the services as Administrator.

```powershell
New-Service `
  -Name "WebApi-Test" `
  -BinaryPathName '"C:\Apps\WebApi\Test\Project.API.exe" --environment Test --urls http://localhost:5100' `
  -DisplayName "Web API - Test" `
  -StartupType Automatic

New-Service `
  -Name "WebApi-Production" `
  -BinaryPathName '"C:\Apps\WebApi\Production\Project.API.exe" --environment Production --urls http://localhost:5200' `
  -DisplayName "Web API - Production" `
  -StartupType Automatic
```

Start the services:

```powershell
Start-Service WebApi-Test
Start-Service WebApi-Production
```

Inspect their configuration:

```powershell
Get-CimInstance Win32_Service `
  -Filter "Name='WebApi-Test' OR Name='WebApi-Production'" |
  Format-List Name, State, StartName, PathName, ExitCode
```

Confirm that the ports are listening:

```powershell
Get-NetTCPConnection -State Listen |
Where-Object { $_.LocalPort -in 5100, 5200 } |
Select-Object LocalAddress, LocalPort, OwningProcess
```

## Challenges Encountered

### .NET SDK compatibility

The selected source project originally targeted an older .NET version. The local machine initially did not contain an SDK capable of building the updated target framework.

The solution and GitHub Actions workflows were aligned on .NET 10 so that local and CI builds use the same major SDK version.

### Environment separation

Running Test and Production on the same machine required each environment to use the correct service name, directory, ASP.NET Core environment, port, and configuration file.

Keeping these values in separate GitHub Environments reduces the risk of hard-coded Test values being used by Production.

### Windows file locking

A Windows Service may report that it has stopped while its process is still exiting and holding application files open.

The deployment workflows therefore:

- Reads the service process ID;
- Stops the service;
- Waits for the Stopped state;
- Verifies that the process has exited;
- Retries clearing the deployment folder.

### Artifact path handling

The downloaded artifact location needed to be handled consistently between the GitHub-hosted build runner and the self-hosted deployment runner.

The workflows use the path returned by `actions/download-artifact`, validate that it exists, and print the artifact structure when an expected path is missing.

### Database migration packaging

The migration bundle output directory must exist before `dotnet ef migrations bundle` runs.

The build job explicitly creates both the application and database artifact directories before publishing the release.

## Assumptions and Limitations

- The Test and Production environments are hosted on the same Windows machine for demonstration purposes.
- Each environment should use a different database, even when both SQL Server databases are hosted on the same server.
- The self-hosted runner is treated as a deployment agent and must be secured because it can manage Windows Services and write to deployment directories.
- Deployment backups are created, but rollback is currently manual.
- The Test and Production workflows each create their own artifact. A mature release process would normally build once and promote the exact same immutable artifact between environments.
- The Test and Production workflow files contain similar deployment logic. This makes the process easy to follow for the assessment, but a reusable workflow would reduce duplication.
- The current deployment replaces files in place and causes a short service interruption.
- The health and database checks validate basic availability but are not a complete end-to-end test suite.

## Potential Improvements

With additional time, I would make the following improvements:

1. **Build once and promote the same artifact**  
   Produce a versioned release artifact after CI and promote it from Test to Production without rebuilding.

2. **Reusable GitHub Actions workflow**  
   Extract the duplicated Test and Production deployment steps into one reusable workflow receiving environment-specific inputs.

3. **Production approval gate**  
   Configure required reviewers on the GitHub `Production` environment.

4. **Automated rollback**  
   Restore the previous timestamped backup and restart the service automatically when health validation fails.

5. **Artifact versioning and traceability**  
   Include the commit SHA, semantic version, and build metadata in the artifact and deployed application.

6. **Stronger security controls**  
   Use a dedicated least-privilege service account, restrict runner access, rotate secrets, and consider an external secret-management system.

7. **Improved deployment availability**  
   Introduce blue-green deployment, IIS deployment slots, containers, or a reverse proxy to reduce downtime.

8. **Observability**  
   Extend the existing logging configuration with centralised dashboards, deployment markers, metrics, and alerting.

9. **Expanded post-deployment tests**  
   Add API smoke tests covering authentication and key business operations in addition to health and database connectivity.
