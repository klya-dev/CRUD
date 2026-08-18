let server = null;

const accessToken = "accessToken";
const tokenUsername = "username";
const defaultCurture = "ru";

var url = window.location.search;
const urlParams = new URLSearchParams(url);
var culture = urlParams.get('culture');
if (culture == null)
    culture = defaultCurture;
const currentCulture = culture;

document.addEventListener('DOMContentLoaded', function () {
    // Запускаем асинхронную загрузку
    fetch('/api/url')
        .then(res => {
            if (!res.ok) throw new Error(res.status);
            return res.json();
        })
        .then(data => {
            server = data; // Записываем данные в глобальную переменную
        })
        .catch(err => console.error('Ошибка конфигурации:', err));
}, false);

async function responseError(response) {
    console.log("Status: ", response.status);
    const data = await response.json();
    let errorMessage = "";

    // Проверка на наличие ошибок валидации
    if (data.errors && !Array.isArray(data.errors)) {
        for (const [field, messages] of Object.entries(data.errors)) {
            if (currentCulture == "ru")
                errorMessage += `Поле: ${field}\n`;
            else if (currentCulture == "en")
                errorMessage += `Field: ${field}\n`;

            messages.forEach(msg => {
                errorMessage += `- ${msg}\n`;
            });
        }
    }

    // Проверка на наличие ошибок из сервиса
    if (data.detail) {
        errorMessage = data.title;
    }

    return errorMessage;
}

function IsInRole(role) {
    if (sessionStorage.getItem(accessToken) == null)
        return false;

    // Разделяем токен по точке и берем вторую часть
    const payload = JSON.parse(atob(sessionStorage.getItem(accessToken).split('.')[1]));

    // Проверяем наличие указанной роли в данных токена
    return payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] === role;
}

// Получить UserId из токена
function GetUserId() {
    if (sessionStorage.getItem("accessToken") == null)
        return false;

    // Разделяем токен по точке и берем вторую часть
    const payload = JSON.parse(atob(sessionStorage.getItem("accessToken").split('.')[1]));

    return payload["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"];
}