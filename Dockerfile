# escape=`
#
#  Phonebook.Web  -  ASP.NET MVC 5 / .NET Framework 4.7.2 on IIS
#  ==============================================================
#  *** WINDOWS CONTAINER IMAGE ***  (this does NOT build on Linux)
#
#  Stages
#    build   : mcr.microsoft.com/dotnet/framework/sdk:4.8     (MSBuild + NuGet)
#    runtime : mcr.microsoft.com/dotnet/framework/aspnet:4.8  (IIS + ASP.NET 4.x)
#  Both are Windows Server 2022 / windowsservercore-ltsc2022 (OS build 20348).
#  The app targets .NET Framework 4.7.2, which runs on the 4.8 runtime.
#
#  Build :  docker build -t phonebook-web:latest .
#  Run   :  see docker-compose.yml and README-Docker.md
#
#  IMPORTANT: this image can only be built and run on a host that supports
#  Windows containers. See README-Docker.md for prerequisites and for the
#  limitations of the current development machine.

# ---------------------------------------------------------------------------
# Stage 1 - restore, compile and publish
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/framework/sdk:4.8-windowsservercore-ltsc2022 AS build
SHELL ["powershell", "-Command", "$ErrorActionPreference='Stop'; $ProgressPreference='SilentlyContinue';"]

WORKDIR C:\src
COPY . .

# Restore packages.config dependencies using the nuget.exe pinned in the repo.
RUN C:\src\tools\nuget.exe restore C:\src\PhonebookAspNetMVC47.sln -NonInteractive -Verbosity quiet

# Compile (Release) and publish a clean web root (no .cs source files) to C:\publish.
RUN msbuild C:\src\PhonebookAspNetMVC47.sln /p:Configuration=Release /p:DeployOnBuild=true /p:DeployDefaultTarget=WebPublish /p:WebPublishMethod=FileSystem /p:publishUrl=C:\publish /p:SkipInvalidConfigurations=true /m /nologo /v:minimal

# Strip the local development connection string so no credential is baked into the image.
RUN C:\src\docker\clear-connectionstring.ps1 -Path C:\publish\Web.config

# ---------------------------------------------------------------------------
# Stage 2 - runtime (IIS)
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/framework/aspnet:4.8-windowsservercore-ltsc2022
SHELL ["powershell", "-Command", "$ErrorActionPreference='Stop'; $ProgressPreference='SilentlyContinue';"]

# Replace the default IIS landing page with the published application.
RUN Remove-Item -Recurse -Force C:\inetpub\wwwroot\* -ErrorAction SilentlyContinue
COPY --from=build C:\publish C:\inetpub\wwwroot
COPY docker\entrypoint.ps1 C:\entrypoint.ps1

EXPOSE 80
ENTRYPOINT ["powershell", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", "C:\\entrypoint.ps1"]
