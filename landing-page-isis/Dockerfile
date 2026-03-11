FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
# Install globalization support for pt-BR and timezones
RUN apt-get update && apt-get install -y --no-install-recommends \
    icu-devtools \
    tzdata \
    && rm -rf /var/lib/apt/lists/*

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_RESPONSE_COMPRESSION_LEVEL=Optimal
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files first to optimize layer caching
COPY ["landing-page-isis/landing-page-isis.csproj", "landing-page-isis/"]
COPY ["landing-page-isis.core/landing-page-isis.core.csproj", "landing-page-isis.core/"]

# Restore dependencies
RUN dotnet restore "landing-page-isis/landing-page-isis.csproj"

# Copy all remaining source code
COPY . .
WORKDIR "/src/landing-page-isis"
RUN dotnet build "./landing-page-isis.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./landing-page-isis.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY landing-page-isis/Templates /app/Templates

ENTRYPOINT ["dotnet", "landing-page-isis.dll"]
