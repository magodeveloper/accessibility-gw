// scenarios/concurrent-users-100.js
// Prueba de carga para 100 usuarios concurrentes - VERSIÓN CONSOLIDADA
// 
// MODOS:
//   SIMPLE: Solo /health y /metrics (sin dependencias)
//   FULL:   Incluye operaciones con microservicios
//
// USO:
//   k6 run scenarios/concurrent-users-100.js                         # Modo FULL
//   k6 run -e TEST_MODE=simple scenarios/concurrent-users-100.js     # Modo SIMPLE

import http from 'k6/http';
import { check, sleep } from 'k6';
import { config, endpoints, generateTestData, validateGatewayResponse, getThresholdsForLevel } from '../utils/config.js';
import { recordRequestMetrics, updateActiveUsers, logMetrics } from '../utils/metrics.js';
import { generateTestUserToken, createAuthHeaders } from '../utils/jwt.js';

const userLevel = 'high';
const userCount = 100;
const TEST_MODE = __ENV.TEST_MODE || 'full';
const isSimpleMode = TEST_MODE.toLowerCase() === 'simple';

export const options = {
    stages: isSimpleMode ? [
        { duration: '20s', target: 25 },
        { duration: '20s', target: 50 },
        { duration: '20s', target: 100 },
        { duration: '120s', target: 100 },
        { duration: '20s', target: 0 }
    ] : [
        { duration: '2m', target: 25 },
        { duration: '2m', target: 50 },
        { duration: '2m', target: 75 },
        { duration: '2m', target: 100 },
        { duration: '10m', target: 100 },
        { duration: '2m', target: 50 },
        { duration: '1m', target: 0 }
    ],
    thresholds: getThresholdsForLevel(userLevel),
    tags: {
        test_type: 'concurrent_users',
        user_count: userCount.toString(),
        scenario: 'high_load',
        mode: isSimpleMode ? 'simple' : 'full'
    }
};

export function setup() {
    console.log(`🚀 Iniciando prueba de carga ALTA con ${userCount} usuarios concurrentes`);
    console.log(`📋 Modo: ${TEST_MODE.toUpperCase()}`);

    if (isSimpleMode) {
        console.log(`ℹ️  Modo SIMPLE: Solo endpoints básicos`);

        const token = generateTestUserToken('loadtest-user-100');
        const authHeaders = createAuthHeaders(token);

        const healthCheck = http.get(`${config.baseUrl}${endpoints.health}`);
        console.log(`✅ Health: ${healthCheck.status}`);

        return {
            startTime: Date.now(),
            mode: 'simple',
            authHeaders: authHeaders
        };
    } else {
        console.log(`ℹ️  Modo FULL: Todos los servicios`);

        const healthCheck = http.get(`${config.baseUrl}${endpoints.health}`);
        if (healthCheck.status !== 200) {
            throw new Error(`Gateway no disponible: ${healthCheck.status}`);
        }

        console.log('✅ Gateway disponible');
        return {
            startTime: Date.now(),
            mode: 'full'
        };
    }
}

export default function concurrentUsersTest(data) {
    updateActiveUsers(__VU);

    if (isSimpleMode) {
        const rand = Math.random();
        if (rand < 0.7) {
            executeHealthCheck();
        } else {
            executeMetricsCheck();
        }
    } else {
        const testData = generateTestData();
        const rand = Math.random();

        if (rand < 0.5) {
            executeUserOperations(testData.user);
        } else if (rand < 0.8) {
            executeAnalysisOperations(testData.analysis);
        } else {
            executeReportOperations(testData.report);
        }
    }

    if (__ITER % 20 === 0) {
        logMetrics(__ITER);
    }

    sleep(isSimpleMode ? Math.random() + 0.5 : Math.random() * 2 + 1);
}

function executeHealthCheck() {
    const response = http.get(`${config.baseUrl}${endpoints.health}`, {
        headers: config.headers,
        tags: { endpoint: 'health', group: 'simple' }
    });
    check(response, { 'health ok': (r) => r.status === 200 || r.status === 503 });
    recordRequestMetrics(response, 'gateway');
}

function executeMetricsCheck() {
    const response = http.get(`${config.baseUrl}${endpoints.metrics}`, {
        headers: config.headers,
        tags: { endpoint: 'metrics', group: 'simple' }
    });
    check(response, { 'metrics ok': (r) => r.status === 200 });
    recordRequestMetrics(response, 'gateway');
}

function executeUserOperations(userData) {
    let response = http.post(
        `${config.baseUrl}${endpoints.users.base}${endpoints.users.endpoints.create}`,
        JSON.stringify(userData),
        { headers: config.headers, tags: { endpoint: 'users_create', group: 'full' } }
    );
    check(response, validateGatewayResponse(response, 201));
    recordRequestMetrics(response, 'users');
}

function executeAnalysisOperations(analysisData) {
    let response = http.post(
        `${config.baseUrl}${endpoints.analysis.base}${endpoints.analysis.endpoints.analyze}`,
        JSON.stringify(analysisData),
        { headers: config.headers, tags: { endpoint: 'analysis', group: 'full' } }
    );
    check(response, validateGatewayResponse(response, 202));
    recordRequestMetrics(response, 'analysis');
}

function executeReportOperations(reportData) {
    let response = http.post(
        `${config.baseUrl}${endpoints.reports.base}${endpoints.reports.endpoints.generate}`,
        JSON.stringify(reportData),
        { headers: config.headers, tags: { endpoint: 'reports', group: 'full' } }
    );
    check(response, validateGatewayResponse(response, 202));
    recordRequestMetrics(response, 'reports');
}

export function teardown(data) {
    const duration = (Date.now() - data.startTime) / 1000;
    console.log(`✅ Prueba completada en ${duration.toFixed(2)} segundos`);
    console.log(`📊 Modo: ${data.mode.toUpperCase()}`);
    console.log(`📊 Usuarios concurrentes máximos: ${userCount}`);
}
