FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Invest.sln .
COPY Invest.Domain/Invest.Domain.csproj Invest.Domain/
COPY Invest.Application/Invest.Application.csproj Invest.Application/
COPY Invest.Infrastructure/Invest.Infrastructure.csproj Invest.Infrastructure/
COPY Invest.API/Invest.API.csproj Invest.API/
COPY Invest.Tests/Invest.Tests.csproj Invest.Tests/

RUN dotnet restore

COPY . .
RUN dotnet publish Invest.API/Invest.API.csproj -c Release -o /app/publish

# ── Stage de testes (unitários — sem banco) ───────────────────────────────────
FROM build AS test
WORKDIR /src
ENTRYPOINT ["dotnet", "test", "Invest.Tests/Invest.Tests.csproj", "--no-build", "-q"]

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5000
ENTRYPOINT ["dotnet", "Invest.API.dll"]
