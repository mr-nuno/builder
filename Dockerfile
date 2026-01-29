FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Directory.Packages.props", "."]
COPY ["ProjectGenerator.Core/ProjectGenerator.Core.csproj", "ProjectGenerator.Core/"]
COPY ["ProjectGenerator.Cli/ProjectGenerator.Cli.csproj", "ProjectGenerator.Cli/"]
RUN dotnet restore "ProjectGenerator.Cli/ProjectGenerator.Cli.csproj"
COPY . .
WORKDIR "/src/ProjectGenerator.Cli"
RUN dotnet build "ProjectGenerator.Cli.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ProjectGenerator.Cli.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ProjectGenerator.Cli.dll"]
