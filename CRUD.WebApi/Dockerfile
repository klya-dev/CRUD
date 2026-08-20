FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# CRUD.Shared и CRUD.Infrastructure.S3 идут локальными nuget пакетами
# Копируем файл конфигурации NuGet и локальные пакеты
COPY configs/NuGet.config ./ 
COPY nugets/ ./nugets/

# Папка с конфигами (prometheus.yml, private_key.pem, public_key.pem, NuGet.config) через volume

# Копируем файлы проектов
COPY CRUD.Models/CRUD.Models.csproj CRUD.Models/
COPY CRUD.Utility/CRUD.Utility.csproj CRUD.Utility/
COPY CRUD.DataAccess/CRUD.DataAccess.csproj CRUD.DataAccess/
COPY CRUD.Services/CRUD.Services.csproj CRUD.Services/
COPY CRUD.WebApi/CRUD.WebApi.csproj CRUD.WebApi/

# Восстанавливаем зависимости/пакеты
RUN dotnet restore CRUD.WebApi/CRUD.WebApi.csproj

# Копируем только нужные для проекта папки
COPY CRUD.Models/ CRUD.Models/
COPY CRUD.Utility/ CRUD.Utility/
COPY CRUD.DataAccess/ CRUD.DataAccess/
COPY CRUD.Services/ CRUD.Services/
COPY CRUD.WebApi/ CRUD.WebApi/

# Компилируем и публикуем приложение
RUN dotnet publish CRUD.WebApi/CRUD.WebApi.csproj -c Release -o /app --no-restore

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

ENTRYPOINT ["dotnet", "CRUD.WebApi.dll"]