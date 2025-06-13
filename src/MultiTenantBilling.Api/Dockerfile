# Use the official .NET 8 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution file
COPY MultiTenantBilling.sln .

# Copy project files
COPY src/MultiTenantBilling.Core/MultiTenantBilling.Core.csproj src/MultiTenantBilling.Core/
COPY src/MultiTenantBilling.Infrastructure/MultiTenantBilling.Infrastructure.csproj src/MultiTenantBilling.Infrastructure/
COPY src/MultiTenantBilling.Api/MultiTenantBilling.Api.csproj src/MultiTenantBilling.Api/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY src/ src/

# Build the application
WORKDIR /src/src/MultiTenantBilling.Api
RUN dotnet build -c Release -o /app/build

# Publish the application
RUN dotnet publish -c Release -o /app/publish

# Use the official .NET 8 runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create logs directory
RUN mkdir -p /app/logs

# Copy published application
COPY --from=build /app/publish .

# Expose ports
EXPOSE 8080
EXPOSE 8081

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080;https://+:8081
ENV ASPNETCORE_ENVIRONMENT=Production

# Start the application
ENTRYPOINT ["dotnet", "MultiTenantBilling.Api.dll"]
