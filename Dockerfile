FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/backend/IndiaFinanceAI.Domain/IndiaFinanceAI.Domain.csproj src/backend/IndiaFinanceAI.Domain/
COPY src/backend/IndiaFinanceAI.Application/IndiaFinanceAI.Application.csproj src/backend/IndiaFinanceAI.Application/
COPY src/backend/IndiaFinanceAI.Infrastructure/IndiaFinanceAI.Infrastructure.csproj src/backend/IndiaFinanceAI.Infrastructure/
COPY src/backend/IndiaFinanceAI.Api/IndiaFinanceAI.Api.csproj src/backend/IndiaFinanceAI.Api/
RUN dotnet restore src/backend/IndiaFinanceAI.Api/IndiaFinanceAI.Api.csproj

COPY src/backend/ src/backend/
RUN dotnet publish src/backend/IndiaFinanceAI.Api/IndiaFinanceAI.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
COPY --from=build --chown=$APP_UID:$APP_UID /app/publish .
ENTRYPOINT ["dotnet", "IndiaFinanceAI.Api.dll"]
