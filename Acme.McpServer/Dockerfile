# Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ./Acme.Contracts/ ./Acme.Contracts/
COPY ./Acme.McpServer/ ./Acme.McpServer/

RUN dotnet restore ./Acme.McpServer/Acme.McpServer.csproj
RUN dotnet publish ./Acme.McpServer/Acme.McpServer.csproj -c Release -o /app/publish

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Listen on 3004 in container
ENV ASPNETCORE_URLS=http://0.0.0.0:3004
# Use docker settings
ENV DOTNET_ENVIRONMENT=Docker

COPY --from=build /app/publish .

EXPOSE 3004
ENTRYPOINT ["dotnet", "Acme.McpServer.dll", "--http"]
