// scenarios/concurrent-users-50.js
// Escenario de prueba de carga para 50 usuarios concurrentes

import http from 'k6/http';
import { check, sleep } from 'k6';
import { config, endpoints, generateTestData, validateGatewayResponse, getConfigForUsers } from '../utils/config.js';
import { recordRequestMetrics, updateActiveUsers, logMetrics, getThresholdsForLevel } from '../utils/metrics.js';

const userLevel = 'medium';
const userCount = 50;

// Configuración de la prueba
export let options = {
    stages: [
        { duration: '1m', target: 15 },       // Ramp-up inicial
        { duration: '1m', target: 30 },       // Incremento gradual
        { duration: '1m', target: userCount }, // Alcanzar 50 usuarios
        { duration: '5m', target: userCount }, // Mantener 50 usuarios
        { duration: '1m', target: 25 },       // Ramp-down gradual
        { duration: '30s', target: 0 }        // Finalizar
    ],

    thresholds: getThresholdsForLevel(userLevel),

    tags: {
        test_type: 'concurrent_users',
        user_count: userCount.toString(),
        scenario: 'medium_load'
    }
};

// Función de setup
export function setup() {
    console.log(`🚀 Iniciando prueba de carga MEDIA con ${userCount} usuarios concurrentes`);

    // Verificar servicios del Gateway
    const services = ['health', 'ready'];
    for (const service of services) {
        const response = http.get(`${config.baseUrl}${endpoints[service]}`);
        if (response.status !== 200) {
            throw new Error(`Servicio ${service} no disponible: ${response.status}`);
        }
    }

    console.log('✅ Todos los servicios están disponibles');
    return { startTime: Date.now() };
}

// Función principal con patrones de carga más realistas
export default function (data) {
    updateActiveUsers(__VU);

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

    logMetrics(__ITER);

    // Sleep variable para simular comportamiento real
    sleep(Math.random() * 3 + 0.5); // 0.5-3.5 segundos
}

function selectScenario() {
    const rand = Math.random();

    if (rand < 0.05) return 'health_checks';      // 5%
    else if (rand < 0.40) return 'browsing';      // 35%
    else if (rand < 0.65) return 'intensive_analysis'; // 25%
    else if (rand < 0.85) return 'report_generation';  // 20%
    else return 'user_management';                 // 15%
}

function executeBrowsingPattern(testData) {
    const group = 'browsing_pattern';

    // Simular navegación típica: listar usuarios, ver detalles
    let response = http.get(
        `${config.baseUrl}${endpoints.users.base}${endpoints.users.endpoints.list}`,
        {
            headers: config.headers,
            tags: { endpoint: 'users_list', group }
        }
    );

    check(response, validateGatewayResponse(response, 200), { group });
    recordRequestMetrics(response, 'users');

    sleep(1);

    // Ver reportes disponibles
    response = http.get(
        `${config.baseUrl}${endpoints.reports.base}${endpoints.reports.endpoints.list}`,
        {
            headers: config.headers,
            tags: { endpoint: 'reports_list', group }
        }
    );

    check(response, validateGatewayResponse(response, 200), { group });
    recordRequestMetrics(response, 'reports');

    sleep(0.5);

    // Verificar estado de análisis en curso
    response = http.get(
        `${config.baseUrl}${endpoints.analysis.base}${endpoints.analysis.endpoints.analyze}`,
        {
            headers: config.headers,
            tags: { endpoint: 'analysis_list', group }
        }
    );

    check(response, validateGatewayResponse(response, 200), { group });
    recordRequestMetrics(response, 'analysis');
}

function executeIntensiveAnalysis(testData) {
    const group = 'intensive_analysis';

    // Múltiples análisis concurrentes
    const analysisRequests = [
        { ...testData.analysis, type: 'accessibility' },
        { ...testData.analysis, type: 'performance' },
        { ...testData.analysis, type: 'seo' }
    ];

    const analysisIds = [];

    for (const analysisRequest of analysisRequests) {
        const response = http.post(
            `${config.baseUrl}${endpoints.analysis.base}${endpoints.analysis.endpoints.analyze}`,
            JSON.stringify(analysisRequest),
            {
                headers: config.headers,
                tags: { endpoint: 'analysis_start', group, analysis_type: analysisRequest.type }
            }
        );

        const analyzeCheck = check(response, validateGatewayResponse(response, 202), { group });
        recordRequestMetrics(response, 'analysis');

        if (analyzeCheck && response.body) {
            try {
                const result = JSON.parse(response.body);
                analysisIds.push(result.id);
            } catch (e) {
                console.error(`Error parsing analysis response: ${e.message}`);
            }
        }

        sleep(0.3); // Pausa breve entre requests
    }

    // Verificar estado de los análisis
    sleep(2);

    for (const analysisId of analysisIds) {
        const response = http.get(
            `${config.baseUrl}${endpoints.analysis.base}${endpoints.analysis.endpoints.status.replace(':id', analysisId)}`,
            {
                headers: config.headers,
                tags: { endpoint: 'analysis_status', group }
            }
        );

        check(response, validateGatewayResponse(response, 200), { group });
        recordRequestMetrics(response, 'analysis');

        sleep(0.2);
    }
}

function executeReportGeneration(testData) {
    const group = 'report_generation';

    // Generar reporte complejo
    const complexReport = {
        ...testData.report,
        includeCharts: true,
        includeRawData: true,
        format: 'pdf',
        sections: ['executive_summary', 'detailed_analysis', 'recommendations', 'appendix']
    };

    let response = http.post(
        `${config.baseUrl}${endpoints.reports.base}${endpoints.reports.endpoints.generate}`,
        JSON.stringify(complexReport),
        {
            headers: config.headers,
            tags: { endpoint: 'reports_generate_complex', group }
        }
    );

    const generateCheck = check(response, validateGatewayResponse(response, 202), { group });
    recordRequestMetrics(response, 'reports');

    if (generateCheck && response.body) {
        try {
            const reportResult = JSON.parse(response.body);
            const reportId = reportResult.id;

            sleep(3); // Tiempo para procesamiento

            // Intentar descargar reporte
            response = http.get(
                `${config.baseUrl}${endpoints.reports.base}${endpoints.reports.endpoints.download.replace(':id', reportId)}`,
                {
                    headers: config.headers,
                    tags: { endpoint: 'reports_download', group }
                }
            );

            check(response, {
                'report ready or processing': (r) => r.status === 200 || r.status === 202
            }, { group });
            recordRequestMetrics(response, 'reports');

        } catch (e) {
            console.error(`Error parsing report response: ${e.message}`);
        }
    }
}

function executeUserManagement(testData) {
    const group = 'user_management';

    // Operaciones CRUD completas de usuarios
    let response = http.post(
        `${config.baseUrl}${endpoints.users.base}${endpoints.users.endpoints.create}`,
        JSON.stringify(testData.user),
        {
            headers: config.headers,
            tags: { endpoint: 'users_create', group }
        }
    );

    const createCheck = check(response, validateGatewayResponse(response, 201), { group });
    recordRequestMetrics(response, 'users');

    if (createCheck && response.body) {
        try {
            const user = JSON.parse(response.body);
            const userId = user.id;

            sleep(0.5);

            // Actualizar usuario
            const updatedUser = {
                ...testData.user,
                name: `${testData.user.name}_updated`,
                lastModified: new Date().toISOString()
            };

            response = http.put(
                `${config.baseUrl}${endpoints.users.base}${endpoints.users.endpoints.update.replace(':id', userId)}`,
                JSON.stringify(updatedUser),
                {
                    headers: config.headers,
                    tags: { endpoint: 'users_update', group }
                }
            );

            check(response, validateGatewayResponse(response, 200), { group });
            recordRequestMetrics(response, 'users');

            sleep(0.5);

            // Obtener perfil completo
            response = http.get(
                `${config.baseUrl}${endpoints.users.base}${endpoints.users.endpoints.profile.replace(':id', userId)}`,
                {
                    headers: config.headers,
                    tags: { endpoint: 'users_profile', group }
                }
            );

            check(response, validateGatewayResponse(response, 200), { group });
            recordRequestMetrics(response, 'users');

            sleep(0.5);

            // Eliminar usuario (cleanup)
            response = http.del(
                `${config.baseUrl}${endpoints.users.base}${endpoints.users.endpoints.delete.replace(':id', userId)}`,
                null,
                {
                    headers: config.headers,
                    tags: { endpoint: 'users_delete', group }
                }
            );

            check(response, validateGatewayResponse(response, 204), { group });
            recordRequestMetrics(response, 'users');

        } catch (e) {
            console.error(`Error in user management: ${e.message}`);
        }
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
    console.log(`✅ Prueba de carga MEDIA completada en ${duration.toFixed(2)} segundos`);
    console.log(`📊 Usuarios concurrentes máximos: ${userCount}`);
    console.log(`🎯 Carga: Media - Patrones de uso realistas`);
}