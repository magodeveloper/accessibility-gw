// scenarios/smoke-test.js
// Prueba de humo - Verificación básica que todos los endpoints funcionan

import http from 'k6/http';
import { check, sleep } from 'k6';
import { config, endpoints, generateTestData, logResponse, validateGatewayResponse } from '../utils/config.js';

// Configuración del test de humo
export const options = {
    stages: [
        { duration: '30s', target: 1 }, // 1 usuario por 30 segundos
    ],
    thresholds: {
        // Para smoke test, toleramos más latencia pero 0 errores
        http_req_duration: ['p(95)<1000'],
        http_req_failed: ['rate<0.01'],
    },
    tags: {
        test_type: 'smoke'
    }
};

export default function smokeTest() {
    const testData = generateTestData();

    // 1. Health Check
    console.log('🏥 Testing health endpoints...');
    testHealthEndpoints();

    sleep(1);

    // 2. Users Service através del Gateway
    console.log('👥 Testing users service...');
    testUsersService(testData.user);

    sleep(1);

    // 3. Analysis Service através del Gateway
    console.log('🔍 Testing analysis service...');
    testAnalysisService(testData.analysis);

    sleep(1);

    // 4. Reports Service através del Gateway
    console.log('📊 Testing reports service...');
    testReportsService(testData.report);

    sleep(2);
}

function testHealthEndpoints() {
    const healthResponse = http.get(`${config.baseUrl}${endpoints.health}`);
    logResponse(healthResponse, 'Health Check');

    check(healthResponse, validateGatewayResponse(healthResponse, 200), {
        endpoint: 'health'
    });

    const readyResponse = http.get(`${config.baseUrl}${endpoints.ready}`);
    logResponse(readyResponse, 'Ready Check');

    check(readyResponse, validateGatewayResponse(readyResponse, 200), {
        endpoint: 'ready'
    });

    const metricsResponse = http.get(`${config.baseUrl}${endpoints.metrics}`);
    logResponse(metricsResponse, 'Metrics Check');

    check(metricsResponse, validateGatewayResponse(metricsResponse, 200), {
        endpoint: 'metrics'
    });
}

function testUsersService(userData) {
    const usersBase = `${config.baseUrl}${endpoints.users.base}`;

    // Crear usuario
    const createResponse = http.post(
        `${usersBase}${endpoints.users.endpoints.create}`,
        JSON.stringify(userData),
        { headers: config.headers }
    );
    logResponse(createResponse, 'Create User');

    const createCheck = check(createResponse, validateGatewayResponse(createResponse, 201), {
        endpoint: 'users_create'
    });

    if (createCheck && createResponse.body) {
        try {
            const createdUser = JSON.parse(createResponse.body);
            const userId = createdUser.id;

            // Obtener usuario
            const getResponse = http.get(
                `${usersBase}${endpoints.users.endpoints.getById.replace(':id', userId)}`,
                { headers: config.headers }
            );
            logResponse(getResponse, 'Get User');

            check(getResponse, validateGatewayResponse(getResponse, 200), {
                endpoint: 'users_get'
            });

            // Actualizar usuario
            const updatedUser = { ...userData, name: `${userData.name}_updated` };
            const updateResponse = http.put(
                `${usersBase}${endpoints.users.endpoints.update.replace(':id', userId)}`,
                JSON.stringify(updatedUser),
                { headers: config.headers }
            );
            logResponse(updateResponse, 'Update User');

            check(updateResponse, validateGatewayResponse(updateResponse, 200), {
                endpoint: 'users_update'
            });

            // Obtener perfil
            const profileResponse = http.get(
                `${usersBase}${endpoints.users.endpoints.profile.replace(':id', userId)}`,
                { headers: config.headers }
            );
            logResponse(profileResponse, 'Get User Profile');

            check(profileResponse, validateGatewayResponse(profileResponse, 200), {
                endpoint: 'users_profile'
            });

            // Actualizar preferencias
            const preferencesResponse = http.put(
                `${usersBase}${endpoints.users.endpoints.preferences.replace(':id', userId)}`,
                JSON.stringify(userData.preferences),
                { headers: config.headers }
            );
            logResponse(preferencesResponse, 'Update Preferences');

            check(preferencesResponse, validateGatewayResponse(preferencesResponse, 200), {
                endpoint: 'users_preferences'
            });

            // Eliminar usuario (cleanup)
            const deleteResponse = http.del(
                `${usersBase}${endpoints.users.endpoints.delete.replace(':id', userId)}`,
                null,
                { headers: config.headers }
            );
            logResponse(deleteResponse, 'Delete User');

            check(deleteResponse, validateGatewayResponse(deleteResponse, 204), {
                endpoint: 'users_delete'
            });

        } catch (e) {
            console.error(`Error in users workflow: ${e.message}`);
        }
    }

    // Listar usuarios
    const listResponse = http.get(
        `${usersBase}${endpoints.users.endpoints.list}`,
        { headers: config.headers }
    );
    logResponse(listResponse, 'List Users');

    check(listResponse, validateGatewayResponse(listResponse, 200), {
        endpoint: 'users_list'
    });
}

function testAnalysisService(analysisData) {
    const analysisBase = `${config.baseUrl}${endpoints.analysis.base}`;

    // Iniciar análisis
    const analyzeResponse = http.post(
        `${analysisBase}${endpoints.analysis.endpoints.analyze}`,
        JSON.stringify(analysisData),
        { headers: config.headers }
    );
    logResponse(analyzeResponse, 'Start Analysis');

    const analyzeCheck = check(analyzeResponse, validateGatewayResponse(analyzeResponse, 202), {
        endpoint: 'analysis_start'
    });

    if (analyzeCheck && analyzeResponse.body) {
        try {
            const analysisResult = JSON.parse(analyzeResponse.body);
            const analysisId = analysisResult.id;

            // Verificar estado del análisis
            const statusResponse = http.get(
                `${analysisBase}${endpoints.analysis.endpoints.status.replace(':id', analysisId)}`,
                { headers: config.headers }
            );
            logResponse(statusResponse, 'Check Analysis Status');

            check(statusResponse, validateGatewayResponse(statusResponse, 200), {
                endpoint: 'analysis_status'
            });

        } catch (e) {
            console.error(`Error in analysis workflow: ${e.message}`);
        }
    }

    // Obtener reportes de análisis
    const reportsResponse = http.get(
        `${analysisBase}${endpoints.analysis.endpoints.reports}`,
        { headers: config.headers }
    );
    logResponse(reportsResponse, 'Get Analysis Reports');

    check(reportsResponse, validateGatewayResponse(reportsResponse, 200), {
        endpoint: 'analysis_reports'
    });

    // Análisis en lote
    const batchData = {
        urls: [analysisData.url, `${analysisData.url}/page2`],
        options: analysisData.options
    };

    const batchResponse = http.post(
        `${analysisBase}${endpoints.analysis.endpoints.batch}`,
        JSON.stringify(batchData),
        { headers: config.headers }
    );
    logResponse(batchResponse, 'Batch Analysis');

    check(batchResponse, validateGatewayResponse(batchResponse, 202), {
        endpoint: 'analysis_batch'
    });
}

function testReportsService(reportData) {
    const reportsBase = `${config.baseUrl}${endpoints.reports.base}`;

    // Generar reporte
    const generateResponse = http.post(
        `${reportsBase}${endpoints.reports.endpoints.generate}`,
        JSON.stringify(reportData),
        { headers: config.headers }
    );
    logResponse(generateResponse, 'Generate Report');

    const generateCheck = check(generateResponse, validateGatewayResponse(generateResponse, 202), {
        endpoint: 'reports_generate'
    });

    // Listar reportes
    const listResponse = http.get(
        `${reportsBase}${endpoints.reports.endpoints.list}`,
        { headers: config.headers }
    );
    logResponse(listResponse, 'List Reports');

    check(listResponse, validateGatewayResponse(listResponse, 200), {
        endpoint: 'reports_list'
    });

    if (generateCheck && generateResponse.body) {
        try {
            const reportResult = JSON.parse(generateResponse.body);
            const reportId = reportResult.id;

            // Intentar descargar reporte (puede estar en procesamiento)
            const downloadResponse = http.get(
                `${reportsBase}${endpoints.reports.endpoints.download.replace(':id', reportId)}`,
                { headers: config.headers }
            );
            logResponse(downloadResponse, 'Download Report');

            // Para smoke test, aceptamos 200 (listo) o 202 (procesando)
            check(downloadResponse, {
                'download status acceptable': (r) => r.status === 200 || r.status === 202
            }, {
                endpoint: 'reports_download'
            });

            // Intentar exportar reporte
            const exportResponse = http.get(
                `${reportsBase}${endpoints.reports.endpoints.export.replace(':id', reportId)}`,
                { headers: config.headers }
            );
            logResponse(exportResponse, 'Export Report');

            check(exportResponse, {
                'export status acceptable': (r) => r.status === 200 || r.status === 202
            }, {
                endpoint: 'reports_export'
            });

        } catch (e) {
            console.error(`Error in reports workflow: ${e.message}`);
        }
    }
}

export function setup() {
    console.log('🔥 Starting Gateway Smoke Test...');
    console.log(`🎯 Target: ${config.baseUrl}`);

    // Verificar que el Gateway esté disponible
    const healthCheck = http.get(`${config.baseUrl}${endpoints.health}`);
    if (healthCheck.status !== 200) {
        throw new Error(`Gateway not available: ${healthCheck.status}`);
    }

    console.log('✅ Gateway is available and ready for testing');
    return { startTime: Date.now() };
}

export function teardown(data) {
    const duration = (Date.now() - data.startTime) / 1000;
    console.log(`✅ Smoke test completed in ${duration.toFixed(2)} seconds`);
    console.log('🎉 All basic functionality verified!');
}