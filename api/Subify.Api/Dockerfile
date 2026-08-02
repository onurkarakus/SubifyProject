# Subify OS API — multi-stage (Faz 11.1.1)
# Build context: repository `api/` directory
#   docker build -f Subify.Api/Dockerfile -t subify-api .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Subify.Domain/Subify.Domain.csproj Subify.Domain/
COPY Subify.Application/Subify.Application.csproj Subify.Application/
COPY Subify.Infrastructure/Subify.Infrastructure.csproj Subify.Infrastructure/
COPY Subify.Api/Subify.Api.csproj Subify.Api/

RUN dotnet restore Subify.Api/Subify.Api.csproj

COPY Subify.Domain/ Subify.Domain/
COPY Subify.Application/ Subify.Application/
COPY Subify.Infrastructure/ Subify.Infrastructure/
COPY Subify.Api/ Subify.Api/

RUN dotnet publish Subify.Api/Subify.Api.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && groupadd --system --gid 1001 subify \
    && useradd --system --uid 1001 --gid subify --create-home subify

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_EnableDiagnostics=0

EXPOSE 8080

COPY --from=build /app/publish .
RUN chown -R subify:subify /app
USER subify

HEALTHCHECK --interval=15s --timeout=5s --start-period=45s --retries=5 \
  CMD curl -fsS http://127.0.0.1:8080/health || exit 1

ENTRYPOINT ["dotnet", "Subify.Api.dll"]
