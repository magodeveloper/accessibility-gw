// scenarios/concurrent-users-100.js
// Escenario de prueba de carga para 100 usuarios concurrentes

import http from 'k6/http';
import { check, sleep } from 'k6';
import { config, endpoints, generateTestData, validateGatewayResponse, getConfigForUsers } from '../utils/config.js';
import { recordRequestMetrics, updateActiveUsers, logMetrics, getThresholdsForLevel } from '../utils/metrics.js';

const userLevel = 'high';
const userCount = 100;

// Configuración de la prueba con ramp-up más gradual
export let options = {
    stages: [
        { duration: '2m', target: 25 },       // Ramp-up inicial suave
        { duration: '2m', target: 50 },       // Incremento a media carga
        { duration: '2m', target: 75 },       // Incremento a alta carga
        { duration: '2m', target: userCount }, // Alcanzar 100 usuarios
        { duration: '10m', target: userCount }, // Mantener 100 usuarios
        { duration: '2m', target: 50 },       // Ramp-down gradual
        { duration: '1m', target: 0 }         // Finalizar
    ],

    thresholds: getThresholdsForLevel(userLevel),

    tags: {
        test_type: 'concurrent_users',
        user_count: userCount.toString(),
        scenario: 'high_load'
    }
};

// Variables globales para coordinación entre VUs
export let sharedData = {
    activeAnalyses: 0,
    activeReports: 0
};

export function setup() {
    console.log(`🚀 Iniciando prueba de carga ALTA con ${userCount} usuarios concurrentes`);

    // Verificar capacidad del sistema antes de la prueba
    const preTestChecks = [
        { endpoint: endpoints.health, expectedStatus: 200 },
        { endpoint: endpoints.ready, expectedStatus: 200 },
        { endpoint: endpoints.metrics, expectedStatus: 200 }
    ];

    for (const check of preTestChecks) {
        const response = http.get(`${config.baseUrl}${check.endpoint}`);
        if (response.status !== check.expectedStatus) {
            throw new Error(`Pre-test check failed for ${check.endpoint}: ${response.status}`);
        }
    }

    console.log('✅ Sistema preparado para carga alta');
    return {
        startTime: Date.now(),
        warningThreshold: 0.02, // 2% error rate warning
        criticalThreshold: 0.05  // 5% error rate critical
    };
}

export default function (data) {
    updateActiveUsers(__VU);

    const testData = generateTestData();
    const scenario = selectWeightedScenario(__VU, __ITER);

    try {
        switch (scenario) {
            case 'light_browsing':
                executeLightBrowsing(testData);
                break;
            case 'heavy_analysis':
                executeHeavyAnalysis(testData);
                break;
            case 'bulk_operations':
                executeBulkOperations(testData);
                break;
            case 'mixed_workload':
                executeMixedWorkload(testData);
                break;
            case 'stress_test':
                executeStressTest(testData);
                break;
            default:
                executeHealthChecks();
        }
    } catch (error) {
        console.error(`Error in VU ${__VU}, iteration ${__ITER}: ${error.message}`);
    }

    logMetrics(__ITER);

    // Sleep variable basado en el VU para distribución temporal
    const sleepTime = (Math.random() * 2) + (0.5 + (__VU % 5) * 0.1);
    sleep(sleepTime);
}

function selectWeightedScenario(vu, iteration) {
    const rand = Math.random();
    const vuMod = vu % 10;

    // Distribución basada en VU para evitar hotspots
    if (vuMod < 2) return 'light_browsing';        // 20%
    else if (vuMod < 5) return 'heavy_analysis';   // 30%
    else if (vuMod < 7) return 'bulk_operations';  // 20%
    else if (vuMod < 9) return 'mixed_workload';   // 20%
    else return 'stress_test';                     // 10%
}

function executeLightBrowsing(testData) {
    const group = 'light_browsing';

    // Navegación eficiente con cache hits simulados
    const endpoints_sequence = [
        { url: `${endpoints.users.base}${endpoints.users.endpoints.list}`, service: 'users' },
        { url: `${endpoints.reports.base}${endpoints.reports.endpoints.list}`, service: 'reports' },
        { url: `${endpoints.analysis.base}/dashboard`, service: 'analysis' }
    ];

    for (const endpoint of endpoints_sequence) {
        const response = http.get(
            `${config.baseUrl}${endpoint.url}`,
            {
                headers: {
                    ...config.headers,
                    'Cache-Control': 'max-age=300' // Simular cache
                },
                tags: {
                    endpoint: endpoint.url.split('/').pop(),
                    group,
                    cache_strategy: 'conditional'
                }
            }
        );

        check(response, validateGatewayResponse(response, 200), { group });
        recordRequestMetrics(response, endpoint.service);

        sleep(0.3); // Tiempo de lectura
    }
}

function executeHeavyAnalysis(testData) {
    const group = 'heavy_analysis';

    // Múltiples análisis paralelos simulados
    const analysisTypes = ['accessibility', 'performance', 'seo', 'security'];
    const batchData = {
        urls: Array.from({ length: 5 }, (_, i) => `https://example${i}.com`),
        options: testData.analysis.options,
        priority: 'high'
    };

    // Batch analysis request
    let response = http.post(
        `${config.baseUrl}${endpoints.analysis.base}${endpoints.analysis.endpoints.batch}`,
        JSON.stringify(batchData),
        {
            headers: config.headers,
            tags: { endpoint: 'analysis_batch', group }
        }
    );

    const batchCheck = check(response, validateGatewayResponse(response, 202), { group });
    recordRequestMetrics(response, 'analysis');

    if (batchCheck && response.body) {
        try {
            const batchResult = JSON.parse(response.body);
            const batchId = batchResult.batchId;

            // Polling para resultados con backoff exponencial
            let pollAttempts = 0;
            const maxPolls = 5;

            while (pollAttempts < maxPolls) {
                const pollDelay = Math.pow(2, pollAttempts) * 0.5; // 0.5s, 1s, 2s, 4s, 8s
                sleep(pollDelay);

                response = http.get(
                    `${config.baseUrl}${endpoints.analysis.base}/batch/${batchId}/status`,
                    {
                        headers: config.headers,
                        tags: { endpoint: 'analysis_batch_status', group, poll_attempt: pollAttempts.toString() }
                    }
                );

                const statusCheck = check(response, validateGatewayResponse(response, 200), { group });
                recordRequestMetrics(response, 'analysis');

                if (statusCheck && response.body) {
                    const status = JSON.parse(response.body);
                    if (status.completed || status.failed) break;
                }

                pollAttempts++;
            }

        } catch (e) {
            console.error(`Error in heavy analysis: ${e.message}`);
        }
    }
}

function executeBulkOperations(testData) {
    const group = 'bulk_operations';

    // Operaciones en lote para usuarios
    const bulkUsers = Array.from({ length: 10 }, (_, i) => ({
        ...testData.user,
        name: `${testData.user.name}_bulk_${i}`,
        email: `bulk_${i}_${testData.user.email}`
    }));

    let response = http.post(
        `${config.baseUrl}${endpoints.users.base}/bulk`,
        JSON.stringify({ users: bulkUsers }),
        {
            headers: config.headers,
            tags: { endpoint: 'users_bulk_create', group }
        }
    );

    const bulkCheck = check(response, validateGatewayResponse(response, 201), { group });
    recordRequestMetrics(response, 'users');

    if (bulkCheck) {
        sleep(1);

        // Bulk update preferences
        const bulkPreferences = bulkUsers.map((user, i) => ({
            email: user.email,
            preferences: {
                ...user.preferences,
                bulkUpdated: true,
                updateIndex: i
            }
        }));

        response = http.put(
            `${config.baseUrl}${endpoints.users.base}/bulk/preferences`,
            JSON.stringify({ updates: bulkPreferences }),
            {
                headers: config.headers,
                tags: { endpoint: 'users_bulk_preferences', group }
            }
        );

        check(response, validateGatewayResponse(response, 200), { group });
        recordRequestMetrics(response, 'users');
    }
}

function executeMixedWorkload(testData) {
    const group = 'mixed_workload';

    // Workflow realista: crear usuario → analizar → generar reporte
    let response = http.post(
        `${config.baseUrl}${endpoints.users.base}${endpoints.users.endpoints.create}`,
        JSON.stringify(testData.user),
        {
            headers: config.headers,
            tags: { endpoint: 'users_create', group }
        }
    );

    const userCheck = check(response, validateGatewayResponse(response, 201), { group });
    recordRequestMetrics(response, 'users');

    if (userCheck && response.body) {
        const user = JSON.parse(response.body);

        sleep(0.5);

        // Análisis asociado al usuario
        const userAnalysis = {
            ...testData.analysis,
            userId: user.id,
            userContext: true
        };

        response = http.post(
            `${config.baseUrl}${endpoints.analysis.base}${endpoints.analysis.endpoints.analyze}`,
            JSON.stringify(userAnalysis),
            {
                headers: config.headers,
                tags: { endpoint: 'analysis_user_context', group }
            }
        );

        const analysisCheck = check(response, validateGatewayResponse(response, 202), { group });
        recordRequestMetrics(response, 'analysis');

        if (analysisCheck && response.body) {
            const analysis = JSON.parse(response.body);

            sleep(1);

            // Reporte personalizado
            const userReport = {
                ...testData.report,
                userId: user.id,
                analysisId: analysis.id,
                personalized: true
            };

            response = http.post(
                `${config.baseUrl}${endpoints.reports.base}${endpoints.reports.endpoints.generate}`,
                JSON.stringify(userReport),
                {
                    headers: config.headers,
                    tags: { endpoint: 'reports_personalized', group }
                }
            );

            check(response, validateGatewayResponse(response, 202), { group });
            recordRequestMetrics(response, 'reports');
        }
    }
}

function executeStressTest(testData) {
    const group = 'stress_test';

    // Requests rápidos consecutivos para probar limits
    const rapidRequests = [
        `${endpoints.health}`,
        `${endpoints.ready}`,
        `${endpoints.users.base}${endpoints.users.endpoints.list}`,
        `${endpoints.analysis.base}/dashboard`,
        `${endpoints.reports.base}${endpoints.reports.endpoints.list}`
    ];

    for (const endpoint of rapidRequests) {
        const response = http.get(
            `${config.baseUrl}${endpoint}`,
            {
                headers: config.headers,
                tags: {
                    endpoint: endpoint.split('/').pop(),
                    group,
                    test_type: 'rapid_fire'
                }
            }
        );

        // Más tolerante a errores en stress test
        check(response, {
            'status acceptable': (r) => r.status < 500,
            'response received': (r) => r.body !== null
        }, { group });

        recordRequestMetrics(response, 'gateway');

        // Sin sleep para stress máximo
    }
}

function executeHealthChecks() {
    const group = 'health_checks';

    const response = http.get(`${config.baseUrl}${endpoints.health}`, {
        headers: config.headers,
        tags: { endpoint: 'health', group }
    });

    check(response, validateGatewayResponse(response, 200), { group });
    recordRequestMetrics(response, 'gateway');
}

export function teardown(data) {
    const duration = (Date.now() - data.startTime) / 1000;
    console.log(`✅ Prueba de carga ALTA completada en ${duration.toFixed(2)} segundos`);
    console.log(`📊 Usuarios concurrentes máximos: ${userCount}`);
    console.log(`⚡ Carga: Alta - Patrones intensivos y mixtos`);
    console.log(`⚠️  Threshold de warning: ${data.warningThreshold * 100}%`);
    console.log(`🚨 Threshold crítico: ${data.criticalThreshold * 100}%`);
}