# Build context: repository root (see docker-compose.yml).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/BuildingBlocks/ src/BuildingBlocks/
COPY src/Payments/ src/Payments/
RUN dotnet publish src/Payments/Payments.Worker/Payments.Worker.csproj -c Release -o /app

# A worker has no HTTP endpoint, so the smaller .NET runtime image is enough.
FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app ./
RUN mkdir -p /app/data && chown "$APP_UID" /app/data
USER $APP_UID
ENV ConnectionStrings__Payments="Data Source=/app/data/payments.db"
ENTRYPOINT ["dotnet", "Payments.Worker.dll"]
