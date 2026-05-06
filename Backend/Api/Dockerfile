FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Backend/Api/HashtagWall.Api.csproj", "Backend/Api/"]
COPY ["Backend/Application/HashtagWall.Application.csproj", "Backend/Application/"]
COPY ["Backend/Infrastructure/HashtagWall.Infrastructure.csproj", "Backend/Infrastructure/"]
COPY ["Backend/Domain/HashtagWall.Domain.csproj", "Backend/Domain/"]

RUN dotnet restore "Backend/Api/HashtagWall.Api.csproj"

COPY Backend/ Backend/

RUN dotnet publish "Backend/Api/HashtagWall.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8500

ENTRYPOINT ["dotnet", "HashtagWall.Api.dll"]
