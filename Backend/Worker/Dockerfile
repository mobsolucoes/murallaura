FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Backend/Worker/HashtagWall.Worker.csproj", "Backend/Worker/"]
COPY ["Backend/Application/HashtagWall.Application.csproj", "Backend/Application/"]
COPY ["Backend/Infrastructure/HashtagWall.Infrastructure.csproj", "Backend/Infrastructure/"]
COPY ["Backend/Domain/HashtagWall.Domain.csproj", "Backend/Domain/"]

RUN dotnet restore "Backend/Worker/HashtagWall.Worker.csproj"

COPY Backend/ Backend/

RUN dotnet publish "Backend/Worker/HashtagWall.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/playwright/dotnet:v1.55.0-noble AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "HashtagWall.Worker.dll"]
