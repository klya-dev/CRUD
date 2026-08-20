FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# CRUD.Shared и CRUD.Infrastructure.S3 идут локальными nuget пакетами
# Копируем файл конфигурации NuGet и локальные пакеты
COPY configs/NuGet.config ./ 
COPY nugets/ ./nugets/

# Папка с конфигами (prometheus.yml, private_key.pem, public_key.pem, NuGet.config) через volume

# Копируем файл проекта
COPY Microservice.EmailSender/Microservice.EmailSender.csproj Microservice.EmailSender/

# Восстанавливаем зависимости/пакеты
RUN dotnet restore Microservice.EmailSender/Microservice.EmailSender.csproj

# Копируем только нужные для проекта папки
COPY Microservice.EmailSender/ Microservice.EmailSender/

# Компилируем и публикуем приложение
RUN dotnet publish Microservice.EmailSender/Microservice.EmailSender.csproj -c Release -o /app --no-restore

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

ENTRYPOINT ["dotnet", "Microservice.EmailSender.dll"]