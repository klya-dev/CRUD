@echo off
setlocal

:: --- НАСТРОЙКИ ---
set "APP_DIR=E:\Projects\Web\CRUD\CRUD.WebApi\bin\Debug\net9.0"
set "EXE_NAME=CRUD.WebApi.exe
set "TEST_CONFIG_SRC=E:\Projects\Web\CRUD\CRUD.Tests\testsettings.json"
set "SECRETS_SRC=C:\Users\Admin\AppData\Roaming\Microsoft\UserSecrets\adb0ac74-b0b1-4c42-98b6-690286bf402e\secrets.json"
:: Секреты тестового проекта!!!

:: 1. Переход в директорию с exe
cd /d "%APP_DIR%"

:: 2. Сохраняем оригинальный конфиг во временный файл
if exist "appsettings.json" (
    echo [INFO] Backup original appsettings...
    if exist "appsettings.json.bak" del "appsettings.json.bak"
    ren "appsettings.json" "appsettings.json.bak"
)

:: 3. Объединяем testsettings.json и secrets.json
echo [INFO] Merging files via string manipulation...

:: Удаляем последнюю скобку первого файла и добавляем запятую
powershell -Command ^
    "$content = Get-Content '%TEST_CONFIG_SRC%' -Raw; " ^
    "$content = $content.Trim().TrimEnd('}'); " ^
    "Set-Content 'temp_merge.json' -Value ($content + ',') -NoNewline"

:: Удаляем первую скобку второго файла и склеиваем
powershell -Command ^
    "$sec = Get-Content '%SECRETS_SRC%' -Raw; " ^
    "$sec = $sec.Trim().TrimStart('{'); " ^
    "Add-Content 'temp_merge.json' -Value $sec"

move /y "temp_merge.json" "appsettings.json" >nul

:: 4. Запуск приложения
echo [INFO] Starting app...
start /wait "" "%EXE_NAME%"

:: 5. Возвращаем всё как было
echo [INFO] Restoring original configuration...
if exist "appsettings.json" del "appsettings.json"
if exist "appsettings.json.bak" (
    ren "appsettings.json.bak" "appsettings.json"
)

echo [SUCCESS] Done.
pause