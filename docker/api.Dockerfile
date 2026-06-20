# ── Stage 1: Build ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

# Copy solution and project files first (layer cache optimization)
COPY Aynesil.sln ./
COPY Directory.Build.props ./
COPY src/Aynesil.Shared/Aynesil.Shared.csproj src/Aynesil.Shared/
COPY src/Aynesil.Domain/Aynesil.Domain.csproj src/Aynesil.Domain/
COPY src/Aynesil.Application/Aynesil.Application.csproj src/Aynesil.Application/
COPY src/Aynesil.Infrastructure/Aynesil.Infrastructure.csproj src/Aynesil.Infrastructure/
COPY src/Aynesil.Api/Aynesil.Api.csproj src/Aynesil.Api/

# Restore NuGet packages
RUN dotnet restore src/Aynesil.Api/Aynesil.Api.csproj

# Copy everything else and build
COPY src/ src/
WORKDIR /src/src/Aynesil.Api
RUN dotnet publish -c Release -o /publish --no-restore

# ── Stage 2: Runtime ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime
WORKDIR /app

# Security: run as non-root
RUN addgroup -S aynesil && adduser -S -G aynesil aynesil
USER aynesil

COPY --from=build --chown=aynesil:aynesil /publish ./

# Create uploads directory
RUN mkdir -p uploads logs

EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "Aynesil.Api.dll"]
