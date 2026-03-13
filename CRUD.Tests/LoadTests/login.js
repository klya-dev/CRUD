// https://grafana.com/docs/k6/latest/get-started/running-k6/
// Download K6 https://github.com/grafana/k6/releases

// Учитывай, что в опциях RateLimiter'а, указаны небольшие значения

// Чтобы запустить тест +приложение должно быть запущено:
// k6.exe в папке Portable
// k6 run login.js
// k6 run "E:\Projects\Web\CRUD\CRUD.Tests\LoadTests\login.js"

import http from 'k6/http';
import { sleep, check } from 'k6';

export const options = {
    vus: 3, // Количество виртуальных пользователей
    duration: '15s', // Сколько будет выполняться тест
};

export default function () {
    const url = 'https://localhost:7260/login';
    const payload = JSON.stringify({
        username: 'admin',
        password: '123',
    });

    const params = {
        headers: {
            'Content-Type': 'application/json',
        },
    };

    let res = http.post(url, payload, params);
    check(res, { "status is 200": (res) => res.status === 200 }); // Должен быть указанный статус
    sleep(1); // Задержка в секундах
}