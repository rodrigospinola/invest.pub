FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Invest.sln .
COPY Invest.Domain/Invest.Domain.csproj Invest.Domain/
COPY Invest.Application/Invest.Application.csproj Invest.Application/
COPY Invest.Infrastructure/Invest.Infrastructure.csproj Invest.Infrastructure/
COPY Invest.API/Invest.API.csproj Invest.API/

RUN dotnet restore Invest.API/Invest.API.csproj

COPY . .
RUN dotnet publish Invest.API/Invest.API.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5000
ENTRYPOINT ["dotnet", "Invest.API.dll"]
