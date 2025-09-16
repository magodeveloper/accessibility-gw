// scenarios/endurance-test.js
// Prueba de resistencia - Evalúa estabilidad durante períodos prolongados

import http from 'k6/http';
import { check, sleep } from 'k6';
import { randomIntBetween } from 'https://jslib.k6.io/k6-utils/1.2.0/index.js';
import { config, endpoints, generateTestData, logResponse, validateGatewayResponse } from '../utils/config.js';

// Configuración de prueba de resistencia
export let options = {
    stages: [
        { duration: '5m', target: 20 },   // Ramp-up gradual
        { duration: '30m', target: 20 },  // Carga sostenida por 30 minutos
        { duration: '5m', target: 0 },    // Ramp-down suave
    ],

    thresholds: {
        // Thresholds estrictos para pruebas de resistencia
        http_req_duration: ['p(95)<800'],      // Latencia estable
        http_req_failed: ['rate<0.01'],        // Muy pocos errores
        http_reqs: ['rate>15'],                // Throughput consistente

        // Thresholds específicos por tiempo
        'http_req_duration{phase:sustained}': ['p(95)<1000'], // Permitir ligera degradación
        'http_req_failed{phase:sustained}': ['rate<0.02'],    // Tolerancia gradual

        // Monitoreo de memoria y recursos
        'http_req_duration{endpoint:health}': ['p(95)<200'],  // Health checks rápidos
    },

    // Configuraciones para pruebas largas
    noConnectionReuse: false,    // Reutilizar conexiones
    discardResponseBodies: true, // Ahorrar memoria
    userAgent: 'k6-endurance-test/1.0',

    tags: {
        test_type: 'endurance'
    }
};

// Variables globales para tracking
let iterationCounter = 0;
let errorCounter = 0;
let memoryLeakDetector = [];

export default function () {
    iterationCounter++;

    // Determinar fase actual
    const phase = determineEndurancePhase();

    // Simular diferentes patrones de uso durante el tiempo
    const userPattern = selectEndurancePattern(iterationCounter);

    switch (userPattern) {
        case 'steady_state':
            performSteadyStateOperations(phase);
            break;
        case 'periodic_burst':
            performPeriodicBurstOperations(phase);
            break;
        case 'maintenance_operations':
            performMaintenanceOperations(phase);
            break;
        case 'memory_intensive':
            performMemoryIntensiveOperations(phase);
            break;
        default:
            performSteadyStateOperations(phase);
    }

    // Monitoreo de memory leaks
    detectMemoryLeaks();

    // Logging periódico para pruebas largas
    if (iterationCounter % 100 === 0) {
        console.log(`🕒 Endurance Test - Iteration ${iterationCounter}, VU ${__VU}, Errors: ${errorCounter}`);
    }

    // Sleep variable para simular comportamiento real
    const sleepTime = getEnduranceSleep(phase, userPattern);
    sleep(sleepTime);
}

function determineEndurancePhase() {
    const elapsed = (Date.now() - __ENV.START_TIME) / 1000 / 60; // minutos

    if (elapsed < 5) return 'rampup';
    if (elapsed > 35) return 'rampdown';
    return 'sustained';
}

function selectEndurancePattern(iteration) {
    // Cambiar patrones cada cierto tiempo para simular uso real
    const cycle = Math.floor(iteration / 50) % 4;

    switch (cycle) {
        case 0: return 'steady_state';
        case 1: return 'periodic_burst';
        case 2: return 'maintenance_operations';
        case 3: return 'memory_intensive';
        default: return 'steady_state';
    }
}

function getEnduranceSleep(phase, pattern) {
    const baseSleep = {
        'rampup': randomIntBetween(2, 4),
        'sustained': randomIntBetween(1, 3),
        'rampdown': randomIntBetween(3, 5)
    };

    const patternMultiplier = {
        'steady_state': 1,
        'periodic_burst': 0.5,
        'maintenance_operations': 2,
        'memory_intensive': 1.5
    };

    return baseSleep[phase] * patternMultiplier[pattern];
}

function performSteadyStateOperations(phase) {
    const group = 'steady_state';
    const testData = generateTestData();

    // Operaciones consistentes y predecibles
    const healthResponse = http.get(`${config.baseUrl}${endpoints.health}`, {
        tags: { endpoint: 'health', group, phase }
    });

    const healthCheck = check(healthResponse, validateGatewayResponse(healthResponse, 200), {
        tags: { endpoint: 'health', group, phase }
    });

    if (!healthCheck) errorCounter++;

    sleep(0.5);

    // Operación CRUD básica
    const createResponse = http.post(
        `${config.baseUrl}${endpoints.users.base}${endpoints.users.endpoints.create}`,
        JSON.stringify({
            ...testData.user,
            enduranceTest: true,
            iteration: iterationCounter,
            timestamp: new Date().toISOString()
        }),
        {
            headers: config.headers,
            tags: { endpoint: 'users_create', group, phase }
        }
    );
    logResponse(createResponse, 'Create User - Steady State');

    const createCheck = check(createResponse, validateGatewayResponse(createResponse, 201), {
        tags: { endpoint: 'users_create', group, phase }
    });

    if (!createCheck) errorCounter++;

    if (createCheck && createResponse.body) {
        try {
            const user = JSON.parse(createResponse.body);
            const userId = user.id;

            sleep(1);

            // Leer usuario
            const getResponse = http.get(
                `${config.baseUrl}${endpoints.users.base}${endpoints.users.endpoints.getById.replace(':id', userId)}`,
                {
                    headers: config.headers,
                    tags: { endpoint: 'users_get', group, phase }
                }
            );

            const getCheck = check(getResponse, validateGatewayResponse(getResponse, 200), {
                tags: { endpoint: 'users_get', group, phase }
            });

            if (!getCheck) errorCounter++;

            sleep(0.5);

            // Limpiar (opcional para evitar acumulación)
            if (Math.random() < 0.3) {
                const deleteResponse = http.del(
                    `${config.baseUrl}${endpoints.users.base}${endpoints.users.endpoints.delete.replace(':id', userId)}`,
                    null,
                    {
                        headers: config.headers,
                        tags: { endpoint: 'users_delete', group, phase }
                    }
                );

                check(deleteResponse, validateGatewayResponse(deleteResponse, 204), {
                    tags: { endpoint: 'users_delete', group, phase }
                });
            }

        } catch (e) {
            console.error(`Error in steady state operations: ${e.message}`);
            errorCounter++;
        }
    }
}

function performPeriodicBurstOperations(phase) {
    const group = 'periodic_burst';
    const testData = generateTestData();

    // Ráfagas periódicas de actividad
    const burstSize = randomIntBetween(3, 7);

    for (let i = 0; i < burstSize; i++) {
        const operation = randomIntBetween(1, 100);

        if (operation <= 50) {
            // 50% - Lecturas rápidas
            const listResponse = http.get(
                `${config.baseUrl}${endpoints.users.base}${endpoints.users.endpoints.list}?page=${i}&limit=10`,
                {
                    headers: config.headers,
                    tags: { endpoint: 'users_list', group, phase, burst_index: i.toString() }
                }
            );

            const listCheck = check(listResponse, validateGatewayResponse(listResponse, 200), {
                tags: { endpoint: 'users_list', group, phase }
            });

            if (!listCheck) errorCounter++;

        } else if (operation <= 80) {
            // 30% - Análisis rápidos
            const quickAnalysis = {
                url: `https://endurance-burst-${i}.com`,
                type: 'quick',
                options: { lightweight: true }
            };

            const analysisResponse = http.post(
                `${config.baseUrl}${endpoints.analysis.base}${endpoints.analysis.endpoints.analyze}`,
                JSON.stringify(quickAnalysis),
                {
                    headers: config.headers,
                    tags: { endpoint: 'analysis_start', group, phase, burst_index: i.toString() }
                }
            );

            const analysisCheck = check(analysisResponse, validateGatewayResponse(analysisResponse, 202), {
                tags: { endpoint: 'analysis_start', group, phase }
            });

            if (!analysisCheck) errorCounter++;

        } else {
            // 20% - Reportes ligeros
            const lightReport = {
                title: `Burst Report ${i}`,
                format: 'json',
                lightweight: true
            };

            const reportResponse = http.post(
                `${config.baseUrl}${endpoints.reports.base}${endpoints.reports.endpoints.generate}`,
                JSON.stringify(lightReport),
                {
                    headers: config.headers,
                    tags: { endpoint: 'reports_generate', group, phase, burst_index: i.toString() }
                }
            );

            const reportCheck = check(reportResponse, validateGatewayResponse(reportResponse, 202), {
                tags: { endpoint: 'reports_generate', group, phase }
            });

            if (!reportCheck) errorCounter++;
        }

        sleep(0.1); // Sleep mínimo entre operaciones de ráfaga
    }
}

function performMaintenanceOperations(phase) {
    const group = 'maintenance';

    // Simular operaciones de mantenimiento/monitoreo
    const maintenanceOperations = [
        {
            name: 'Health Check',
            url: `${config.baseUrl}${endpoints.health}`,
            method: 'GET'
        },
        {
            name: 'Metrics Check',
            url: `${config.baseUrl}${endpoints.metrics}`,
            method: 'GET'
        },
        {
            name: 'Ready Check',
            url: `${config.baseUrl}${endpoints.ready}`,
            method: 'GET'
        }
    ];

    maintenanceOperations.forEach((op, index) => {
        const response = http.get(op.url, {
            tags: { endpoint: op.name.toLowerCase().replace(' ', '_'), group, phase }
        });

        const operationCheck = check(response, validateGatewayResponse(response, 200), {
            tags: { endpoint: op.name.toLowerCase().replace(' ', '_'), group, phase }
        });

        if (!operationCheck) errorCounter++;

        sleep(1); // Pausa entre operaciones de mantenimiento
    });

    // Verificar listas para detectar memory leaks
    const usersResponse = http.get(
        `${config.baseUrl}${endpoints.users.base}${endpoints.users.endpoints.list}?limit=100`,
        {
            headers: config.headers,
            tags: { endpoint: 'users_list_large', group, phase }
        }
    );

    if (usersResponse.body) {
        try {
            const users = JSON.parse(usersResponse.body);
            memoryLeakDetector.push({
                timestamp: Date.now(),
                userCount: users.length || 0,
                responseSize: usersResponse.body.length
            });

            // Mantener solo los últimos 10 registros
            if (memoryLeakDetector.length > 10) {
                memoryLeakDetector = memoryLeakDetector.slice(-10);
            }
        } catch (e) {
            console.error(`Error parsing users response: ${e.message}`);
        }
    }
}

function performMemoryIntensiveOperations(phase) {
    const group = 'memory_intensive';
    const testData = generateTestData();

    // Operaciones que pueden causar memory leaks
    const largeData = {
        ...testData.analysis,
        largePayload: Array.from({ length: 100 }, (_, i) => ({
            index: i,
            data: `large_data_string_${Math.random().toString(36).substring(7)}`,
            timestamp: new Date().toISOString(),
            metadata: {
                iteration: iterationCounter,
                vu: __VU,
                phase: phase
            }
        }))
    };

    const largeResponse = http.post(
        `${config.baseUrl}${endpoints.analysis.base}${endpoints.analysis.endpoints.batch}`,
        JSON.stringify({ requests: [largeData] }),
        {
            headers: config.headers,
            timeout: '30s',
            tags: { endpoint: 'analysis_batch_large', group, phase }
        }
    );
    logResponse(largeResponse, 'Large Batch Analysis - Memory Intensive');

    const largeCheck = check(largeResponse, {
        'large operation handled': (r) => r.status < 500
    }, {
        tags: { endpoint: 'analysis_batch_large', group, phase }
    });

    if (!largeCheck) errorCounter++;

    // Forzar garbage collection simulation
    sleep(2);

    // Verificar que el sistema sigue respondiendo
    const healthAfterLarge = http.get(`${config.baseUrl}${endpoints.health}`, {
        tags: { endpoint: 'health_after_large', group, phase }
    });

    const healthAfterCheck = check(healthAfterLarge, validateGatewayResponse(healthAfterLarge, 200), {
        tags: { endpoint: 'health_after_large', group, phase }
    });

    if (!healthAfterCheck) errorCounter++;
}

function detectMemoryLeaks() {
    if (memoryLeakDetector.length >= 5) {
        const first = memoryLeakDetector[0];
        const last = memoryLeakDetector[memoryLeakDetector.length - 1];

        const userGrowth = last.userCount - first.userCount;
        const responseGrowth = last.responseSize - first.responseSize;

        if (userGrowth > 1000 || responseGrowth > 100000) {
            console.log(`⚠️  Potential memory leak detected: Users +${userGrowth}, Response size +${responseGrowth}`);
        }
    }
}

export function setup() {
    console.log('🏃‍♂️ Starting Gateway Endurance Test...');
    console.log(`🎯 Target: ${config.baseUrl}`);
    console.log('⏰ Duration: 40 minutes (5m ramp-up + 30m sustained + 5m ramp-down)');
    console.log('🔍 Monitoring: memory leaks, resource exhaustion, degradation');

    // Verificar que el Gateway esté disponible
    const healthCheck = http.get(`${config.baseUrl}${endpoints.health}`);
    if (healthCheck.status !== 200) {
        throw new Error(`Gateway not available: ${healthCheck.status}`);
    }

    console.log('✅ Gateway is ready for endurance testing');

    // Establecer tiempo de inicio para cálculos de fase
    __ENV.START_TIME = Date.now();

    return {
        startTime: Date.now(),
        sustainedDuration: 30, // minutos
        maxAcceptableErrors: 100
    };
}

export function teardown(data) {
    const duration = (Date.now() - data.startTime) / 1000 / 60; // minutos
    const errorRate = errorCounter / iterationCounter;

    console.log(`✅ Endurance test completed in ${duration.toFixed(2)} minutes`);
    console.log(`📊 Total iterations: ${iterationCounter}`);
    console.log(`❌ Total errors: ${errorCounter}`);
    console.log(`📈 Error rate: ${(errorRate * 100).toFixed(3)}%`);

    // Evaluación de resultados
    if (errorCounter <= data.maxAcceptableErrors) {
        console.log('🎉 PASSED: System maintained stability during endurance test');
    } else {
        console.log('❌ FAILED: Too many errors during endurance test');
    }

    // Resumen de memory leak detection
    if (memoryLeakDetector.length > 0) {
        const first = memoryLeakDetector[0];
        const last = memoryLeakDetector[memoryLeakDetector.length - 1];
        console.log(`🧠 Memory analysis: Users ${first.userCount} → ${last.userCount}, Response size ${first.responseSize} → ${last.responseSize}`);
    }

    console.log('🏁 ENDURANCE TEST COMPLETED');
}