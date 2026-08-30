# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release

WORKDIR /src

# Copy csproj
COPY SampleDataAccessApi/SampleDataAccessApi.csproj SampleDataAccessApi/

# Restore
RUN dotnet restore "SampleDataAccessApi/SampleDataAccessApi.csproj"

# Copy the rest of the source code
COPY SampleDataAccessApi/ SampleDataAccessApi/

# Build
RUN dotnet build "SampleDataAccessApi/SampleDataAccessApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish
RUN dotnet publish "SampleDataAccessApi/SampleDataAccessApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SampleDataAccessApi.dll"]
