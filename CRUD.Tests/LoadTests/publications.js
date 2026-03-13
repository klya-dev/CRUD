// https://grafana.com/docs/k6/latest/get-started/running-k6/
// Download K6 https://github.com/grafana/k6/releases

// Учитывай, что в опциях RateLimiter'а, указаны небольшие значения

// Чтобы запустить тест +приложение должно быть запущено:
// k6.exe в папке Portable
// k6 run publications.js
// k6 run "E:\Projects\Web\CRUD\CRUD.Tests\LoadTests\publications.js"

import http from 'k6/http';
import { sleep, check } from 'k6';

export const options = {
    vus: 3, // Количество виртуальных пользователей
    duration: '15s', // Сколько будет выполняться тест
};

export default function() {
    let res = http.get('https://localhost:7260/v1/publications?count=1');
    check(res, { "status is 200": (res) => res.status === 200 }); // Должен быть указанный статус
    sleep(1); // Задержка в секундах
}