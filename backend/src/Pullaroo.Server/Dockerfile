FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["backend/src/Pullaroo.Server/Pullaroo.Server.csproj", "backend/src/Pullaroo.Server/"]
COPY ["backend/src/AgGrid.ServerSideRowModel/AgGrid.ServerSideRowModel.csproj", "backend/src/AgGrid.ServerSideRowModel/"]
COPY ["backend/src/Pullaroo.Contracts/Pullaroo.Contracts.csproj", "backend/src/Pullaroo.Contracts/"]
COPY ["backend/src/Pullaroo.Common/Pullaroo.Common.csproj", "backend/src/Pullaroo.Common/"]
RUN dotnet restore "backend/src/Pullaroo.Server/Pullaroo.Server.csproj"
COPY . .
WORKDIR "/src/backend/src/Pullaroo.Server"
RUN dotnet build "Pullaroo.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Pullaroo.Server.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Pullaroo.Server.dll"]
