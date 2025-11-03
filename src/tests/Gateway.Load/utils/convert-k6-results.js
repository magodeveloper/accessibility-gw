// utils/convert-k6-results.js
// Script para convertir resultados NDJSON de K6 a formato resumido para el dashboard

const fs = require('node:fs');
const path = require('node:path');

/**
 * Procesa una métrica individual del tipo Point
 */
function processMetricPoint(metricName, value, metrics) {
    if (metricName === 'http_req_duration' && value > 0) {
        metrics.http_req_duration.values.push(value);
        return { totalRequests: 0, failedRequests: 0 };
    }

    if (metricName === 'http_reqs') {
        metrics.http_reqs.count++;
        return { totalRequests: 1, failedRequests: 0 };
    }

    if (metricName === 'http_req_failed' && value === 1) {
        metrics.http_req_failed.count++;
        return { totalRequests: 0, failedRequests: 1 };
    }

    if (metricName === 'iterations') {
        metrics.iterations.count++;
    } else if (metricName === 'data_sent') {
        metrics.data_sent.count += value;
    } else if (metricName === 'data_received') {
        metrics.data_received.count += value;
    }

    return { totalRequests: 0, failedRequests: 0 };
}

/**
 * Calcula estadísticas de duración de requests
 */
function calculateDurationStats(metrics) {
    if (metrics.http_req_duration.values.length === 0) return;

    const sortedValues = [...metrics.http_req_duration.values];
    sortedValues.sort((a, b) => a - b);
    const sum = sortedValues.reduce((a, b) => a + b, 0);

    metrics.http_req_duration.avg = sum / sortedValues.length;
    metrics.http_req_duration.p95 = percentile(sortedValues, 95);
    metrics.http_req_duration.p99 = percentile(sortedValues, 99);
}

/**
 * Crea el objeto de salida en formato esperado por el dashboard
 */
function createOutputObject(metrics, users, testRunDurationMs) {
    return {
        metrics: {
            http_req_duration: {
                avg: roundTo(metrics.http_req_duration.avg, 1),
                'p(95)': roundTo(metrics.http_req_duration.p95, 1),
                'p(99)': roundTo(metrics.http_req_duration.p99, 1)
            },
            http_reqs: {
                count: metrics.http_reqs.count,
                rate: roundTo(metrics.http_reqs.rate, 1)
            },
            http_req_failed: {
                count: metrics.http_req_failed.count,
                rate: roundTo(metrics.http_req_failed.rate, 4)
            },
            iterations: {
                count: metrics.iterations.count
            },
            data_sent: {
                count: metrics.data_sent.count
            },
            data_received: {
                count: metrics.data_received.count
            }
        },
        options: {
            scenarios: {
                default: {
                    executor: 'ramping-vus',
                    vus: users,
                    duration: Math.round(testRunDurationMs / 1000) + 's'
                }
            }
        },
        state: {
            testRunDurationMs: Math.round(testRunDurationMs)
        }
    };
}

/**
 * Convierte archivo NDJSON de K6 a formato JSON resumido
 * @param {string} inputFile - Ruta al archivo NDJSON de K6
 * @param {string} outputFile - Ruta al archivo JSON de salida
 * @param {number} users - Número de usuarios concurrentes
 */
function convertK6Results(inputFile, outputFile, users) {
    console.log(`📊 Convirtiendo ${inputFile}...`);

    // Leer archivo NDJSON
    const content = fs.readFileSync(inputFile, 'utf8');
    const lines = content.split('\n').filter(line => line.trim());

    // Estructuras para acumular métricas
    const metrics = {
        http_req_duration: { values: [], avg: 0, p95: 0, p99: 0 },
        http_reqs: { count: 0, rate: 0 },
        http_req_failed: { count: 0, rate: 0 },
        iterations: { count: 0 },
        data_sent: { count: 0 },
        data_received: { count: 0 }
    };

    let testRunDurationMs = 0;
    let totalRequests = 0;
    let failedRequests = 0;

    // Procesar cada línea NDJSON
    for (const line of lines) {
        try {
            const entry = JSON.parse(line);

            // Procesar métricas de tipo Point
            if (entry.type === 'Point' && entry.data) {
                const result = processMetricPoint(entry.metric, entry.data.value, metrics);
                totalRequests += result.totalRequests;
                failedRequests += result.failedRequests;
            }
        } catch (e) {
            // Ignorar líneas que no son JSON válido
        }
    }

    // Calcular estadísticas
    calculateDurationStats(metrics);

    // Calcular tasa de errores
    if (totalRequests > 0) {
        metrics.http_req_failed.rate = failedRequests / totalRequests;
    }

    // Estimar duración basada en el número de iteraciones y usuarios
    // Cada iteración toma aproximadamente 2 segundos
    testRunDurationMs = (metrics.iterations.count / users) * 2000;

    // Calcular rate de requests (estimado)
    if (testRunDurationMs > 0) {
        metrics.http_reqs.rate = (totalRequests / (testRunDurationMs / 1000));
    }

    // Crear objeto de salida en formato esperado por el dashboard
    const output = createOutputObject(metrics, users, testRunDurationMs);

    // Guardar archivo JSON
    fs.writeFileSync(outputFile, JSON.stringify(output, null, 2));
    console.log(`✅ Archivo convertido: ${outputFile}`);
    console.log(`   - Requests: ${metrics.http_reqs.count}`);
    console.log(`   - Error rate: ${(metrics.http_req_failed.rate * 100).toFixed(2)}%`);
    console.log(`   - Avg duration: ${metrics.http_req_duration.avg.toFixed(1)}ms`);
    console.log(`   - P95: ${metrics.http_req_duration.p95.toFixed(1)}ms`);
    console.log(`   - P99: ${metrics.http_req_duration.p99.toFixed(1)}ms`);
}

/**
 * Calcula el percentil de un array ordenado
 */
function percentile(sortedArray, percentile) {
    if (sortedArray.length === 0) return 0;
    const index = (percentile / 100) * (sortedArray.length - 1);
    const lower = Math.floor(index);
    const upper = Math.ceil(index);
    const weight = index % 1;

    if (lower === upper) {
        return sortedArray[lower];
    }

    return sortedArray[lower] * (1 - weight) + sortedArray[upper] * weight;
}

/**
 * Redondea un número a decimales específicos
 */
function roundTo(num, decimals) {
    return Math.round(num * Math.pow(10, decimals)) / Math.pow(10, decimals);
}

// Ejecutar si se llama directamente
if (require.main === module) {
    const args = process.argv.slice(2);

    if (args.length < 3) {
        console.log('Uso: node convert-k6-results.js <input-file> <output-file> <users>');
        console.log('Ejemplo: node convert-k6-results.js results/light-load-k6-real.json results/light-load-k6.json 20');
        process.exit(1);
    }

    const [inputFile, outputFile, users] = args;
    convertK6Results(inputFile, outputFile, Number.parseInt(users));
}

module.exports = { convertK6Results };
