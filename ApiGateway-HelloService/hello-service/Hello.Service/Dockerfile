FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копируем проект и восстанавливаем зависимости
COPY ["Hello.Service.csproj", "./"]
RUN dotnet restore "Hello.Service.csproj"

# Копируем остальной код и собираем релиз
COPY . .
RUN dotnet publish "Hello.Service.csproj" -c Release -o /app/publish

# Финальный образ для запуска
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Сервис будет слушать порт 5001 внутри контейнера
ENV ASPNETCORE_URLS=http://+:5001

ENTRYPOINT ["dotnet", "Hello.Service.dll"]
