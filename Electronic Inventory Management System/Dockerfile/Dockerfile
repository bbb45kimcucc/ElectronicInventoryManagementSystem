# Build stage
using System.Composition;

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /app

COPY . .
RUN dotnet publish -c Release -o out

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app

COPY --from=build /app/out .

EXPOSE 8080
ENTRYPOINT ["dotnet", "ElectronicInventoryManagementSystem.dll"]# Build stage
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /app

COPY . .
RUN dotnet publish -c Release -o out

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app

COPY --from=build /app/out .

EXPOSE 8080
ENTRYPOINT ["dotnet", "ElectronicInventoryManagementSystem.dll"]