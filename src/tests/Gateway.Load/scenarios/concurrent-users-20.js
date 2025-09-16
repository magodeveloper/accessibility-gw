// scenarios/concurrent-users-20.js
// Escenario de prueba de carga para 20 usuarios concurrentes

import http from 'k6/http';
import { check, sleep } from 'k6';
import { config, endpoints, generateTestData, validateGatewayResponse, getConfigForUsers } from '../utils/config.js';
import { recordRequestMetrics, updateActiveUsers, logMetrics, getThresholdsForLevel } from '../utils/metrics.js';

const userLevel = 'light';
const userCount = 20;

// Configuración de la prueba
export let options = {
    stages: [
        { duration: '30s', target: 5 },      // Ramp-up suave
        { duration: '1m', target: userCount }, // Alcanzar 20 usuarios
        { duration: '3m', target: userCount }, // Mantener 20 usuarios
        { duration: '30s', target: 0 }        // Ramp-down
    ],

    thresholds: getThresholdsForLevel(userLevel),

    tags: {
        test_type: 'concurrent_users',
        user_count: userCount.toString(),
        scenario: 'steady_state'
    }
};

// Función de setup para preparar datos de prueba
export function setup() {
    console.log(`🚀 Iniciando prueba de carga con ${userCount} usuarios concurrentes`);

    // Verificar que el Gateway esté disponible
    const healthCheck = http.get(`${config.baseUrl}${endpoints.health}`);
    if (healthCheck.status !== 200) {
        throw new Error(`Gateway no está disponible: ${healthCheck.status}`);
    }

    console.log('✅ Gateway está disponible y listo para las pruebas');
    return { startTime: Date.now() };
}

// Función principal de la prueba
export default function (data) {
    const iterationStart = Date.now();
    updateActiveUsers(__VU);

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

    // Log de métricas periódico
    logMetrics(__ITER);

    // Sleep entre requests (simulate user think time)
    sleep(Math.random() * 2 + 1); // 1-3 segundos
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
    console.log(`📊 Usuarios concurrentes máximos: ${userCount}`);
}