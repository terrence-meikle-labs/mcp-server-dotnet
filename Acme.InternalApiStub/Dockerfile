# Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ./Acme.Contracts/ ./Acme.Contracts/
COPY ./Acme.InternalApiStub/ ./Acme.InternalApiStub/

RUN dotnet restore ./Acme.InternalApiStub/Acme.InternalApiStub.csproj
RUN dotnet publish ./Acme.InternalApiStub/Acme.InternalApiStub.csproj -c Release -o /app/publish

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

ENV ASPNETCORE_URLS=http://0.0.0.0:5159
COPY --from=build /app/publish .

EXPOSE 5159
ENTRYPOINT ["dotnet", "Acme.InternalApiStub.dll"]
