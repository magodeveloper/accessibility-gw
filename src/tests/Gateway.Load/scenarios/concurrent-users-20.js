// scenarios/concurrent-users-20.js
// Prueba de carga para 20 usuarios concurrentes - VERSIÓN CONSOLIDADA
// 
// MODOS:
//   SIMPLE: Solo /health y /metrics (sin dependencias)
//   FULL:   Incluye operaciones con microservicios
//
// USO:
//   k6 run scenarios/concurrent-users-20.js                         # Modo FULL
//   k6 run -e TEST_MODE=simple scenarios/concurrent-users-20.js     # Modo SIMPLE

import http from 'k6/http';
import { check, sleep } from 'k6';
import { config, endpoints, generateTestData, validateGatewayResponse, getThresholdsForLevel } from '../utils/config.js';
import { recordRequestMetrics, updateActiveUsers, logMetrics } from '../utils/metrics.js';
import { generateTestUserToken, createAuthHeaders } from '../utils/jwt.js';

const userLevel = 'light';
const userCount = 20;
const TEST_MODE = __ENV.TEST_MODE || 'full';
const isSimpleMode = TEST_MODE.toLowerCase() === 'simple';

export const options = {
    stages: isSimpleMode ? [
        { duration: '10s', target: 5 },
        { duration: '20s', target: 20 },
        { duration: '60s', target: 20 },
        { duration: '10s', target: 0 }
    ] : [
        { duration: '30s', target: 5 },
        { duration: '1m', target: 20 },
        { duration: '3m', target: 20 },
        { duration: '30s', target: 0 }
    ],
    thresholds: getThresholdsForLevel(userLevel),
    tags: {
        test_type: 'concurrent_users',
        user_count: userCount.toString(),
        scenario: 'steady_state',
        mode: 'full'
    }
};

// Función de setup para preparar datos de prueba
export function setup() {
    console.log(`🚀 Iniciando prueba de carga con ${userCount} usuarios concurrentes`);
    console.log(`📋 Modo: ${TEST_MODE.toUpperCase()}`);

    if (isSimpleMode) {
        // Setup para modo SIMPLE
        console.log(`ℹ️  Modo SIMPLE: Solo endpoints básicos (/health, /ready, /metrics)`);

        // Generar token JWT para autenticación
        const token = generateTestUserToken('load-test-user-20');
        const authHeaders = createAuthHeaders(token);
        console.log('✅ Token JWT generado para pruebas');

        // Verificar que el Gateway esté disponible
        const healthCheck = http.get(`${config.baseUrl}${endpoints.health}`);
        if (healthCheck.status === 0 || healthCheck.error) {
            throw new Error(`Gateway no accesible: ${healthCheck.error || 'Conexión fallida'}`);
        }
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
        // Setup para modo FULL
        console.log(`ℹ️  Modo FULL: Todos los servicios (usuarios, análisis, reportes)`);

        // Verificar que el Gateway esté disponible
        const healthCheck = http.get(`${config.baseUrl}${endpoints.health}`);
        if (healthCheck.status !== 200) {
            throw new Error(`Gateway no está disponible: ${healthCheck.status}`);
        }

        console.log('✅ Gateway está disponible y listo para las pruebas');
        return {
            startTime: Date.now(),
            mode: 'full'
        };
    }
}

// Función principal de la prueba
export default function concurrentUsersTest(data) {
    updateActiveUsers(__VU);

    if (isSimpleMode) {
        // Modo SIMPLE: Solo endpoints básicos
        executeSimpleMode(data);
    } else {
        // Modo FULL: Escenarios completos
        executeFullMode(data);
    }

    // Log de métricas periódico
    if (__ITER % 10 === 0) {
        logMetrics(__ITER);
    }

    // Sleep entre requests (simulate user think time)
    const sleepTime = isSimpleMode
        ? Math.random() * 1.5 + 0.5  // 0.5-2 segundos en modo simple
        : Math.random() * 2 + 1;      // 1-3 segundos en modo full
    sleep(sleepTime);
}

// ============================================
// MODO SIMPLE: Solo endpoints básicos
// ============================================
function executeSimpleMode(data) {
    // Distribuir requests entre los 3 endpoints disponibles
    const rand = Math.random();

    if (rand < 0.6) {
        // 60%: Health check
        executeHealthCheck();
    } else if (rand < 0.9) {
        // 30%: Ready check (requiere auth)
        executeReadyCheck(data.authHeaders);
    } else {
        // 10%: Metrics check
        executeMetricsCheck();
    }
}

function executeHealthCheck() {
    const group = 'health_check';
    const response = http.get(`${config.baseUrl}${endpoints.health}`, {
        headers: config.headers,
        tags: { endpoint: 'health', group }
    });

    check(response, {
        'health check ok': (r) => r.status === 200 || r.status === 503,
        'response under 3s': (r) => r.timings.duration < 3000
    });
    recordRequestMetrics(response, 'gateway');
}

function executeReadyCheck(authHeaders) {
    const group = 'ready_check';
    const response = http.get(`${config.baseUrl}${endpoints.ready}`, {
        headers: authHeaders,
        tags: { endpoint: 'ready', group }
    });

    check(response, {
        'ready check ok': (r) => r.status === 200,
        'response under 3s': (r) => r.timings.duration < 3000
    });
    recordRequestMetrics(response, 'gateway');
}

function executeMetricsCheck() {
    const group = 'metrics_check';
    const response = http.get(`${config.baseUrl}${endpoints.metrics}`, {
        headers: config.headers,
        tags: { endpoint: 'metrics', group }
    });

    check(response, {
        'metrics check ok': (r) => r.status === 200,
        'response under 3s': (r) => r.timings.duration < 3000
    });
    recordRequestMetrics(response, 'gateway');
}

// ============================================
// MODO FULL: Todos los servicios
// ============================================
function executeFullMode(data) {
    // Generar datos de prueba únicos para cada iteración
    const testData = generateTestData();

    // Escenario 1: Health checks (10% del tráfico)
    if (Math.random() < 0.1) {
        executeHealthChecks();
    }
    // Escenario 2: Operaciones de usuarios (60% del tráfico)
    else if (Math.random() < 0.7) {
        executeUserOperations(testData.user);
    }
    // Escenario 3: Análisis de accesibilidad (20% del tráfico)
    else if (Math.random() < 0.9) {
        executeAnalysisOperations(testData.analysis);
    }
    // Escenario 4: Generación de reportes (10% del tráfico)
    else {
        executeReportOperations(testData.report);
    }
}

function executeHealthChecks() {
    const group = 'health_checks';

    // Health check
    let response = http.get(`${config.baseUrl}${endpoints.health}`, {
        headers: config.headers,
        tags: { endpoint: 'health', group }
    });

    check(response, validateGatewayResponse(response, 200), { group });
    recordRequestMetrics(response, 'gateway');

    // Ready check
    response = http.get(`${config.baseUrl}${endpoints.ready}`, {
        headers: config.headers,
        tags: { endpoint: 'ready', group }
    });

    check(response, validateGatewayResponse(response, 200), { group });
    recordRequestMetrics(response, 'gateway');
}

function executeUserOperations(userData) {
    const group = 'user_operations';

    // Crear usuario
    let response = http.post(
        `${config.baseUrl}${endpoints.users.base}${endpoints.users.endpoints.create}`,
        JSON.stringify(userData),
        {
            headers: config.headers,
            tags: { endpoint: 'users_create', group }
        }
    );

    const createCheck = check(response, validateGatewayResponse(response, 201), { group });
    recordRequestMetrics(response, 'users');

    if (createCheck && response.body) {
        try {
            const createdUser = JSON.parse(response.body);
            const userId = createdUser.id;

            sleep(0.5);

            // Obtener usuario creado
            response = http.get(
                `${config.baseUrl}${endpoints.users.base}${endpoints.users.endpoints.getById.replace(':id', userId)}`,
                {
                    headers: config.headers,
                    tags: { endpoint: 'users_get', group }
                }
            );

            check(response, validateGatewayResponse(response, 200), { group });
            recordRequestMetrics(response, 'users');

            sleep(0.5);

            // Actualizar preferencias
            const updatedPreferences = {
                ...userData.preferences,
                lastUpdated: new Date().toISOString()
            };

            response = http.put(
                `${config.baseUrl}${endpoints.users.base}${endpoints.users.endpoints.preferences.replace(':id', userId)}`,
                JSON.stringify(updatedPreferences),
                {
                    headers: config.headers,
                    tags: { endpoint: 'users_preferences', group }
                }
            );

            check(response, validateGatewayResponse(response, 200), { group });
            recordRequestMetrics(response, 'users');

        } catch (e) {
            console.error(`Error parsing user response: ${e.message}`);
        }
    }
}

function executeAnalysisOperations(analysisData) {
    const group = 'analysis_operations';

    // Iniciar análisis
    let response = http.post(
        `${config.baseUrl}${endpoints.analysis.base}${endpoints.analysis.endpoints.analyze}`,
        JSON.stringify(analysisData),
        {
            headers: config.headers,
            tags: { endpoint: 'analysis_start', group }
        }
    );

    const analyzeCheck = check(response, validateGatewayResponse(response, 202), { group });
    recordRequestMetrics(response, 'analysis');

    if (analyzeCheck && response.body) {
        try {
            const analysisResult = JSON.parse(response.body);
            const analysisId = analysisResult.id;

            sleep(1);

            // Verificar estado del análisis
            response = http.get(
                `${config.baseUrl}${endpoints.analysis.base}${endpoints.analysis.endpoints.status.replace(':id', analysisId)}`,
                {
                    headers: config.headers,
                    tags: { endpoint: 'analysis_status', group }
                }
            );

            check(response, validateGatewayResponse(response, 200), { group });
            recordRequestMetrics(response, 'analysis');

        } catch (e) {
            console.error(`Error parsing analysis response: ${e.message}`);
        }
    }
}

function executeReportOperations(reportData) {
    const group = 'report_operations';

    // Generar reporte
    let response = http.post(
        `${config.baseUrl}${endpoints.reports.base}${endpoints.reports.endpoints.generate}`,
        JSON.stringify(reportData),
        {
            headers: config.headers,
            tags: { endpoint: 'reports_generate', group }
        }
    );

    const generateCheck = check(response, validateGatewayResponse(response, 202), { group });
    recordRequestMetrics(response, 'reports');

    if (generateCheck) {
        sleep(0.5);

        // Listar reportes disponibles
        response = http.get(
            `${config.baseUrl}${endpoints.reports.base}${endpoints.reports.endpoints.list}`,
            {
                headers: config.headers,
                tags: { endpoint: 'reports_list', group }
            }
        );

        check(response, validateGatewayResponse(response, 200), { group });
        recordRequestMetrics(response, 'reports');
    }
}

// Función de teardown
export function teardown(data) {
    const duration = (Date.now() - data.startTime) / 1000;
    console.log(`✅ Prueba completada en ${duration.toFixed(2)} segundos`);
    console.log(`📊 Modo: ${data.mode.toUpperCase()}`);
    console.log(`📊 Usuarios concurrentes máximos: ${userCount}`);
}