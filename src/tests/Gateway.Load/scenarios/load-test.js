// scenarios/load-test.js
// Prueba de carga normal - Simula carga típica de producción

import http from 'k6/http';
import { check, sleep } from 'k6';
import { SharedArray } from 'k6/data';
import { randomIntBetween } from 'https://jslib.k6.io/k6-utils/1.2.0/index.js';
import { config, endpoints, generateTestData, logResponse, validateGatewayResponse } from '../utils/config.js';

// Datos compartidos para las pruebas
const userData = new SharedArray('users', function () {
    const users = [];
    for (let i = 0; i < 100; i++) {
        users.push(generateTestData().user);
    }
    return users;
});

// Configuración de carga normal
export const options = {
    stages: [
        { duration: '2m', target: 10 },  // Ramp-up gradual
        { duration: '5m', target: 50 },  // Carga sostenida normal
        { duration: '2m', target: 100 }, // Incremento a carga alta
        { duration: '5m', target: 100 }, // Mantener carga alta
        { duration: '3m', target: 0 },   // Ramp-down
    ],
    thresholds: {
        http_req_duration: ['p(95)<500', 'p(99)<1000'],
        http_req_failed: ['rate<0.02'],
        http_reqs: ['rate>50'],
        checks: ['rate>0.95']
    },

    // Configuración adicional
    noConnectionReuse: false,
    userAgent: 'k6-load-test/1.0',

    tags: {
        test_type: 'load'
    }
};

export default function loadTest() {
    // Simular diferentes tipos de usuarios
    const userType = randomIntBetween(1, 100);

    if (userType <= 40) {
        // 40% - Usuarios navegando y consultando
        simulateRegularUser();
    } else if (userType <= 70) {
        // 30% - Usuarios creando contenido
        simulateActiveUser();
    } else if (userType <= 85) {
        // 15% - Usuarios ejecutando análisis
        simulateAnalysisUser();
    } else {
        // 15% - Usuarios generando reportes
        simulateReportUser();
    }

    // Think time variable
    sleep(randomIntBetween(1, 3));
}

function simulateRegularUser() {
    const group = 'regular_user';

    // Health check ocasional
    if (Math.random() < 0.1) {
        const healthResponse = http.get(`${config.baseUrl}${endpoints.health}`);
        check(healthResponse, validateGatewayResponse(healthResponse, 200), {
            tags: { endpoint: 'health', group }
        });
    }

    // Navegar por usuarios
    const usersResponse = http.get(
        `${config.baseUrl}${endpoints.users.base}${endpoints.users.endpoints.list}`,
        { headers: config.headers }
    );
    logResponse(usersResponse, 'List Users - Regular User');

    check(usersResponse, validateGatewayResponse(usersResponse, 200), {
        tags: { endpoint: 'users_list', group }
    });

    sleep(1);

    // Ver reportes disponibles
    const reportsResponse = http.get(
        `${config.baseUrl}${endpoints.reports.base}${endpoints.reports.endpoints.list}`,
        { headers: config.headers }
    );
    logResponse(reportsResponse, 'List Reports - Regular User');

    check(reportsResponse, validateGatewayResponse(reportsResponse, 200), {
        tags: { endpoint: 'reports_list', group }
    });

    sleep(1);

    // Verificar métricas del sistema
    if (Math.random() < 0.2) {
        const metricsResponse = http.get(`${config.baseUrl}${endpoints.metrics}`);
        check(metricsResponse, validateGatewayResponse(metricsResponse, 200), {
            tags: { endpoint: 'metrics', group }
        });
    }
}

function simulateActiveUser() {
    const group = 'active_user';
    const user = userData[randomIntBetween(0, userData.length - 1)];

    // Crear usuario
    const createResponse = http.post(
        `${config.baseUrl}${endpoints.users.base}${endpoints.users.endpoints.create}`,
        JSON.stringify(user),
        { headers: config.headers }
    );
    logResponse(createResponse, 'Create User - Active User');

    const createCheck = check(createResponse, validateGatewayResponse(createResponse, 201), {
        tags: { endpoint: 'users_create', group }
    });

    if (createCheck && createResponse.body) {
        try {
            const createdUser = JSON.parse(createResponse.body);
            const userId = createdUser.id;

            sleep(1);

            // Obtener y actualizar usuario
            const getResponse = http.get(
                `${config.baseUrl}${endpoints.users.base}${endpoints.users.endpoints.getById.replace(':id', userId)}`,
                { headers: config.headers }
            );
            logResponse(getResponse, 'Get User - Active User');

            check(getResponse, validateGatewayResponse(getResponse, 200), {
                tags: { endpoint: 'users_get', group }
            });

            sleep(1);

            // Actualizar preferencias
            const updatedPreferences = {
                ...user.preferences,
                lastActivity: new Date().toISOString(),
                sessionId: `session_${__VU}_${__ITER}`
            };

            const preferencesResponse = http.put(
                `${config.baseUrl}${endpoints.users.base}${endpoints.users.endpoints.preferences.replace(':id', userId)}`,
                JSON.stringify(updatedPreferences),
                { headers: config.headers }
            );
            logResponse(preferencesResponse, 'Update Preferences - Active User');

            check(preferencesResponse, validateGatewayResponse(preferencesResponse, 200), {
                tags: { endpoint: 'users_preferences', group }
            });

            sleep(1);

            // Obtener perfil completo
            const profileResponse = http.get(
                `${config.baseUrl}${endpoints.users.base}${endpoints.users.endpoints.profile.replace(':id', userId)}`,
                { headers: config.headers }
            );
            logResponse(profileResponse, 'Get Profile - Active User');

            check(profileResponse, validateGatewayResponse(profileResponse, 200), {
                tags: { endpoint: 'users_profile', group }
            });

        } catch (e) {
            console.error(`Error in active user workflow: ${e.message}`);
        }
    }
}

function simulateAnalysisUser() {
    const group = 'analysis_user';
    const testData = generateTestData();

    // Iniciar análisis
    const analysisRequest = {
        ...testData.analysis,
        priority: randomIntBetween(1, 3),
        detailedReport: Math.random() > 0.5
    };

    const analyzeResponse = http.post(
        `${config.baseUrl}${endpoints.analysis.base}${endpoints.analysis.endpoints.analyze}`,
        JSON.stringify(analysisRequest),
        { headers: config.headers }
    );
    logResponse(analyzeResponse, 'Start Analysis - Analysis User');

    const analyzeCheck = check(analyzeResponse, validateGatewayResponse(analyzeResponse, 202), {
        tags: { endpoint: 'analysis_start', group }
    });

    if (analyzeCheck && analyzeResponse.body) {
        try {
            const analysisResult = JSON.parse(analyzeResponse.body);
            const analysisId = analysisResult.id;

            // Monitoreo del análisis con polling
            for (let i = 0; i < 3; i++) {
                sleep(randomIntBetween(2, 5));

                const statusResponse = http.get(
                    `${config.baseUrl}${endpoints.analysis.base}${endpoints.analysis.endpoints.status.replace(':id', analysisId)}`,
                    { headers: config.headers }
                );
                logResponse(statusResponse, `Check Analysis Status ${i + 1} - Analysis User`);

                const statusCheck = check(statusResponse, validateGatewayResponse(statusResponse, 200), {
                    tags: { endpoint: 'analysis_status', group, poll_iteration: (i + 1).toString() }
                });

                if (statusCheck && statusResponse.body) {
                    const status = JSON.parse(statusResponse.body);
                    if (status.completed) {
                        console.log(`Analysis ${analysisId} completed`);
                        break;
                    }
                }
            }

        } catch (e) {
            console.error(`Error in analysis user workflow: ${e.message}`);
        }
    }

    // Obtener lista de análisis previos
    const reportsResponse = http.get(
        `${config.baseUrl}${endpoints.analysis.base}${endpoints.analysis.endpoints.reports}`,
        { headers: config.headers }
    );
    logResponse(reportsResponse, 'Get Analysis Reports - Analysis User');

    check(reportsResponse, validateGatewayResponse(reportsResponse, 200), {
        tags: { endpoint: 'analysis_reports', group }
    });

    // Análisis en lote ocasional
    if (Math.random() < 0.3) {
        const batchRequest = {
            urls: Array.from({ length: randomIntBetween(2, 5) }, (_, i) =>
                `https://example${i}.com/page${randomIntBetween(1, 100)}`
            ),
            options: testData.analysis.options
        };

        const batchResponse = http.post(
            `${config.baseUrl}${endpoints.analysis.base}${endpoints.analysis.endpoints.batch}`,
            JSON.stringify(batchRequest),
            { headers: config.headers }
        );
        logResponse(batchResponse, 'Batch Analysis - Analysis User');

        check(batchResponse, validateGatewayResponse(batchResponse, 202), {
            tags: { endpoint: 'analysis_batch', group }
        });
    }
}

function simulateReportUser() {
    const group = 'report_user';
    const testData = generateTestData();

    // Generar reporte complejo
    const reportRequest = {
        ...testData.report,
        template: ['detailed', 'summary', 'executive'][randomIntBetween(0, 2)],
        includeCharts: Math.random() > 0.5,
        includeRawData: Math.random() > 0.7,
        format: ['pdf', 'html', 'json'][randomIntBetween(0, 2)]
    };

    const generateResponse = http.post(
        `${config.baseUrl}${endpoints.reports.base}${endpoints.reports.endpoints.generate}`,
        JSON.stringify(reportRequest),
        { headers: config.headers }
    );
    logResponse(generateResponse, 'Generate Report - Report User');

    const generateCheck = check(generateResponse, validateGatewayResponse(generateResponse, 202), {
        tags: { endpoint: 'reports_generate', group }
    });

    sleep(2);

    // Listar reportes
    const listResponse = http.get(
        `${config.baseUrl}${endpoints.reports.base}${endpoints.reports.endpoints.list}`,
        { headers: config.headers }
    );
    logResponse(listResponse, 'List Reports - Report User');

    check(listResponse, validateGatewayResponse(listResponse, 200), {
        tags: { endpoint: 'reports_list', group }
    });

    if (generateCheck && generateResponse.body) {
        try {
            const reportResult = JSON.parse(generateResponse.body);
            const reportId = reportResult.id;

            // Intentar descargar reporte
            sleep(randomIntBetween(3, 8));

            const downloadResponse = http.get(
                `${config.baseUrl}${endpoints.reports.base}${endpoints.reports.endpoints.download.replace(':id', reportId)}`,
                { headers: config.headers }
            );
            logResponse(downloadResponse, 'Download Report - Report User');

            check(downloadResponse, {
                'download successful or processing': (r) => r.status === 200 || r.status === 202
            }, {
                tags: { endpoint: 'reports_download', group }
            });

            // Exportar ocasionalmente
            if (Math.random() < 0.4) {
                const exportResponse = http.get(
                    `${config.baseUrl}${endpoints.reports.base}${endpoints.reports.endpoints.export.replace(':id', reportId)}`,
                    { headers: config.headers }
                );
                logResponse(exportResponse, 'Export Report - Report User');

                check(exportResponse, {
                    'export successful or processing': (r) => r.status === 200 || r.status === 202
                }, {
                    tags: { endpoint: 'reports_export', group }
                });
            }

        } catch (e) {
            console.error(`Error in report user workflow: ${e.message}`);
        }
    }
}

// Request pesado ocasional para simular operaciones complejas
function simulateHeavyOperation() {
    const group = 'heavy_operation';

    if (Math.random() < 0.05) { // 5% de chance
        const largeRequest = {
            type: 'bulk_analysis',
            data: Array.from({ length: 50 }, () => generateTestData().analysis)
        };

        const heavyResponse = http.post(
            `${config.baseUrl}${endpoints.users.base}${endpoints.users.endpoints.largeRequest}`,
            JSON.stringify(largeRequest),
            {
                headers: config.headers,
                timeout: '30s'
            }
        );
        logResponse(heavyResponse, 'Heavy Operation');

        check(heavyResponse, {
            'heavy operation handled': (r) => r.status < 500
        }, {
            tags: { endpoint: 'heavy_operation', group }
        });
    }
}

export function setup() {
    console.log('🔥 Starting Gateway Load Test...');
    console.log(`🎯 Target: ${config.baseUrl}`);
    console.log('📊 Test will simulate normal production load');

    // Verificar que el Gateway esté disponible
    const healthCheck = http.get(`${config.baseUrl}${endpoints.health}`);
    if (healthCheck.status !== 200) {
        throw new Error(`Gateway not available: ${healthCheck.status}`);
    }

    console.log('✅ Gateway is ready for load testing');
    return { startTime: Date.now() };
}

export function teardown(data) {
    const duration = (Date.now() - data.startTime) / 1000;
    console.log(`✅ Load test completed in ${duration.toFixed(2)} seconds`);
    console.log('📈 Normal production load simulation finished');
}