// scenarios/stress-test.js
// Prueba de estrés - Encuentra el punto de quiebre del sistema

import http from 'k6/http';
import { check, sleep } from 'k6';
import { randomIntBetween } from 'https://jslib.k6.io/k6-utils/1.2.0/index.js';
import { config, endpoints, generateTestData, logResponse, validateGatewayResponse } from '../utils/config.js';

// Configuración de prueba de estrés
export let options = {
    stages: [
        { duration: '2m', target: 50 },   // Ramp-up inicial
        { duration: '2m', target: 100 },  // Carga normal
        { duration: '2m', target: 200 },  // Estrés moderado
        { duration: '3m', target: 300 },  // Estrés alto
        { duration: '2m', target: 400 },  // Estrés extremo
        { duration: '2m', target: 200 },  // Recuperación
        { duration: '3m', target: 0 },    // Ramp-down
    ],

    // Thresholds más permisivos para pruebas de estrés
    thresholds: {
        // Permitimos mayor latencia bajo estrés
        http_req_duration: ['p(95)<2000'],
        http_req_duration_95: ['p(95)<3000'],
        // Toleramos hasta 5% de errores en pruebas de estrés
        http_req_failed: ['rate<0.05'],
        // Throughput mínimo reducido
        http_reqs: ['rate>5'],
        // Métricas específicas por endpoint
        'http_req_duration{endpoint:health}': ['p(95)<500'],
        'http_req_duration{endpoint:users_list}': ['p(95)<1500'],
        'http_req_duration{endpoint:users_create}': ['p(95)<3000'],
    },

    // No reutilizar conexiones para simular carga real
    noConnectionReuse: true,

    tags: {
        test_type: 'stress'
    }
};

export default function () {
    // Distribuir la carga entre diferentes tipos de operaciones
    const operation = randomIntBetween(1, 100);

    if (operation <= 25) {
        // 25% - Operaciones rápidas (health checks, listas)
        performLightOperations();
    } else if (operation <= 50) {
        // 25% - Operaciones de lectura
        performReadOperations();
    } else if (operation <= 75) {
        // 25% - Operaciones de escritura
        performWriteOperations();
    } else {
        // 25% - Operaciones pesadas
        performHeavyOperations();
    }

    // Sleep variable para simular comportamiento real bajo estrés
    sleep(randomIntBetween(0, 2));
}

function performLightOperations() {
    const group = 'light_operations';

    // Health checks múltiples
    const healthResponse = http.get(`${config.baseUrl}${endpoints.health}`, {
        tags: { endpoint: 'health', group }
    });
    logResponse(healthResponse, 'Health Check - Light');

    check(healthResponse, validateGatewayResponse(healthResponse, 200), {
        tags: { endpoint: 'health', group }
    });

    // Ready check
    const readyResponse = http.get(`${config.baseUrl}${endpoints.ready}`, {
        tags: { endpoint: 'ready', group }
    });

    check(readyResponse, validateGatewayResponse(readyResponse, 200), {
        tags: { endpoint: 'ready', group }
    });

    // Métricas del sistema
    const metricsResponse = http.get(`${config.baseUrl}${endpoints.metrics}`, {
        tags: { endpoint: 'metrics', group }
    });

    check(metricsResponse, validateGatewayResponse(metricsResponse, 200), {
        tags: { endpoint: 'metrics', group }
    });
}

function performReadOperations() {
    const group = 'read_operations';

    // Listar usuarios con paginación
    const usersResponse = http.get(
        `${config.baseUrl}${endpoints.users.base}${endpoints.users.endpoints.list}?page=${randomIntBetween(1, 10)}&limit=${randomIntBetween(10, 50)}`,
        {
            headers: config.headers,
            tags: { endpoint: 'users_list', group }
        }
    );
    logResponse(usersResponse, 'List Users - Read');

    check(usersResponse, validateGatewayResponse(usersResponse, 200), {
        tags: { endpoint: 'users_list', group }
    });

    // Listar reportes
    const reportsResponse = http.get(
        `${config.baseUrl}${endpoints.reports.base}${endpoints.reports.endpoints.list}`,
        {
            headers: config.headers,
            tags: { endpoint: 'reports_list', group }
        }
    );
    logResponse(reportsResponse, 'List Reports - Read');

    check(reportsResponse, validateGatewayResponse(reportsResponse, 200), {
        tags: { endpoint: 'reports_list', group }
    });

    // Obtener análisis recientes
    const analysisResponse = http.get(
        `${config.baseUrl}${endpoints.analysis.base}${endpoints.analysis.endpoints.reports}`,
        {
            headers: config.headers,
            tags: { endpoint: 'analysis_reports', group }
        }
    );
    logResponse(analysisResponse, 'Analysis Reports - Read');

    check(analysisResponse, validateGatewayResponse(analysisResponse, 200), {
        tags: { endpoint: 'analysis_reports', group }
    });
}

function performWriteOperations() {
    const group = 'write_operations';
    const testData = generateTestData();

    // Crear múltiples usuarios rápidamente
    for (let i = 0; i < randomIntBetween(1, 3); i++) {
        const userData = {
            ...testData.user,
            name: `${testData.user.name}_stress_${i}`,
            email: `stress_${i}_${testData.user.email}`,
            stressTest: true
        };

        const createResponse = http.post(
            `${config.baseUrl}${endpoints.users.base}${endpoints.users.endpoints.create}`,
            JSON.stringify(userData),
            {
                headers: config.headers,
                tags: { endpoint: 'users_create', group }
            }
        );
        logResponse(createResponse, `Create User ${i} - Write`);

        check(createResponse, {
            'user created or conflict': (r) => r.status === 201 || r.status === 409
        }, {
            tags: { endpoint: 'users_create', group }
        });

        // Sleep mínimo entre creaciones
        sleep(0.1);
    }

    // Iniciar análisis múltiples
    for (let i = 0; i < randomIntBetween(1, 2); i++) {
        const analysisData = {
            ...testData.analysis,
            url: `https://stress-test-${i}.com/page${randomIntBetween(1, 1000)}`,
            priority: 'high'
        };

        const analysisResponse = http.post(
            `${config.baseUrl}${endpoints.analysis.base}${endpoints.analysis.endpoints.analyze}`,
            JSON.stringify(analysisData),
            {
                headers: config.headers,
                tags: { endpoint: 'analysis_start', group }
            }
        );
        logResponse(analysisResponse, `Start Analysis ${i} - Write`);

        check(analysisResponse, {
            'analysis started or queued': (r) => r.status === 202 || r.status === 429
        }, {
            tags: { endpoint: 'analysis_start', group }
        });

        sleep(0.1);
    }
}

function performHeavyOperations() {
    const group = 'heavy_operations';
    const testData = generateTestData();

    // Análisis en lote grande
    const batchSize = randomIntBetween(5, 15);
    const batchData = {
        urls: Array.from({ length: batchSize }, (_, i) =>
            `https://heavy-test-${__VU}-${i}.com/stress`
        ),
        options: {
            ...testData.analysis.options,
            priority: 'low', // Baja prioridad para no sobrecargar
            includeScreenshots: false // Reducir carga
        }
    };

    const batchResponse = http.post(
        `${config.baseUrl}${endpoints.analysis.base}${endpoints.analysis.endpoints.batch}`,
        JSON.stringify(batchData),
        {
            headers: config.headers,
            timeout: '30s',
            tags: { endpoint: 'analysis_batch', group }
        }
    );
    logResponse(batchResponse, 'Batch Analysis - Heavy');

    check(batchResponse, {
        'batch handled': (r) => r.status < 500
    }, {
        tags: { endpoint: 'analysis_batch', group }
    });

    // Reporte complejo
    const complexReport = {
        ...testData.report,
        format: 'pdf',
        includeCharts: true,
        includeRawData: true,
        includeImages: false, // Reducir tamaño
        priority: 'low',
        sections: ['summary', 'analysis'] // Reducir secciones
    };

    const reportResponse = http.post(
        `${config.baseUrl}${endpoints.reports.base}${endpoints.reports.endpoints.generate}`,
        JSON.stringify(complexReport),
        {
            headers: config.headers,
            timeout: '30s',
            tags: { endpoint: 'reports_generate', group }
        }
    );
    logResponse(reportResponse, 'Complex Report - Heavy');

    check(reportResponse, {
        'report queued or processed': (r) => r.status === 202 || r.status === 200 || r.status === 429
    }, {
        tags: { endpoint: 'reports_generate', group }
    });

    // Operación sintética muy pesada
    if (Math.random() < 0.1) { // Solo 10% del tiempo
        const largePayload = {
            operation: 'stress_test',
            data: Array.from({ length: 100 }, () => ({
                id: randomIntBetween(1, 10000),
                data: `large_data_${Math.random().toString(36).substring(7)}`
            }))
        };

        const heavyResponse = http.post(
            `${config.baseUrl}${endpoints.users.base}${endpoints.users.endpoints.largeRequest}`,
            JSON.stringify(largePayload),
            {
                headers: config.headers,
                timeout: '60s',
                tags: { endpoint: 'large_request', group }
            }
        );
        logResponse(heavyResponse, 'Large Request - Heavy');

        check(heavyResponse, {
            'large request handled': (r) => r.status < 500
        }, {
            tags: { endpoint: 'large_request', group }
        });
    }
}

// Función para simular usuarios problemáticos
function simulateProblematicUser() {
    const group = 'problematic_user';

    // Múltiples requests simultáneos del mismo usuario
    const rapidRequests = [
        `${config.baseUrl}${endpoints.health}`,
        `${config.baseUrl}${endpoints.ready}`,
        `${config.baseUrl}${endpoints.users.base}${endpoints.users.endpoints.list}`,
        `${config.baseUrl}${endpoints.reports.base}${endpoints.reports.endpoints.list}`
    ];

    rapidRequests.forEach((url, index) => {
        const response = http.get(url, {
            headers: config.headers,
            tags: { endpoint: `rapid_${index}`, group }
        });

        check(response, {
            'rapid request handled': (r) => r.status < 500
        }, {
            tags: { endpoint: `rapid_${index}`, group }
        });
    });
}

export function setup() {
    console.log('💪 Starting Gateway Stress Test...');
    console.log(`🎯 Target: ${config.baseUrl}`);
    console.log('⚠️  This test will push the system to its limits');

    // Verificar que el Gateway esté disponible
    const healthCheck = http.get(`${config.baseUrl}${endpoints.health}`);
    if (healthCheck.status !== 200) {
        throw new Error(`Gateway not available: ${healthCheck.status}`);
    }

    console.log('✅ Gateway is ready for stress testing');
    console.log('📊 Monitoring for degradation points and failure modes');

    return {
        startTime: Date.now(),
        maxUsers: 400,
        degradationThreshold: 2000 // 2s response time
    };
}

export function teardown(data) {
    const duration = (Date.now() - data.startTime) / 1000;
    console.log(`✅ Stress test completed in ${duration.toFixed(2)} seconds`);
    console.log(`💪 Maximum concurrent users reached: ${data.maxUsers}`);
    console.log('📊 Check results for system breaking points and recovery behavior');
    console.log('⚠️  Review logs for errors and performance degradation patterns');
}