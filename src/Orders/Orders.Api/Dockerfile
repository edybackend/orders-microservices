# Build context: repository root (see docker-compose.yml).

# 1) Front-end: React build served as static files by the API.
FROM node:22-slim AS web
WORKDIR /web
COPY web/package.json web/package-lock.json ./
RUN npm ci
COPY web/ ./
RUN npm run build

# 2) Back-end: publish the API.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/BuildingBlocks/ src/BuildingBlocks/
COPY src/Orders/ src/Orders/
RUN dotnet publish src/Orders/Orders.Api/Orders.Api.csproj -c Release -o /app

# 3) Runtime: ASP.NET Core only, non-root user.
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app ./
COPY --from=web /web/dist ./wwwroot
RUN mkdir -p /app/data && chown "$APP_UID" /app/data
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ConnectionStrings__Orders="Data Source=/app/data/orders.db"
EXPOSE 8080
ENTRYPOINT ["dotnet", "Orders.Api.dll"]
