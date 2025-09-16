// scenarios/concurrent-users-500.js
// Escenario de prueba de carga EXTREMA para 500 usuarios concurrentes

import http from 'k6/http';
import { check, sleep } from 'k6';
import { config, endpoints, generateTestData, validateGatewayResponse, getConfigForUsers } from '../utils/config.js';
import { recordRequestMetrics, updateActiveUsers, logMetrics, getThresholdsForLevel } from '../utils/metrics.js';

const userLevel = 'extreme';
const userCount = 500;

// Configuración de la prueba con ramp-up muy gradual para carga extrema
export let options = {
    stages: [
        { duration: '3m', target: 50 },       // Ramp-up muy suave
        { duration: '3m', target: 100 },      // Incremento gradual
        { duration: '3m', target: 200 },      // Carga media-alta
        { duration: '3m', target: 300 },      // Carga alta
        { duration: '3m', target: 400 },      // Carga muy alta
        { duration: '2m', target: userCount }, // Alcanzar 500 usuarios
        { duration: '15m', target: userCount }, // Mantener 500 usuarios
        { duration: '3m', target: 300 },      // Ramp-down gradual
        { duration: '2m', target: 100 },      // Continuar ramp-down
        { duration: '1m', target: 0 }         // Finalizar
    ],

    thresholds: getThresholdsForLevel(userLevel),

    tags: {
        test_type: 'concurrent_users',
        user_count: userCount.toString(),
        scenario: 'extreme_load'
    },

    // Configuraciones adicionales para carga extrema
    discardResponseBodies: true, // Descartar bodies para ahorrar memoria
    noConnectionReuse: false,    // Reutilizar conexiones HTTP
    userAgent: 'Gateway-LoadTest-Extreme/1.0'
};

// Variables compartidas para coordinación y limitación
let requestCounter = 0;
let errorCounter = 0;
let lastErrorRate = 0;

export function setup() {
    console.log(`🚀 Iniciando prueba de carga EXTREMA con ${userCount} usuarios concurrentes`);
    console.log(`⚠️  ADVERTENCIA: Esta es una prueba de stress máximo`);

    // Verificación exhaustiva del sistema
    const systemChecks = [
        { name: 'Gateway Health', endpoint: endpoints.health, expectedStatus: 200 },
        { name: 'Gateway Ready', endpoint: endpoints.ready, expectedStatus: 200 },
        { name: 'Gateway Metrics', endpoint: endpoints.metrics, expectedStatus: 200 }
    ];

    for (const check of systemChecks) {
        const response = http.get(`${config.baseUrl}${check.endpoint}`, {
            timeout: '10s' // Timeout más largo para verificaciones
        });

        if (response.status !== check.expectedStatus) {
            throw new Error(`❌ ${check.name} failed: ${response.status}`);
        }
        console.log(`✅ ${check.name}: OK`);
    }

    // Test de capacidad básica
    console.log('🔄 Ejecutando test de capacidad básica...');
    const capacityTest = executeCapacityTest();
    if (!capacityTest.passed) {
        throw new Error(`❌ Sistema no preparado para carga extrema: ${capacityTest.reason}`);
    }

    console.log('✅ Sistema preparado para carga extrema');
    console.log(`📊 Baseline response time: ${capacityTest.baseline}ms`);

    return {
        startTime: Date.now(),
        baseline: capacityTest.baseline,
        maxErrorRate: 0.05,        // 5% error rate máximo
        circuitBreakerThreshold: 0.1 // 10% error rate para circuit breaker
    };
}

function executeCapacityTest() {
    const testRequests = 10;
    let totalTime = 0;
    let errors = 0;

    for (let i = 0; i < testRequests; i++) {
        const response = http.get(`${config.baseUrl}${endpoints.health}`);
        totalTime += response.timings.duration;
        if (response.status !== 200) errors++;
        sleep(0.1);
    }

    const avgTime = totalTime / testRequests;
    const errorRate = errors / testRequests;

    return {
        passed: avgTime < 1000 && errorRate < 0.1, // <1s y <10% errores
        baseline: avgTime,
        errorRate: errorRate,
        reason: avgTime >= 1000 ? 'Response time too high' : 'Error rate too high'
    };
}

export default function (data) {
    updateActiveUsers(__VU);
    requestCounter++;

    // Circuit breaker simple basado en error rate
    if (shouldSkipIteration(data)) {
        sleep(5); // Backoff cuando hay muchos errores
        return;
    }

    const testData = generateTestData();
    const scenario = selectOptimizedScenario(__VU, __ITER);

    try {
        switch (scenario) {
            case 'minimal_health':
                executeMinimalHealth();
                break;
            case 'read_heavy':
                executeReadHeavyPattern(testData);
                break;
            case 'write_optimized':
                executeWriteOptimizedPattern(testData);
                break;
            case 'batch_efficient':
                executeBatchEfficientPattern(testData);
                break;
            case 'circuit_test':
                executeCircuitTest(testData);
                break;
            default:
                executeMinimalHealth();
        }
    } catch (error) {
        errorCounter++;
        console.error(`❌ VU ${__VU}, iteration ${__ITER}: ${error.message}`);
    }

    // Log menos frecuente para reducir overhead
    if (__ITER % 500 === 0) {
        logMetrics(__ITER);
        logSystemHealth();
    }

    // Sleep optimizado para carga extrema
    const sleepTime = calculateOptimalSleep(__VU, requestCounter, errorCounter);
    sleep(sleepTime);
}

function shouldSkipIteration(data) {
    const currentErrorRate = errorCounter / Math.max(requestCounter, 1);
    lastErrorRate = currentErrorRate;

    return currentErrorRate > data.circuitBreakerThreshold;
}

function selectOptimizedScenario(vu, iteration) {
    // Distribución optimizada para minimizar contención
    const vuGroup = vu % 20;

    if (vuGroup < 8) return 'minimal_health';        // 40% - Operaciones ligeras
    else if (vuGroup < 14) return 'read_heavy';      // 30% - Lecturas optimizadas
    else if (vuGroup < 17) return 'write_optimized'; // 15% - Escrituras eficientes
    else if (vuGroup < 19) return 'batch_efficient'; // 10% - Operaciones en lote
    else return 'circuit_test';                      // 5% - Test de límites
}

function executeMinimalHealth() {
    const group = 'minimal_health';

    const response = http.get(`${config.baseUrl}${endpoints.health}`, {
        headers: { 'Accept': 'application/json' },
        tags: { endpoint: 'health', group, load_level: 'extreme' }
    });

    const success = check(response, {
        'health check ok': (r) => r.status === 200,
        'response under 3s': (r) => r.timings.duration < 3000
    }, { group });

    if (!success) errorCounter++;
    recordRequestMetrics(response, 'gateway');
}

function executeReadHeavyPattern(testData) {
    const group = 'read_heavy';

    // Solo operaciones de lectura para minimizar carga en BD
    const readEndpoints = [
        `${endpoints.users.base}${endpoints.users.endpoints.list}?limit=10`,
        `${endpoints.reports.base}${endpoints.reports.endpoints.list}?limit=5`,
        `${endpoints.analysis.base}/summary`
    ];

    const endpoint = readEndpoints[__VU % readEndpoints.length];

    const response = http.get(`${config.baseUrl}${endpoint}`, {
        headers: {
            ...config.headers,
            'Cache-Control': 'max-age=60', // Aprovechar cache
            'Accept-Encoding': 'gzip, deflate' // Compresión
        },
        tags: {
            endpoint: endpoint.split('/').pop(),
            group,
            operation: 'read'
        }
    });

    const success = check(response, {
        'read successful': (r) => r.status < 400,
        'reasonable time': (r) => r.timings.duration < 5000
    }, { group });

    if (!success) errorCounter++;
    recordRequestMetrics(response, 'gateway');
}

function executeWriteOptimizedPattern(testData) {
    const group = 'write_optimized';

    // Escrituras mínimas y eficientes
    const minimalUser = {
        name: `User_${__VU}_${__ITER}`,
        email: `user${__VU}${__ITER}@test.com`
    };

    const response = http.post(
        `${config.baseUrl}${endpoints.users.base}${endpoints.users.endpoints.create}`,
        JSON.stringify(minimalUser),
        {
            headers: config.headers,
            tags: { endpoint: 'users_create_minimal', group, operation: 'write' }
        }
    );

    const success = check(response, {
        'create ok or conflict': (r) => r.status === 201 || r.status === 409,
        'write under 5s': (r) => r.timings.duration < 5000
    }, { group });

    if (!success) errorCounter++;
    recordRequestMetrics(response, 'users');
}

function executeBatchEfficientPattern(testData) {
    const group = 'batch_efficient';

    // Operaciones en lote pequeñas para eficiencia
    const batchSize = 3; // Lotes pequeños para carga extrema
    const batchData = {
        requests: Array.from({ length: batchSize }, (_, i) => ({
            id: `${__VU}_${__ITER}_${i}`,
            type: 'quick_analysis',
            url: `https://example${i}.com`
        }))
    };

    const response = http.post(
        `${config.baseUrl}${endpoints.analysis.base}/batch-quick`,
        JSON.stringify(batchData),
        {
            headers: config.headers,
            tags: { endpoint: 'analysis_batch_quick', group, batch_size: batchSize.toString() }
        }
    );

    const success = check(response, {
        'batch accepted': (r) => r.status === 202 || r.status === 200,
        'batch under 3s': (r) => r.timings.duration < 3000
    }, { group });

    if (!success) errorCounter++;
    recordRequestMetrics(response, 'analysis');
}

function executeCircuitTest(testData) {
    const group = 'circuit_test';

    // Test de límites para verificar circuit breakers
    const response = http.get(`${config.baseUrl}${endpoints.users.base}/stress-test`, {
        headers: config.headers,
        tags: { endpoint: 'stress_test', group, test_type: 'circuit' },
        timeout: '2s' // Timeout agresivo
    });

    // Más tolerante en circuit test
    const success = check(response, {
        'circuit response': (r) => r.status !== 0, // Cualquier respuesta es válida
        'not timeout': (r) => r.timings.duration < 2000
    }, { group });

    if (!success) errorCounter++;
    recordRequestMetrics(response, 'gateway');
}

function calculateOptimalSleep(vu, requests, errors) {
    const errorRate = errors / Math.max(requests, 1);
    const baseSlleep = 0.1; // Sleep mínimo para carga extrema

    // Incrementar sleep si hay muchos errores
    if (errorRate > 0.05) return baseSlleep * 5;  // 500ms
    if (errorRate > 0.02) return baseSlleep * 3;  // 300ms
    if (errorRate > 0.01) return baseSlleep * 2;  // 200ms

    // Distribución temporal basada en VU
    return baseSlleep + (vu % 10) * 0.05; // 100-500ms
}

function logSystemHealth() {
    const errorRate = errorCounter / Math.max(requestCounter, 1);
    console.log(`🔍 System Health - Requests: ${requestCounter}, Errors: ${errorCounter}, Error Rate: ${(errorRate * 100).toFixed(2)}%`);

    if (errorRate > 0.05) {
        console.log(`⚠️  HIGH ERROR RATE: ${(errorRate * 100).toFixed(2)}%`);
    }
}

export function teardown(data) {
    const duration = (Date.now() - data.startTime) / 1000;
    const finalErrorRate = errorCounter / Math.max(requestCounter, 1);

    console.log(`✅ Prueba de carga EXTREMA completada en ${duration.toFixed(2)} segundos`);
    console.log(`📊 Usuarios concurrentes máximos: ${userCount}`);
    console.log(`⚡ Total requests: ${requestCounter}`);
    console.log(`❌ Total errors: ${errorCounter}`);
    console.log(`📈 Error rate final: ${(finalErrorRate * 100).toFixed(2)}%`);
    console.log(`⏱️  Baseline response time: ${data.baseline.toFixed(2)}ms`);

    // Evaluación de resultados
    if (finalErrorRate <= data.maxErrorRate) {
        console.log(`🎉 PASSED: Error rate dentro del límite aceptable (${data.maxErrorRate * 100}%)`);
    } else {
        console.log(`❌ FAILED: Error rate excede el límite (${data.maxErrorRate * 100}%)`);
    }

    console.log(`🏁 EXTREME LOAD TEST COMPLETED`);
}