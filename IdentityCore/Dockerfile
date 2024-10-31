FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["../Helpers/Helpers.csproj", "Helpers/"]
COPY ["../IdentityCore.DAL.PostgreSQL/IdentityCore.DAL.PostgreSQL.csproj", "IdentityCore.DAL.PostgreSQL/"]
COPY ["../IdentityCore/IdentityCore.csproj", "IdentityCore/"]

RUN dotnet restore "IdentityCore/IdentityCore.csproj"

COPY .. .

WORKDIR "/src/IdentityCore"
RUN dotnet build "IdentityCore.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "IdentityCore.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "IdentityCore.dll"]