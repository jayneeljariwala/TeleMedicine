FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 7001
ENV ASPNETCORE_URLS=http://+:7001

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Ummanu.DummyAPI.csproj", "./"]
RUN dotnet restore "Ummanu.DummyAPI.csproj"
COPY . .
RUN dotnet build "Ummanu.DummyAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Ummanu.DummyAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Ummanu.DummyAPI.dll"]

