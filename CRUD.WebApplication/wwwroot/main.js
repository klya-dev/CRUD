const server = "https://localhost:7260";
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

    // При нажатии на кнопку отправки формы идёт запрос к /login для получения токена
    document.getElementById("submitLogin2").addEventListener("click", async e => {
        e.preventDefault();
        // Отправляет запрос и получаем ответ
        const response = await fetch(server + "/login", {
            method: "POST",
            headers: { "Accept": "application/json", "Content-Type": "application/json", "Accept-Language": currentCulture },
            body: JSON.stringify({
                username: document.getElementById("login_username2").value,
                password: document.getElementById("login_password2").value
            })
        });

        // Если запрос прошёл нормально
        if (response.ok === true) {
            // Получаем данные
            const data = await response.json();

            // Изменяем содержимое и видимость блоков на странице
            document.getElementById("userName").innerText = data.username;
            document.getElementById("userInfo").style.display = "flex";
            document.getElementById("authInfo").style.display = "none";

            // Сохраняем в хранилище sessionStorage токен доступа
            sessionStorage.setItem(accessToken, data.access_token);
            sessionStorage.setItem(tokenUsername, data.username);

            console.log("авторизация успешна");

            closeCurrentModal("loginModal");
        }
        else { // Если произошла ошибка
            var errorMessage = await responseError(response);

            // Вставка всех ошибок в элемент
            document.getElementById("login_errors").innerText = errorMessage;
        }
    });

    document.getElementById("submitRegister").addEventListener("click", async e => {
        e.preventDefault();
        const response = await fetch(server + "/register", {
            method: "POST",
            headers: { "Accept": "application/json", "Content-Type": "application/json", "Accept-Language": currentCulture },
            body: JSON.stringify({
                firstname: document.getElementById("reg_firstname").value,
                username: document.getElementById("reg_username").value,
                password: document.getElementById("reg_password").value,
                languagecode: document.getElementById("reg_language_code").value
            })
        });
        if (response.ok === true) {
            const data = await response.json();

            document.getElementById("userName").innerText = data.username;
            document.getElementById("userInfo").style.display = "flex";
            document.getElementById("authInfo").style.display = "none";

            sessionStorage.setItem(accessToken, data.access_token);
            sessionStorage.setItem(tokenUsername, data.username);

            closeCurrentModal("registerModal");
        }
        else {
            var errorMessage = await responseError(response);

            // Вставка всех ошибок в элемент
            document.getElementById("reg_errors").innerText = errorMessage;
        }
    });

    // Условный выход - просто удаляем токен и меняем видимость блоков
    document.getElementById("logOut").addEventListener("click", e => {
        e.preventDefault();
        document.getElementById("userName").innerText = "";
        document.getElementById("userInfo").style.display = "none";
        document.getElementById("authInfo").style.display = "flex";
        sessionStorage.removeItem(accessToken);
        sessionStorage.removeItem(tokenUsername);
    });

    let usernameItem = sessionStorage.getItem(tokenUsername);
    if (sessionStorage.getItem(accessToken) != null && usernameItem != null) {
        document.getElementById("authInfo").style.display = "none";
        document.getElementById("userName").innerText = usernameItem;
        document.getElementById("userInfo").style.display = "flex";
    }
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

function closeCurrentModal(elementIdModal) {
    document.body.classList.remove("modal-open");
    document.body.style.removeProperty("overflow");
    document.body.style.removeProperty("padding-right");
    var modalElement = document.getElementById(elementIdModal);
    modalElement.style.display = "none";
    modalElement.classList.remove("show");
    modalElement.removeAttribute("role-dialog");
    modalElement.removeAttribute("aria-modal");
    modalElement.setAttribute("aria-hidden", true);
    var el = document.getElementsByClassName("modal-backdrop");
    while (el.length > 0) {
        el[0].parentNode.removeChild(el[0]);
    }
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