# -----------------------------------------------------------------------------
# STAGE 1: BUILD (Bygger själva Agenten)
# -----------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["MyAgent.Builder/MyAgent.Builder.csproj", "MyAgent.Builder/"]
RUN dotnet restore "MyAgent.Builder/MyAgent.Builder.csproj"

COPY . .
WORKDIR "/src/MyAgent.Builder"
RUN dotnet publish "MyAgent.Builder.csproj" -c Release -o /app/publish /p:UseAppHost=false

# -----------------------------------------------------------------------------
# STAGE 2: RUNTIME (Här körs Agenten + dotnet ef)
# -----------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS final
WORKDIR /app

# 1. Installera systemverktyg (git, curl, gh)
RUN apt-get update && \
    apt-get install -y git curl && \
    curl -fsSL https://cli.github.com/packages/githubcli-archive-keyring.gpg | dd of=/usr/share/keyrings/githubcli-archive-keyring.gpg \
    && chmod go+r /usr/share/keyrings/githubcli-archive-keyring.gpg \
    && echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/githubcli-archive-keyring.gpg] https://cli.github.com/packages stable main" | tee /etc/apt/sources.list.d/github-cli.list > /dev/null \
    && apt-get update \
    && apt-get install -y gh \
    && rm -rf /var/lib/apt/lists/*

# 2. Installera dotnet-ef (VIKTIGT!)
RUN dotnet tool install --global dotnet-ef

# 3. Lägg till dotnet tools i PATH så systemet hittar kommandot
ENV PATH="$PATH:/root/.dotnet/tools"

# Kopiera Agenten
COPY --from=build /app/publish .

WORKDIR /data
ENTRYPOINT ["dotnet", "/app/MyAgent.Builder.dll"]