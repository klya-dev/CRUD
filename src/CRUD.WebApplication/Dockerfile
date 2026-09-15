FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# CRUD.WebApplication не использует локальные nuget пакеты, поэтому NuGet.config не нужен

# Копируем файл проекта
COPY CRUD.WebApplication/CRUD.WebApplication.csproj CRUD.WebApplication/

# Восстанавливаем зависимости/пакеты
RUN dotnet restore CRUD.WebApplication/CRUD.WebApplication.csproj

# Копируем только нужные для проекта папки
COPY CRUD.WebApplication/ CRUD.WebApplication/

# Компилируем и публикуем приложение
RUN dotnet publish CRUD.WebApplication/CRUD.WebApplication.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Создаём пользователя, копируем данные и выдаём права
RUN useradd --uid 5678 --user-group --create-home appuser
COPY --from=build --chown=appuser:appuser /app .
RUN mkdir -p /data && chown -R appuser:appuser /data
USER appuser

# Определяем окружение и порты
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "CRUD.WebApplication.dll"]