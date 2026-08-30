## Build stage
#FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
#ARG BUILD_CONFIGURATION=Release
#WORKDIR /src
#
## Copy everything
#COPY . .
#
## Restore
#RUN dotnet restore "./SampleDataAccessApi.csproj"
#
## Build
#RUN dotnet build "./SampleDataAccessApi.csproj" -c $BUILD_CONFIGURATION -o /app/build
#
## Publish
#RUN dotnet publish "./SampleDataAccessApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false
#
## Runtime stage
#FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
#WORKDIR /app
#COPY --from=build /app/publish .
#
#ENV ASPNETCORE_URLS=http://+:8080
#EXPOSE 8080
#
#ENTRYPOINT ["dotnet", "SampleDataAccessApi.dll"]
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release

# Set working directory inside the container
WORKDIR /src

# Copy only the csproj first (better caching)
COPY SampleDataAccessApi/SampleDataAccessApi.csproj SampleApi/

# Restore dependencies
# RUN dotnet restore "SampleDataAccessApi/SampleDataAccessApi.csproj" 
# Get all the restore packages from the nuget cache in my local machine
# this solution is only when running in public networks (hotspots)
# also disable running jobs in parallel
#COPY nuget.config .
RUN dotnet restore "SampleDataAccessApi/SampleDataAccessApi.csproj" 
#RUN --mount=type=cache,target=/root/.nuget/packages \
#    dotnet restore "SampleDataAccessApi/SampleDataAccessApi.csproj" --disable-parallel



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
