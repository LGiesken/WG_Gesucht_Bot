# Basis-Image
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj und restore
COPY SeleniumBot/SeleniumBot.csproj SeleniumBot/
RUN dotnet restore SeleniumBot/SeleniumBot.csproj

# Copy alles und build
COPY . .
WORKDIR /src/SeleniumBot
RUN dotnet publish -c Release -o /app/publish

# Runtime-Image
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app

# Chrome & ChromeDriver installieren
RUN apt-get update && apt-get install -y wget gnupg unzip \
    && wget -q -O - https://dl.google.com/linux/linux_signing_key.pub | gpg --dearmor > /usr/share/keyrings/google-chrome.gpg \
    && echo "deb [arch=amd64 signed-by=/usr/share/keyrings/google-chrome.gpg] http://dl.google.com/linux/chrome/deb/ stable main" > /etc/apt/sources.list.d/google-chrome.list \
    && apt-get update \
    && apt-get install -y google-chrome-beta \
    && rm -rf /var/lib/apt/lists/*

# ChromeDriver installieren
RUN wget -q https://edgedl.me.gvt1.com/edgedl/chrome/chrome-for-testing/139.0.7258.0/linux64/chromedriver-linux64.zip \
    && unzip chromedriver-linux64.zip \
    && mv chromedriver-linux64/chromedriver /usr/local/bin/ \
    && chmod +x /usr/local/bin/chromedriver \
    && rm -rf chromedriver-linux64*

COPY --from=build /app/publish .

# Entrypoint
ENTRYPOINT ["dotnet", "SeleniumBot.dll"]
