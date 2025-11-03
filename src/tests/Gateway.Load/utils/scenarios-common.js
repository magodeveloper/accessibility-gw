// utils/scenarios-common.js
// Funciones comunes reutilizables para todos los escenarios de carga

import http from 'k6/http';
import { check, sleep } from 'k6';
import { config, endpoints, validateGatewayResponse } from './config.js';
import { recordRequestMetrics } from './metrics.js';

/**
 * Ejecuta health checks del Gateway
 * @param {string} [group='health_checks'] - Nombre del grupo para métricas
 */
export function executeHealthChecks(group = 'health_checks') {
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

/**
 * Ejecuta operaciones de usuarios (crear, obtener, actualizar preferencias)
 * @param {Object} userData - Datos del usuario para las operaciones
 * @param {string} [group='user_operations'] - Nombre del grupo para métricas
 * @returns {boolean} - true si todas las operaciones fueron exitosas
 */
export function executeUserOperations(userData, group = 'user_operations') {
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

            return true;

        } catch (e) {
            console.error(`Error parsing user response: ${e.message}`);
            return false;
        }
    }

    return false;
}

/**
 * Ejecuta operaciones de análisis de accesibilidad
 * @param {Object} analysisData - Datos para el análisis
 * @param {string} [group='analysis_operations'] - Nombre del grupo para métricas
 * @returns {boolean} - true si todas las operaciones fueron exitosas
 */
export function executeAnalysisOperations(analysisData, group = 'analysis_operations') {
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

            return true;

        } catch (e) {
            console.error(`Error parsing analysis response: ${e.message}`);
            return false;
        }
    }

    return false;
}

/**
 * Ejecuta operaciones de generación de reportes
 * @param {Object} reportData - Datos para el reporte
 * @param {string} [group='report_operations'] - Nombre del grupo para métricas
 * @returns {boolean} - true si todas las operaciones fueron exitosas
 */
export function executeReportOperations(reportData, group = 'report_operations') {
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

        return true;
    }

    return false;
}

/**
 * Ejecuta un health check simple (solo /health)
 * Usado en escenarios simplificados
 */
export function executeSimpleHealthCheck() {
    const response = http.get(`${config.baseUrl}${endpoints.health}`, {
        headers: config.headers,
        tags: { endpoint: 'health', group: 'simple_checks' }
    });

    check(response, validateGatewayResponse(response, 200), { group: 'simple_checks' });
    recordRequestMetrics(response, 'gateway');
}

/**
 * Ejecuta un metrics check (solo /metrics)
 * Usado en escenarios simplificados
 */
export function executeMetricsCheck() {
    const response = http.get(`${config.baseUrl}${endpoints.metrics}`, {
        headers: config.headers,
        tags: { endpoint: 'metrics', group: 'simple_checks' }
    });

    check(response, validateGatewayResponse(response, 200), { group: 'simple_checks' });
    recordRequestMetrics(response, 'gateway');
}

/**
 * Distribuye el tráfico entre diferentes tipos de operaciones
 * Basado en porcentajes configurables
 * @param {Object} testData - Datos de prueba generados
 * @param {string} scenarioType - 'full' o 'simple'
 */
export function executeDistributedTraffic(testData, scenarioType = 'full') {
    const rand = Math.random();

    if (scenarioType === 'simple') {
        // Escenario simple: solo health y metrics
        if (rand < 0.6) {
            executeSimpleHealthCheck();
        } else {
            executeMetricsCheck();
        }
    } else if (rand < 0.1) {
        // Escenario completo: distribución realista
        // 10% - Health checks
        executeHealthChecks();
    } else if (rand < 0.7) {
        // 60% - Operaciones de usuarios
        executeUserOperations(testData.user);
    } else if (rand < 0.9) {
        // 20% - Análisis de accesibilidad
        executeAnalysisOperations(testData.analysis);
    } else {
        // 10% - Generación de reportes
        executeReportOperations(testData.report);
    }
}

/**
 * Calcula el tiempo de sleep basado en el nivel de carga
 * @param {string} userLevel - 'light', 'medium', 'high', o 'extreme'
 * @returns {number} - Tiempo de sleep en segundos
 */
export function calculateThinkTime(userLevel) {
    const thinkTimes = {
        light: { min: 1, max: 3 },    // 1-3 segundos
        medium: { min: 0.5, max: 2 }, // 0.5-2 segundos
        high: { min: 0.3, max: 1.5 }, // 0.3-1.5 segundos
        extreme: { min: 0.1, max: 1 }  // 0.1-1 segundo
    };

    const times = thinkTimes[userLevel] || thinkTimes.medium;
    return Math.random() * (times.max - times.min) + times.min;
}

/**
 * Genera configuración de stages basada en el número de usuarios y nivel
 * @param {number} userCount - Número de usuarios concurrentes objetivo
 * @param {string} userLevel - 'light', 'medium', 'high', o 'extreme'
 * @returns {Array} - Array de stages para k6
 */
export function generateStages(userCount, userLevel) {
    const stageConfigs = {
        light: {
            rampUpTime: '30s',
            plateauTime: '3m',
            rampDownTime: '30s',
            initialTarget: Math.ceil(userCount * 0.25)
        },
        medium: {
            rampUpTime: '1m',
            plateauTime: '5m',
            rampDownTime: '1m',
            initialTarget: Math.ceil(userCount * 0.2)
        },
        high: {
            rampUpTime: '2m',
            plateauTime: '10m',
            rampDownTime: '2m',
            initialTarget: Math.ceil(userCount * 0.15)
        },
        extreme: {
            rampUpTime: '3m',
            plateauTime: '15m',
            rampDownTime: '3m',
            initialTarget: Math.ceil(userCount * 0.1),
            gradual: true // Flag para ramp-up gradual
        }
    };

    const stageConfig = stageConfigs[userLevel] || stageConfigs.medium;

    if (userLevel === 'extreme' && userCount >= 400) {
        // Para carga extrema, usar ramp-up gradual multi-etapa
        return [
            { duration: '3m', target: Math.ceil(userCount * 0.1) },
            { duration: '3m', target: Math.ceil(userCount * 0.2) },
            { duration: '3m', target: Math.ceil(userCount * 0.4) },
            { duration: '3m', target: Math.ceil(userCount * 0.6) },
            { duration: '3m', target: Math.ceil(userCount * 0.8) },
            { duration: '2m', target: userCount },
            { duration: stageConfig.plateauTime, target: userCount },
            { duration: '3m', target: Math.ceil(userCount * 0.6) },
            { duration: '2m', target: Math.ceil(userCount * 0.2) },
            { duration: '1m', target: 0 }
        ];
    }

    // Configuración estándar para otros niveles
    return [
        { duration: stageConfig.rampUpTime, target: stageConfig.initialTarget },
        { duration: stageConfig.rampUpTime, target: userCount },
        { duration: stageConfig.plateauTime, target: userCount },
        { duration: stageConfig.rampDownTime, target: 0 }
    ];
}

/**
 * Ejecuta test de capacidad básica del sistema
 * @returns {Object} - Resultado del test con passed, baseline, reason
 */
export function executeCapacityTest() {
    // Ejecutar 5 requests simples
    const responses = [];
    for (let i = 0; i < 5; i++) {
        const response = http.get(`${config.baseUrl}${endpoints.health}`, {
            timeout: '5s'
        });
        responses.push(response);

        if (response.status !== 200) {
            return {
                passed: false,
                baseline: null,
                reason: `Health check failed with status ${response.status}`
            };
        }

        sleep(0.2);
    }

    // Calcular baseline de response time
    const responseTimes = responses.map(r => r.timings.duration);
    const avgResponseTime = responseTimes.reduce((a, b) => a + b, 0) / responseTimes.length;

    // Sistema debe responder en menos de 500ms para considerarse listo
    if (avgResponseTime > 500) {
        return {
            passed: false,
            baseline: avgResponseTime,
            reason: `Average response time too high: ${avgResponseTime.toFixed(2)}ms > 500ms`
        };
    }

    return {
        passed: true,
        baseline: avgResponseTime,
        reason: 'System ready'
    };
}
