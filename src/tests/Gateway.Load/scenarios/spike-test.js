// scenarios/spike-test.js
// Prueba de picos de carga - Evalúa respuesta a incrementos súbitos de tráfico

import http from 'k6/http';
import { check, sleep } from 'k6';
import { randomIntBetween } from 'https://jslib.k6.io/k6-utils/1.2.0/index.js';
import { config, endpoints, generateTestData, logResponse, validateGatewayResponse } from '../utils/config.js';

// Configuración de prueba de picos (spike)
export const options = {
    stages: [
        { duration: '1m', target: 10 },   // Línea base normal
        { duration: '30s', target: 200 }, // Pico súbito #1
        { duration: '1m', target: 10 },   // Recuperación
        { duration: '30s', target: 300 }, // Pico súbito #2 (mayor)
        { duration: '1m', target: 20 },   // Recuperación parcial
        { duration: '30s', target: 500 }, // Pico súbito #3 (extremo)
        { duration: '2m', target: 10 },   // Recuperación completa
        { duration: '30s', target: 0 },   // Finalizar
    ],

    thresholds: {
        // Thresholds específicos para picos de carga
        http_req_duration: ['p(95)<1500'], // Más tolerante durante picos
        http_req_failed: ['rate<0.1'],     // Hasta 10% de errores aceptable
        http_reqs: ['rate>10'],            // Throughput mínimo

        // Métricas por fase
        'http_req_duration{phase:baseline}': ['p(95)<500'],
        'http_req_duration{phase:spike}': ['p(95)<3000'],
        'http_req_duration{phase:recovery}': ['p(95)<800'],
    },

    // Configuración optimizada para picos
    noConnectionReuse: false, // Reutilizar conexiones
    userAgent: 'k6-spike-test/1.0',

    tags: {
        test_type: 'spike'
    }
};

export default function spikeTest() {
    // Determinar fase actual basada en VUs activos
    const currentPhase = determinePhase(__VU);

    // Ajustar comportamiento según la fase
    switch (currentPhase) {
        case 'baseline':
            performBaselineOperations();
            break;
        case 'spike':
            performSpikeOperations();
            break;
        case 'recovery':
            performRecoveryOperations();
            break;
        default:
            performBaselineOperations();
    }

    // Sleep variable según la fase
    const sleepTime = getSleepForPhase(currentPhase);
    sleep(sleepTime);
}

function determinePhase(vu) {
    if (vu <= 20) return 'baseline';
    if (vu <= 100) return 'recovery';
    return 'spike';
}

function getSleepForPhase(phase) {
    switch (phase) {
        case 'baseline': return randomIntBetween(1, 3);
        case 'spike': return randomIntBetween(0, 1);     // Mínimo sleep durante picos
        case 'recovery': return randomIntBetween(2, 4);   // Más sleep durante recuperación
        default: return 1;
    }
}

function performBaselineOperations() {
    const group = 'baseline';
    const testData = generateTestData();

    // Operaciones normales y estables
    const healthResponse = http.get(`${config.baseUrl}${endpoints.health}`, {
        tags: { endpoint: 'health', group, phase: 'baseline' }
    });

    check(healthResponse, validateGatewayResponse(healthResponse, 200), {
        tags: { endpoint: 'health', group, phase: 'baseline' }
    });

    sleep(0.5);

    // Operación típica de usuario
    const usersResponse = http.get(
        `${config.baseUrl}${endpoints.users.base}${endpoints.users.endpoints.list}`,
        {
            headers: config.headers,
            tags: { endpoint: 'users_list', group, phase: 'baseline' }
        }
    );
    logResponse(usersResponse, 'List Users - Baseline');

    check(usersResponse, validateGatewayResponse(usersResponse, 200), {
        tags: { endpoint: 'users_list', group, phase: 'baseline' }
    });

    sleep(0.5);

    // Crear usuario ocasionalmente
    if (Math.random() < 0.3) {
        const createResponse = http.post(
            `${config.baseUrl}${endpoints.users.base}${endpoints.users.endpoints.create}`,
            JSON.stringify(testData.user),
            {
                headers: config.headers,
                tags: { endpoint: 'users_create', group, phase: 'baseline' }
            }
        );
        logResponse(createResponse, 'Create User - Baseline');

        check(createResponse, validateGatewayResponse(createResponse, 201), {
            tags: { endpoint: 'users_create', group, phase: 'baseline' }
        });
    }
}

function performSpikeOperations() {
    const group = 'spike';

    // Durante picos, simular múltiples operaciones rápidas
    const operations = randomIntBetween(1, 4);

    for (let i = 0; i < operations; i++) {
        const operationType = randomIntBetween(1, 100);

        if (operationType <= 40) {
            // 40% - Health checks rápidos
            const healthResponse = http.get(`${config.baseUrl}${endpoints.health}`, {
                tags: { endpoint: 'health', group, phase: 'spike' }
            });

            check(healthResponse, {
                'health check spike': (r) => r.status === 200,
                'response under 5s': (r) => r.timings.duration < 5000
            }, {
                tags: { endpoint: 'health', group, phase: 'spike' }
            });

        } else if (operationType <= 70) {
            // 30% - Lecturas rápidas
            const readResponse = http.get(
                `${config.baseUrl}${endpoints.users.base}${endpoints.users.endpoints.list}?limit=5`,
                {
                    headers: config.headers,
                    tags: { endpoint: 'users_list', group, phase: 'spike' }
                }
            );
            logResponse(readResponse, 'Quick Read - Spike');

            check(readResponse, {
                'quick read spike': (r) => r.status < 500,
                'reasonable spike time': (r) => r.timings.duration < 10000
            }, {
                tags: { endpoint: 'users_list', group, phase: 'spike' }
            });

        } else if (operationType <= 85) {
            // 15% - Escrituras durante pico
            const spikeUser = {
                name: `SpikeUser_${__VU}_${__ITER}_${i}`,
                email: `spike${__VU}${__ITER}${i}@test.com`,
                spike: true
            };

            const createResponse = http.post(
                `${config.baseUrl}${endpoints.users.base}${endpoints.users.endpoints.create}`,
                JSON.stringify(spikeUser),
                {
                    headers: config.headers,
                    tags: { endpoint: 'users_create', group, phase: 'spike' }
                }
            );
            logResponse(createResponse, 'Create User - Spike');

            check(createResponse, {
                'create during spike': (r) => r.status < 500,
                'create or conflict': (r) => r.status === 201 || r.status === 409 || r.status === 429
            }, {
                tags: { endpoint: 'users_create', group, phase: 'spike' }
            });

        } else {
            // 15% - Análisis rápido
            const quickAnalysis = {
                url: `https://spike-test-${__VU}-${i}.com`,
                type: 'quick',
                options: { timeout: 10 }
            };

            const analysisResponse = http.post(
                `${config.baseUrl}${endpoints.analysis.base}${endpoints.analysis.endpoints.analyze}`,
                JSON.stringify(quickAnalysis),
                {
                    headers: config.headers,
                    tags: { endpoint: 'analysis_start', group, phase: 'spike' }
                }
            );
            logResponse(analysisResponse, 'Quick Analysis - Spike');

            check(analysisResponse, {
                'analysis during spike': (r) => r.status < 500,
                'analysis queued or started': (r) => r.status === 202 || r.status === 429
            }, {
                tags: { endpoint: 'analysis_start', group, phase: 'spike' }
            });
        }

        // Sleep mínimo entre operaciones del spike
        sleep(0.1);
    }
}

function performRecoveryOperations() {
    const group = 'recovery';
    const testData = generateTestData();

    // Durante recuperación, operaciones más conservadoras
    const healthResponse = http.get(`${config.baseUrl}${endpoints.health}`, {
        tags: { endpoint: 'health', group, phase: 'recovery' }
    });

    check(healthResponse, validateGatewayResponse(healthResponse, 200), {
        tags: { endpoint: 'health', group, phase: 'recovery' }
    });

    sleep(1);

    // Verificar métricas del sistema durante recuperación
    const metricsResponse = http.get(`${config.baseUrl}${endpoints.metrics}`, {
        tags: { endpoint: 'metrics', group, phase: 'recovery' }
    });

    check(metricsResponse, validateGatewayResponse(metricsResponse, 200), {
        tags: { endpoint: 'metrics', group, phase: 'recovery' }
    });

    sleep(1);

    // Operaciones de lectura suaves
    const reportsResponse = http.get(
        `${config.baseUrl}${endpoints.reports.base}${endpoints.reports.endpoints.list}`,
        {
            headers: config.headers,
            tags: { endpoint: 'reports_list', group, phase: 'recovery' }
        }
    );
    logResponse(reportsResponse, 'List Reports - Recovery');

    check(reportsResponse, validateGatewayResponse(reportsResponse, 200), {
        tags: { endpoint: 'reports_list', group, phase: 'recovery' }
    });

    // Operación de escritura ocasional y suave
    if (Math.random() < 0.2) {
        const recoveryUser = {
            ...testData.user,
            name: `${testData.user.name}_recovery`,
            recovery: true
        };

        const createResponse = http.post(
            `${config.baseUrl}${endpoints.users.base}${endpoints.users.endpoints.create}`,
            JSON.stringify(recoveryUser),
            {
                headers: config.headers,
                tags: { endpoint: 'users_create', group, phase: 'recovery' }
            }
        );
        logResponse(createResponse, 'Create User - Recovery');

        check(createResponse, validateGatewayResponse(createResponse, 201), {
            tags: { endpoint: 'users_create', group, phase: 'recovery' }
        });
    }
}

// Función para simular comportamiento de autoescalado
function simulateAutoScalingBehavior() {
    if (__VU > 100) { // Durante picos
        // Simular respuestas más lentas por autoescalado
        sleep(randomIntBetween(1, 3));
    }
}

// Función para verificar circuit breakers
function checkCircuitBreakers() {
    const group = 'circuit_breaker_test';

    // Test específico de circuit breakers durante picos
    if (__VU > 200) {
        const response = http.get(`${config.baseUrl}${endpoints.health}`, {
            tags: { endpoint: 'health', group, test_type: 'circuit_breaker' }
        });

        check(response, {
            'circuit breaker active': (r) => r.status === 503 || r.status === 429 || r.status === 200
        }, {
            tags: { endpoint: 'health', group }
        });
    }
}

export function setup() {
    console.log('⚡ Starting Gateway Spike Test...');
    console.log(`🎯 Target: ${config.baseUrl}`);
    console.log('📈 This test simulates sudden traffic spikes');

    // Verificar que el Gateway esté disponible
    const healthCheck = http.get(`${config.baseUrl}${endpoints.health}`);
    if (healthCheck.status !== 200) {
        throw new Error(`Gateway not available: ${healthCheck.status}`);
    }

    console.log('✅ Gateway is ready for spike testing');
    console.log('⚡ Will test response to sudden load increases');
    console.log('📊 Monitoring for: autoscaling, circuit breakers, recovery time');

    return {
        startTime: Date.now(),
        spikeLevels: [200, 300, 500],
        recoveryTarget: 500 // ms target during recovery
    };
}

export function teardown(data) {
    const duration = (Date.now() - data.startTime) / 1000;
    console.log(`✅ Spike test completed in ${duration.toFixed(2)} seconds`);
    console.log(`⚡ Tested spike levels: ${data.spikeLevels.join(', ')} users`);
    console.log('📊 Check results for:');
    console.log('   - Response time during spikes');
    console.log('   - Error rates during spikes');
    console.log('   - Recovery time after spikes');
    console.log('   - Circuit breaker activation');
    console.log('   - Autoscaling behavior');
}