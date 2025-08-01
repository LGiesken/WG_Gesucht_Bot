# 1. Basis-Image für das Build-Environment
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Nur csproj zuerst kopieren und Wiederherstellen (bessere Caching-Effizienz)
COPY SeleniumBot/*.csproj ./SeleniumBot/
RUN dotnet restore ./SeleniumBot/SeleniumBot.csproj

# Restlichen Code kopieren
COPY SeleniumBot/. ./SeleniumBot/

# Build
WORKDIR /src/SeleniumBot
RUN dotnet publish -c Release -o /app/publish

# 2. Runtime-Image
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app

# Abhängigkeiten installieren (Chrome + Treiber)
RUN apt-get update && apt-get install -y wget unzip curl gnupg \
    && rm -rf /var/lib/apt/lists/*

# Google Chrome installieren
RUN curl -fsSL https://dl.google.com/linux/linux_signing_key.pub | gpg --dearmor -o /usr/share/keyrings/google-linux.gpg \
    && echo "deb [arch=amd64 signed-by=/usr/share/keyrings/google-linux.gpg] http://dl.google.com/linux/chrome/deb/ stable main" > /etc/apt/sources.list.d/google-chrome.list \
    && apt-get update \
    && apt-get install -y google-chrome-stable \
    && rm -rf /var/lib/apt/lists/*

# Selenium ChromeDriver Version 138 installieren
RUN wget https://edgedl.me.gvt1.com/edgedl/chrome/chrome-for-testing/138.0.7204.183/linux64/chromedriver-linux64.zip \
    && unzip chromedriver-linux64.zip \
    && mv chromedriver-linux64/chromedriver /usr/local/bin/ \
    && chmod +x /usr/local/bin/chromedriver \
    && rm -rf chromedriver-linux64*

# Veröffentlichtes Projekt kopieren
COPY --from=build /app/publish .

# Entry point
ENTRYPOINT ["dotnet", "SeleniumBot.dll"]
