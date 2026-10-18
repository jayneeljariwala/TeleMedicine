FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/NHS111.API/NHS111.API.csproj", "src/NHS111.API/"]
COPY ["src/NHS111.Application/NHS111.Application.csproj", "src/NHS111.Application/"]
COPY ["src/NHS111.Domain/NHS111.Domain.csproj", "src/NHS111.Domain/"]
COPY ["src/NHS111.Infrastructure/NHS111.Infrastructure.csproj", "src/NHS111.Infrastructure/"]
COPY ["src/NHS111.Contracts/NHS111.Contracts.csproj", "src/NHS111.Contracts/"]
RUN dotnet restore "src/NHS111.API/NHS111.API.csproj"
COPY . .
WORKDIR "/src/src/NHS111.API"
RUN dotnet build "NHS111.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NHS111.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NHS111.API.dll"]
