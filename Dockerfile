FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/BookCatalog.Domain/BookCatalog.Domain.csproj src/BookCatalog.Domain/
COPY src/BookCatalog.Application/BookCatalog.Application.csproj src/BookCatalog.Application/
COPY src/BookCatalog.Infrastructure/BookCatalog.Infrastructure.csproj src/BookCatalog.Infrastructure/
COPY src/BookCatalog.Api/BookCatalog.Api.csproj src/BookCatalog.Api/
RUN dotnet restore src/BookCatalog.Api/BookCatalog.Api.csproj

COPY src/ src/
RUN dotnet publish src/BookCatalog.Api/BookCatalog.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "BookCatalog.Api.dll"]