FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["consumers/NHS111.UmmanuConsumer/NHS111.UmmanuConsumer.csproj", "consumers/NHS111.UmmanuConsumer/"]
COPY ["src/NHS111.Contracts/NHS111.Contracts.csproj", "src/NHS111.Contracts/"]
COPY ["src/NHS111.Infrastructure/NHS111.Infrastructure.csproj", "src/NHS111.Infrastructure/"]
COPY ["src/NHS111.Domain/NHS111.Domain.csproj", "src/NHS111.Domain/"]
RUN dotnet restore "consumers/NHS111.UmmanuConsumer/NHS111.UmmanuConsumer.csproj"
COPY . .
WORKDIR "/src/consumers/NHS111.UmmanuConsumer"
RUN dotnet build "NHS111.UmmanuConsumer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NHS111.UmmanuConsumer.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NHS111.UmmanuConsumer.dll"]

