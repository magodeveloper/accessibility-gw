// utils/metrics.js
// Métricas personalizadas para las pruebas de carga del Gateway

import { Counter, Gauge, Rate, Trend } from 'k6/metrics';

// Métricas personalizadas del Gateway
export const gatewayMetrics = {
    // Contadores
    gatewayRequests: new Counter('gateway_requests_total'),
    gatewayErrors: new Counter('gateway_errors_total'),
    serviceRequests: new Counter('service_requests_total'),
    serviceErrors: new Counter('service_errors_total'),

    // Rates
    gatewayErrorRate: new Rate('gateway_error_rate'),
    serviceErrorRate: new Rate('service_error_rate'),
    timeoutRate: new Rate('timeout_rate'),

    // Trends (para percentiles)
    gatewayDuration: new Trend('gateway_duration'),
    serviceDuration: new Trend('service_duration'),
    queueTime: new Trend('queue_time'),

    // Gauges (No usadas directamente - K6 provee vus y vus_max built-in)
    // activeConnections: new Gauge('active_connections'),
    // concurrentUsers: new Gauge('concurrent_users')
};

// Función para registrar métricas de request
export function recordRequestMetrics(response, service = 'gateway') {
    const isError = response.status >= 400;
    const isTimeout = response.timings.duration > 10000;

    // Contadores
    gatewayMetrics.gatewayRequests.add(1);
    if (isError) {
        gatewayMetrics.gatewayErrors.add(1);
    }

    if (service !== 'gateway') {
        gatewayMetrics.serviceRequests.add(1);
        if (isError) {
            gatewayMetrics.serviceErrors.add(1);
        }
    }

    // Rates
    gatewayMetrics.gatewayErrorRate.add(isError);
    if (service !== 'gateway') {
        gatewayMetrics.serviceErrorRate.add(isError);
    }
    gatewayMetrics.timeoutRate.add(isTimeout);

    // Trends
    gatewayMetrics.gatewayDuration.add(response.timings.duration);
    if (service !== 'gateway') {
        gatewayMetrics.serviceDuration.add(response.timings.duration);
    }

    // Queue time (tiempo de espera)
    if (response.timings.waiting) {
        gatewayMetrics.queueTime.add(response.timings.waiting);
    }
}

// Funciones deshabilitadas - K6 ya provee métricas vus y vus_max built-in
// No es necesario trackear usuarios activos manualmente
export function updateActiveUsers(count) {
    // No-op: K6 ya trackea esto con 'vus' metric
}

export function updateActiveConnections(count) {
    // No-op: K6 ya trackea esto con 'vus' metric
}

// Función para obtener summary de métricas
export function getMetricsSummary() {
    return {
        requests: {
            total: gatewayMetrics.gatewayRequests.count,
            errors: gatewayMetrics.gatewayErrors.count,
            errorRate: gatewayMetrics.gatewayErrorRate.rate
        },
        performance: {
            avgDuration: gatewayMetrics.gatewayDuration.avg,
            p95Duration: gatewayMetrics.gatewayDuration.p(95),
            p99Duration: gatewayMetrics.gatewayDuration.p(99)
        },
        services: {
            requests: gatewayMetrics.serviceRequests.count,
            errors: gatewayMetrics.serviceErrors.count,
            errorRate: gatewayMetrics.serviceErrorRate.rate
        }
    };
}

// Función para logging de métricas en tiempo real
export function logMetrics(iteration) {
    if (iteration % 100 === 0) { // Log cada 100 iteraciones
        const summary = getMetricsSummary();
        console.log(`📊 Iteration ${iteration} - Requests: ${summary.requests.total}, Errors: ${summary.requests.errors}, Avg Duration: ${summary.performance.avgDuration.toFixed(2)}ms`);
    }
}

// Configuración de métricas por defecto para k6
export const defaultMetricsConfig = {
    // Métricas HTTP básicas
    http_req_duration: ['avg', 'p(90)', 'p(95)', 'p(99)', 'max'],
    http_req_failed: ['rate'],
    http_reqs: ['count', 'rate'],

    // Métricas de iteraciones
    iteration_duration: ['avg', 'p(95)'],
    iterations: ['count', 'rate'],

    // Métricas de VUs
    vus: ['value'],
    vus_max: ['value']
};

// ⚠️ NOTA: Los thresholds personalizados se definen en utils/config.js
// Usar la función getThresholdsForLevel() desde config.js para obtener thresholds