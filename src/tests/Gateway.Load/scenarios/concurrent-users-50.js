// scenarios/concurrent-users-50.js
// Prueba de carga para 50 usuarios concurrentes - VERSIÓN CONSOLIDADA
// 
// MODOS:
//   SIMPLE: Solo /health y /metrics (sin dependencias)
//   FULL:   Incluye operaciones con microservicios
//
// USO:
//   k6 run scenarios/concurrent-users-50.js                         # Modo FULL
//   k6 run -e TEST_MODE=simple scenarios/concurrent-users-50.js     # Modo SIMPLE

import http from 'k6/http';
import { check, sleep } from 'k6';
import { config, endpoints, generateTestData, validateGatewayResponse, getThresholdsForLevel } from '../utils/config.js';
import { recordRequestMetrics, updateActiveUsers, logMetrics } from '../utils/metrics.js';
import { generateTestUserToken, createAuthHeaders } from '../utils/jwt.js';

const userLevel = 'medium';
const userCount = 50;
const TEST_MODE = __ENV.TEST_MODE || 'full';
const isSimpleMode = TEST_MODE.toLowerCase() === 'simple';

export const options = {
    stages: isSimpleMode ? [
        { duration: '15s', target: 15 },
        { duration: '15s', target: 30 },
        { duration: '15s', target: 50 },
        { duration: '90s', target: 50 },
        { duration: '15s', target: 0 }
    ] : [
        { duration: '1m', target: 15 },
        { duration: '1m', target: 30 },
        { duration: '1m', target: 50 },
        { duration: '5m', target: 50 },
        { duration: '1m', target: 25 },
        { duration: '30s', target: 0 }
    ],
    thresholds: getThresholdsForLevel(userLevel),
    tags: {
        test_type: 'concurrent_users',
        user_count: userCount.toString(),
        scenario: 'medium_load',
        mode: isSimpleMode ? 'simple' : 'full'
    }
};

export function setup() {
    console.log(`🚀 Iniciando prueba de carga MEDIA con ${userCount} usuarios concurrentes`);
    console.log(`📋 Modo: ${TEST_MODE.toUpperCase()}`);

    if (isSimpleMode) {
        console.log(`ℹ️  Modo SIMPLE: Solo endpoints básicos`);

        const token = generateTestUserToken('loadtest-user-50', 'loadtest50@test.com', ['User', 'Tester']);
        const authHeaders = createAuthHeaders(token);
        console.log('✅ Token JWT generado');

        const healthCheck = http.get(`${config.baseUrl}${endpoints.health}`);
        console.log(`✅ Gateway Health: status ${healthCheck.status}`);

        const readyCheck = http.get(`${config.baseUrl}${endpoints.ready}`, { headers: authHeaders });
        console.log(`✅ Gateway Ready: status ${readyCheck.status}`);

        const metricsCheck = http.get(`${config.baseUrl}${endpoints.metrics}`);
        console.log(`✅ Gateway Metrics: status ${metricsCheck.status}`);

        return {
            startTime: Date.now(),
            mode: 'simple',
            token: token,
            authHeaders: authHeaders
        };
    } else {
        console.log(`ℹ️  Modo FULL: Todos los servicios`);

        const services = ['health', 'ready'];
        for (const service of services) {
            const response = http.get(`${config.baseUrl}${endpoints[service]}`);
            if (response.status !== 200) {
                throw new Error(`Servicio ${service} no disponible: ${response.status}`);
            }
        }

        console.log('✅ Todos los servicios disponibles');
        return {
            startTime: Date.now(),
            mode: 'full'
        };
    }
}

export default function concurrentUsersTest(data) {
    updateActiveUsers(__VU);

    if (isSimpleMode) {
        executeSimpleMode(data);
    } else {
        executeFullMode(data);
    }

    if (__ITER % 10 === 0) {
        logMetrics(__ITER);
    }

    const sleepTime = isSimpleMode
        ? Math.random() * 1.5 + 0.5
        : Math.random() * 3 + 0.5;
    sleep(sleepTime);
}

function executeSimpleMode(data) {
    const rand = Math.random();

    if (rand < 0.6) {
        executeHealthCheck();
    } else if (rand < 0.9) {
        executeReadyCheck(data.authHeaders);
    } else {
        executeMetricsCheck();
    }
}

function executeHealthCheck() {
    const response = http.get(`${config.baseUrl}${endpoints.health}`, {
        headers: config.headers,
        tags: { endpoint: 'health', group: 'health_check' }
    });

    check(response, {
        'health check ok': (r) => r.status === 200 || r.status === 503,
        'response under 3s': (r) => r.timings.duration < 3000
    });
    recordRequestMetrics(response, 'gateway');
}

function executeReadyCheck(authHeaders) {
    const response = http.get(`${config.baseUrl}${endpoints.ready}`, {
        headers: authHeaders,
        tags: { endpoint: 'ready', group: 'ready_check' }
    });

    check(response, {
        'ready check ok': (r) => r.status === 200,
        'response under 3s': (r) => r.timings.duration < 3000
    });
    recordRequestMetrics(response, 'gateway');
}

function executeMetricsCheck() {
    const response = http.get(`${config.baseUrl}${endpoints.metrics}`, {
        headers: config.headers,
        tags: { endpoint: 'metrics', group: 'metrics_check' }
    });

    check(response, {
        'metrics check ok': (r) => r.status === 200,
        'response under 3s': (r) => r.timings.duration < 3000
    });
    recordRequestMetrics(response, 'gateway');
}

function executeFullMode(data) {
    const testData = generateTestData();
    const scenario = selectScenario();

    switch (scenario) {
        case 'browsing':
            executeBrowsingPattern(testData);
            break;
        case 'intensive_analysis':
            executeIntensiveAnalysis(testData);
            break;
        case 'report_generation':
            executeReportGeneration(testData);
            break;
        case 'user_management':
            executeUserManagement(testData);
            break;
        default:
            executeHealthChecks();
    }
}

function selectScenario() {
    const rand = Math.random();
    if (rand < 0.05) return 'health_checks';
    else if (rand < 0.4) return 'browsing';
    else if (rand < 0.65) return 'intensive_analysis';
    else if (rand < 0.85) return 'report_generation';
    else return 'user_management';
}

function executeBrowsingPattern(testData) {
    const group = 'browsing_pattern';

    let response = http.get(
        `${config.baseUrl}${endpoints.users.base}${endpoints.users.endpoints.list}`,
        { headers: config.headers, tags: { endpoint: 'users_list', group } }
    );

    check(response, validateGatewayResponse(response, 200), { group });
    recordRequestMetrics(response, 'users');

    sleep(0.5);

    if (response.body) {
        try {
            const users = JSON.parse(response.body);
            if (users.length > 0) {
                const randomUser = users[Math.floor(Math.random() * users.length)];
                response = http.get(
                    `${config.baseUrl}${endpoints.users.base}${endpoints.users.endpoints.getById.replace(':id', randomUser.id)}`,
                    { headers: config.headers, tags: { endpoint: 'users_get', group } }
                );
                check(response, validateGatewayResponse(response, 200), { group });
                recordRequestMetrics(response, 'users');
            }
        } catch (e) {
            console.error(`Error: ${e.message}`);
        }
    }
}

function executeIntensiveAnalysis(testData) {
    const group = 'intensive_analysis';

    let response = http.post(
        `${config.baseUrl}${endpoints.analysis.base}${endpoints.analysis.endpoints.analyze}`,
        JSON.stringify(testData.analysis),
        { headers: config.headers, tags: { endpoint: 'analysis_start', group } }
    );

    const analyzeCheck = check(response, validateGatewayResponse(response, 202), { group });
    recordRequestMetrics(response, 'analysis');

    if (analyzeCheck && response.body) {
        try {
            const analysisResult = JSON.parse(response.body);
            sleep(1);

            response = http.get(
                `${config.baseUrl}${endpoints.analysis.base}${endpoints.analysis.endpoints.status.replace(':id', analysisResult.id)}`,
                { headers: config.headers, tags: { endpoint: 'analysis_status', group } }
            );
            check(response, validateGatewayResponse(response, 200), { group });
            recordRequestMetrics(response, 'analysis');
        } catch (e) {
            console.error(`Error: ${e.message}`);
        }
    }
}

function executeReportGeneration(testData) {
    const group = 'report_generation';

    let response = http.post(
        `${config.baseUrl}${endpoints.reports.base}${endpoints.reports.endpoints.generate}`,
        JSON.stringify(testData.report),
        { headers: config.headers, tags: { endpoint: 'reports_generate', group } }
    );

    const generateCheck = check(response, validateGatewayResponse(response, 202), { group });
    recordRequestMetrics(response, 'reports');

    if (generateCheck) {
        sleep(0.5);
        response = http.get(
            `${config.baseUrl}${endpoints.reports.base}${endpoints.reports.endpoints.list}`,
            { headers: config.headers, tags: { endpoint: 'reports_list', group } }
        );
        check(response, validateGatewayResponse(response, 200), { group });
        recordRequestMetrics(response, 'reports');
    }
}

function executeUserManagement(testData) {
    const group = 'user_management';

    let response = http.post(
        `${config.baseUrl}${endpoints.users.base}${endpoints.users.endpoints.create}`,
        JSON.stringify(testData.user),
        { headers: config.headers, tags: { endpoint: 'users_create', group } }
    );

    const createCheck = check(response, validateGatewayResponse(response, 201), { group });
    recordRequestMetrics(response, 'users');

    if (createCheck && response.body) {
        try {
            const createdUser = JSON.parse(response.body);
            sleep(0.5);

            response = http.get(
                `${config.baseUrl}${endpoints.users.base}${endpoints.users.endpoints.getById.replace(':id', createdUser.id)}`,
                { headers: config.headers, tags: { endpoint: 'users_get', group } }
            );
            check(response, validateGatewayResponse(response, 200), { group });
            recordRequestMetrics(response, 'users');
        } catch (e) {
            console.error(`Error: ${e.message}`);
        }
    }
}

function executeHealthChecks() {
    const group = 'health_checks';

    let response = http.get(`${config.baseUrl}${endpoints.health}`, {
        headers: config.headers,
        tags: { endpoint: 'health', group }
    });
    check(response, validateGatewayResponse(response, 200), { group });
    recordRequestMetrics(response, 'gateway');

    response = http.get(`${config.baseUrl}${endpoints.ready}`, {
        headers: config.headers,
        tags: { endpoint: 'ready', group }
    });
    check(response, validateGatewayResponse(response, 200), { group });
    recordRequestMetrics(response, 'gateway');
}

export function teardown(data) {
    const duration = (Date.now() - data.startTime) / 1000;
    console.log(`✅ Prueba completada en ${duration.toFixed(2)} segundos`);
    console.log(`📊 Modo: ${data.mode.toUpperCase()}`);
    console.log(`📊 Usuarios concurrentes máximos: ${userCount}`);
}
