# ── Build stage ───────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore layer: only copy project files first so this layer is cached
# until dependencies change (not every code change)
COPY HTrack.sln ./
COPY src/Htrack.Api/Htrack.Api.csproj ./src/Htrack.Api/
RUN dotnet restore src/Htrack.Api/Htrack.Api.csproj

# Copy everything else and publish
COPY . .
RUN dotnet publish src/Htrack.Api/Htrack.Api.csproj \
    -c Release -o /app/publish --no-restore

# ── Runtime stage ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# Install tzdata for Asia/Tashkent timezone support
RUN apt-get update \
    && apt-get install -y --no-install-recommends tzdata \
    && rm -rf /var/lib/apt/lists/*

ENV TZ=Asia/Tashkent
ENV ASPNETCORE_ENVIRONMENT=Production

WORKDIR /app

COPY --from=build /app/publish .

# Render.com injects PORT at runtime. Program.cs reads it via
# Environment.GetEnvironmentVariable("PORT") and binds Kestrel accordingly.
EXPOSE 8080

ENTRYPOINT ["dotnet", "Htrack.Api.dll"]
