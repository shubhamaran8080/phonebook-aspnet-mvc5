# Phonebook - Docker Setup (ASP.NET MVC 5 / .NET Framework 4.7.2)

This document describes how to build and run the existing **Phonebook.Web**
ASP.NET MVC 5 application (targeting **.NET Framework 4.7.2**, using **Entity
Framework 6**, **ASP.NET Identity** and **SQL Server**) inside a **Windows
container** running IIS.

It is an additive wrapper around the existing solution. It does **not** change
the application, its UI, its business logic, its database or its authentication.

> **Read this first - host requirements.**
> This is a classic .NET Framework application. It **cannot** run in a Linux
> container; it needs a **Windows container** (`windowsservercore` + IIS).
> Windows containers are only available on a supported host (see
> [Prerequisites](#prerequisites)). The development machine this setup was
> created on (Windows 11 **Home**) does **not** support Windows containers, so
> the image could not be built or run there. The configuration below is written
> to Microsoft's documented pattern, but it is **unverified on this machine** -
> see [Known limitations](#known-limitations).

---

## Contents

| File | Purpose |
| --- | --- |
| `Dockerfile` | Multi-stage build: restore + compile + publish, then deploy to IIS (`aspnet:4.8`). |
| `.dockerignore` | Excludes VCS, IDE files, build output, restored packages and `.env` from the build context. |
| `docker-compose.yml` | Runs the web container and maps it to `localhost:8080`. SQL Server stays external. |
| `.env.example` | Template for the local, git-ignored `.env` file (placeholders only). |
| `docker/entrypoint.ps1` | Injects the connection string / demo-user settings from environment variables, then starts IIS. |
| `docker/clear-connectionstring.ps1` | Build-time helper that removes the development connection string from the published `Web.config` so no credential is baked into the image. |

---

## Prerequisites

### Host operating system (pick one)

Windows containers are supported on:

1. **Windows Server 2022 / 2025** - use **process isolation** when the host OS
   build matches the image OS build. Both `sdk` and `aspnet` images used here are
   `windowsservercore-ltsc2022` (**OS build 20348**).
2. **Windows 10/11 Pro, Enterprise or Education** - use **Hyper-V isolation**
   (`DOCKER_ISOLATION=hyperv`) so a Server 2022 image can run on a client OS.
   Hyper-V must be enabled.

> **Windows Home editions cannot run Windows containers.** Hyper-V / Windows
> containers are not available on Home. Docker Desktop on Home only provides
> **Linux** containers, which cannot host this application.

### Software

- **Docker Desktop** (or Docker Engine) configured for **Windows containers**.
  In Docker Desktop: *Settings -> General -> "Use the WSL 2 based engine"* is for
  Linux; switch Windows/containers via the tray icon **"Switch to Windows
  containers"**, or run on a Windows Server host. Do **not** expect Linux mode to
  work for this image.
- Enough resources for a Windows Server Core image (several GB of disk; the
  first build pulls large base layers).
- Internet access during the first build, so `nuget restore` can download
  `packages.config` dependencies and Docker can pull the base images.

### Not required

- No local Visual Studio is needed - compilation happens inside the `sdk` image.
- No local SQL Server is required *on the machine running Docker*, but the
  application still needs a reachable SQL Server (see below).

---

## Windows vs Linux containers

| | Linux container | Windows container |
| --- | --- | --- |
| Runs .NET Framework 4.7.2 / MVC 5 / IIS | No | Yes |
| Supported by this Dockerfile | No | Yes |
| Docker Desktop on Windows 11 Home | Yes | No |

The `Dockerfile` deliberately uses `mcr.microsoft.com/dotnet/framework/*`
**Windows** base images. There is no ASP.NET Core / Linux runtime here, because
that could not run this application unchanged. If you try to build while Docker
is in Linux mode, the build fails at the `FROM` step with a
"no matching manifest for linux/amd64" error - that is expected and is a
Windows-container requirement, not a bug in the app.

---

## Build the image

All commands are **PowerShell**, from the repository root:

```powershell
Set-Location 'E:\PhonebookAspNetMVC47'

# 1. Make sure Docker is in Windows-container mode (client host only):
#    Docker Desktop tray icon -> "Switch to Windows containers..."
#    Confirm with:
docker info --format '{{.OSType}}'     # must print: windows

# 2. Build:
docker build -t phonebook-web:latest -f Dockerfile .
```

The build performs three steps:

1. `nuget restore PhonebookAspNetMVC47.sln` (restores `packages.config`).
2. `msbuild ... /p:DeployOnBuild=true /p:WebPublishMethod=FileSystem /p:publishUrl=C:\publish`
   - compiles the solution in **Release** and publishes a clean web root (no
   `.cs` source files) to `C:\publish`.
3. Copies `C:\publish` into the runtime image's `C:\inetpub\wwwroot` and clears
   the development connection string from `Web.config`.

---

## Configure SQL Server (safely)

The application reads its database from the **`DefaultConnection`** connection
string. This project must keep using its **existing** SQL Server database -
the Docker setup never creates, drops or overwrites a database.

### Where the connection string comes from

1. The image ships with `DefaultConnection` **blanked out**
   (`docker/clear-connectionstring.ps1`) so no credential is baked into the
   image.
2. At container start, `docker/entrypoint.ps1` writes the value of the
   **`DEFAULT_CONNECTION_STRING`** environment variable into `Web.config`.
3. `docker-compose.yml` loads that variable from the git-ignored **`.env`**
   file (via `env_file`).

Fractions:

```powershell
Copy-Item .env.example .env
notepad .env      # replace every CHANGE_ME, point at your SQL Server
```

`.env.example` contains **placeholders only**, and `.env` is git-ignored, so real
credentials never get committed.

### Reaching SQL Server from inside the container

A container's `localhost` is the container, **not** the Windows host. Use one of:

| SQL Server location | `Server=` value |
| --- | --- |
| A SQL Server on the Docker **host** or LAN | the machine name or IP, e.g. `sql01.corp.local,1433` |
| SQL Server published on the host, using Docker Desktop | `host.docker.internal,1433` |
| A SQL Server container on the same Docker network | the service/container name, e.g. `sqlserver,1433` |

Example `.env` value (again: placeholders, use your own credentials):

```
DEFAULT_CONNECTION_STRING=Server=host.docker.internal,1433;Initial Catalog=PhonebookAspNetMVC47;User ID=YOUR_LOGIN;Password=YOUR_PASSWORD;MultipleActiveResultSets=True;Encrypt=False;TrustServerCertificate=True
```

Notes:

- The existing **SQL Server Express** instance on the host
  (`.\SQLEXPRESS`) uses **named instances / dynamic ports**. A container cannot
  reach a named instance by `.\SQLEXPRESS`; either enable **TCP/IP** for a fixed
  port (e.g. 1433) on that instance, or point at a SQL Server that already
  listens on TCP. See [Troubleshooting](#troubleshooting).
- The application's database initializer is **additive only** - it creates
  missing schema and seeds demo data but never drops existing tables or data.
  Against your existing `PhonebookAspNetMVC47` database it is a no-op.
- **Demo user**: the `DemoUserEnabled` app setting controls whether the demo
  account is seeded. In `.env`, keep `DEMO_USER_ENABLED=false` for anything other
  than local development. If you do enable it, also supply `DEMO_USER_USERNAME`,
  `DEMO_USER_EMAIL` and `DEMO_USER_PASSWORD`.

### Why there is no SQL Server service in `docker-compose.yml`

- A containerized SQL Server would start as a **new, empty** instance, which
  risks masking/replacing your existing database - the opposite of the goal here.
- Publicly supported SQL Server **Windows**-container images are limited; mixing
  a Linux SQL Server container with this Windows web container is not portable
  across Docker modes.
- Keeping SQL Server external is the safe, correct choice for an existing
  database. Point the container at it with `DEFAULT_CONNECTION_STRING`.

Authentication is unchanged: the app still uses **ASP.NET Identity** with the
existing password-hashing configuration (BCrypt). No authentication code or
configuration is altered by this Docker setup.

---

## Start the application

```powershell
Set-Location 'E:\PhonebookAspNetMVC47'
docker compose up --build -d
```

Then open:

### http://localhost:8080/

- The compose file maps host port **8080** to container port **80**.
- If port 8080 is taken, change the left-hand side in `docker-compose.yml`
  (`"<free-port>:80"`).

Without Compose:

```powershell
docker run -d --name phonebook-web `
  --isolation=process `
  --env-file .env `
  -p 8080:80 `
  phonebook-web:latest
```

> On Windows 10/11 Pro/Enterprise where the host OS build differs from the image
> build (20348), add `--isolation=hyperv` (or set `DOCKER_ISOLATION=hyperv` in
> `.env`) instead of `process`.

## Stop / remove the container

```powershell
# Stop and remove the container (keeps the image)
docker compose down

# Stop only
docker compose stop

# Start again
docker compose start

# Remove the image as well (after "down")
docker rmi phonebook-web:latest
```

`docker compose down` does **not** delete any database, and this setup defines no
volumes, so nothing of yours is removed.

---

## Inspect logs

```powershell
# Follow the web container logs
docker compose logs -f phonebook-web

# Last 200 lines
docker logs --tail 200 phonebook-web

# Confirm IIS is listening inside the container
docker exec phonebook-web powershell -Command "Get-Service w3svc"
```

The entrypoint prints whether the connection string was applied. It never prints
the connection string itself.

---

## Rebuild after code changes

Because the application is compiled **inside** the image, rebuild whenever the
code changes:

```powershell
docker compose build --no-cache
docker compose up -d
```

The `--no-cache` flag guarantees `nuget restore` and `msbuild` re-run. Omit it
for a faster incremental rebuild if you have not changed `packages.config`.

---

## HTTP / UI verification checklist

After `docker compose up`, confirm the site is served end to end:

```powershell
# 1. Home page (redirects to the login page when anonymous)
Invoke-WebRequest http://localhost:8080/ -UseBasicParsing | Select-Object StatusCode

# 2. Login page renders
Invoke-WebRequest http://localhost:8080/Account/Login -UseBasicParsing |
  Select-Object StatusCode, @{n='len';e={$_.Content.Length}}

# 3. Static assets (CSS / JS / layout) are served
Invoke-WebRequest http://localhost:8080/Content/site.css -UseBasicParsing | Select-Object StatusCode, Headers
Invoke-WebRequest http://localhost:8080/Scripts/jquery-3.7.1.min.js -UseBasicParsing | Select-Object StatusCode
```

Then, in a browser:

1. Open `http://localhost:8080/` - you should be redirected to `/Account/Login`.
2. Log in with your database's Identity account.
3. Confirm the dashboard, the contacts list and the stylesheet load, and that the
   sidebar and icons render normally (i.e. the Razor layout and `Content/site.css`
   are being served).
4. Exercise a contacts Create/Edit/Details/Delete round trip against test data
   only.

---

## Known limitations

1. **Windows-host only.** As above, this configuration only works where Windows
   containers are supported. It was authored on Windows 11 **Home**, where that
   is not possible, so the image has **not** been built or run there. Every file
   here is written to Microsoft's documented ASP.NET + IIS pattern, but treat the
   first build on a supported host as the real verification step.
2. **Database is external and not containerized** (intentional, to protect your
   existing data).
3. **`packages.config` restore needs network access** on the first build.
4. **Image size.** Windows Server Core + IIS + the .NET Framework is large
   (multiple GB). This is inherent to .NET Framework containerization.
5. **`bin\roslyn`.** The project uses `Microsoft.CodeDom.Providers.DotNetCompilerPlatform`;
   the project's existing build target copies the Roslyn compiler into `bin\roslyn`.
   If Razor views fail to compile at runtime, confirm `C:\inetpub\wwwroot\bin\roslyn\csc.exe`
   exists in the container.

---

## Troubleshooting

| Symptom | Cause / fix |
| --- | --- |
| `no matching manifest for linux/amd64` while building | Docker is in **Linux** mode. Switch to Windows containers, or run on a Windows Server host. |
| `image operating system "windows" cannot be used on this platform` | Same as above - the host cannot run Windows containers (e.g. Windows Home). Use a supported host. |
| `failed to solve: ... hcsshim::... The container operating system does not match the host operating system` | Isolation mismatch. Use `DOCKER_ISOLATION=hyperv` on Windows 10/11 Pro, or match the host/image builds with `process`. |
| `msbuild` not recognised during `docker build` | The `sdk` base image provides MSBuild; if a custom base is used, invoke `MSBuild.exe` by full path. |
| `The WebPublishMethod ... Microsoft.WebApplication.targets was not found` | The publish targets are provided by the build tools in the `sdk` image. Ensure the image is `mcr.microsoft.com/dotnet/framework/sdk:4.8-windowsservercore-ltsc2022`. |
| Container starts but the site shows a DB error | `DEFAULT_CONNECTION_STRING` is unset or wrong. Check `.env` and `docker compose logs`. Remember `localhost` inside the container is not the host. |
| Cannot reach `.\SQLEXPRESS` from the container | Host used a named instance without TCP. Enable **TCP/IP** with a fixed port for that instance, or use a server that listens on TCP. |
| `A network-related or instance-specific error ...` | SQL Server not reachable on the configured host/port from the container; check firewall and the `Server=` value. |
| Port 8080 already in use | Change the host side of the port mapping in `docker-compose.yml`. |

---

## Alternatives (if Windows containers are unavailable on your machine)

1. **Run on a supported host.** Build/push the image on Windows Server 2022/2025
   or Windows 11 Pro with Hyper-V isolation, then deploy it there. This is the
   intended production path.
2. **Keep running without Docker.** The application already runs correctly on
   IIS / IIS Express with the existing SQL Server - for example:
   ```powershell
   & "C:\Program Files\IIS Express\iisexpress.exe" /path:"E:\PhonebookAspNetMVC47\Phonebook.Web" /port:8081
   ```
3. **Containerize only the database (Linux) - optional, development only.**
   Docker Desktop's Linux mode can run a SQL Server Linux container. This creates
   a **separate, empty** SQL Server, so it is only appropriate for throwaway
   experiments - never as a replacement for your existing database. If you use it,
   point `DEFAULT_CONNECTION_STRING` at that container and re-seed it with the
   app's initializer.

---

## Exact command summary

```powershell
# One-time setup
Set-Location 'E:\PhonebookAspNetMVC47'
Copy-Item .env.example .env
notepad .env                                   # fill in real, local values

# Verify Docker is in Windows-container mode  (must print: windows)
docker info --format '{{.OSType}}'

# Build and start
docker compose up --build -d

# Open
Start-Process http://localhost:8080/

# Logs
docker compose logs -f phonebook-web

# Rebuild after code changes
docker compose build --no-cache ; docker compose up -d

# Stop
docker compose down
```
