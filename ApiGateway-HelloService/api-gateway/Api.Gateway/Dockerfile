FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Api.Gateway.csproj", "./"]
RUN dotnet restore "Api.Gateway.csproj"

COPY . .
RUN dotnet publish "Api.Gateway.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Шлюз будет слушать порт 7000
ENV ASPNETCORE_URLS=http://+:7000

ENTRYPOINT ["dotnet", "Api.Gateway.dll"]
