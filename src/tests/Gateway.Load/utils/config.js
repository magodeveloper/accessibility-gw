// utils/config.js
// Configuración centralizada para las pruebas de carga del Gateway

export const config = {
    // URLs base - Usar variable de entorno o default a puerto 8100 (Gateway)
    baseUrl: __ENV.BASE_URL || 'http://localhost:8100',

    // Configuraciones de carga por usuarios concurrentes
    concurrentUsers: {
        light: {
            users: Number.parseInt(__ENV.USERS) || 20,
            duration: __ENV.DURATION || '5m'
        },
        medium: {
            users: Number.parseInt(__ENV.USERS) || 50,
            duration: __ENV.DURATION || '5m'
        },
        high: {
            users: Number.parseInt(__ENV.USERS) || 100,
            duration: __ENV.DURATION || '10m'
        },
        extreme: {
            users: Number.parseInt(__ENV.USERS) || 500,
            duration: __ENV.DURATION || '15m'
        }
    },

    // Thresholds por nivel de carga
    thresholds: {
        light: {
            http_req_duration: ['p(95)<300'],
            http_req_failed: ['rate<0.005'],
            http_reqs: ['rate>15']
        },
        medium: {
            http_req_duration: ['p(95)<500'],
            http_req_failed: ['rate<0.01'],
            http_reqs: ['rate>30']
        },
        high: {
            http_req_duration: ['p(95)<800'],
            http_req_failed: ['rate<0.02'],
            http_reqs: ['rate>50']
        },
        extreme: {
            http_req_duration: ['p(95)<1500'],
            http_req_failed: ['rate<0.05'],
            http_reqs: ['rate>100']
        }
    },

    // Headers comunes
    headers: {
        'Content-Type': 'application/json',
        'Accept': 'application/json',
        'User-Agent': 'Gateway-LoadTest/1.0'
    },

    // Configuración de sleep
    sleep: {
        min: Number.parseFloat(__ENV.SLEEP_MIN) || 0.5,
        max: Number.parseFloat(__ENV.SLEEP_MAX) || 2
    }
};

// Endpoints principales del Gateway
export const endpoints = {
    health: '/health',
    ready: '/ready',
    metrics: '/metrics',

    // Servicios a través del Gateway
    users: {
        base: '/api/v1/services/users',
        endpoints: {
            list: '/users',
            create: '/users',
            getById: '/users/:id',
            update: '/users/:id',
            delete: '/users/:id',
            profile: '/users/:id/profile',
            preferences: '/users/:id/preferences',
            largeRequest: '/large-request'
        }
    },

    analysis: {
        base: '/api/v1/services/analysis',
        endpoints: {
            analyze: '/analyze',
            reports: '/reports',
            batch: '/batch-analyze',
            status: '/status/:id'
        }
    },

    reports: {
        base: '/api/v1/services/reports',
        endpoints: {
            generate: '/generate',
            list: '/reports',
            download: '/reports/:id/download',
            export: '/reports/:id/export'
        }
    }
};

// Función para generar datos de prueba
export function generateTestData() {
    return {
        user: {
            name: `TestUser_${Math.random().toString(36).substring(7)}`,
            email: `test_${Math.random().toString(36).substring(7)}@example.com`,
            age: Math.floor(Math.random() * 50) + 18,
            preferences: {
                theme: Math.random() > 0.5 ? 'dark' : 'light',
                notifications: Math.random() > 0.5,
                language: ['es', 'en', 'fr'][Math.floor(Math.random() * 3)]
            }
        },
        analysis: {
            url: `https://example.com/page${Math.floor(Math.random() * 1000)}`,
            type: ['accessibility', 'performance', 'seo'][Math.floor(Math.random() * 3)],
            options: {
                includeImages: Math.random() > 0.5,
                checkContrast: Math.random() > 0.5,
                validateHtml: Math.random() > 0.5
            }
        },
        report: {
            title: `Report_${Math.random().toString(36).substring(7)}`,
            format: ['pdf', 'html', 'json'][Math.floor(Math.random() * 3)],
            includeDetails: Math.random() > 0.5
        }
    };
}

// Función para logging personalizado
export function logResponse(response, context) {
    if (response.status >= 400) {
        console.error(`❌ ${context}: ${response.status} - ${response.body}`);
    } else if (__ENV.VERBOSE === 'true') {
        console.log(`✅ ${context}: ${response.status} (${response.timings.duration}ms)`);
    }
}

// Función para validar respuestas del Gateway
export function validateGatewayResponse(response, expectedStatus = 200) {
    const checks = {
        [`status is ${expectedStatus}`]: response.status === expectedStatus,
        'response time reasonable': response.timings.duration < 5000,
        'has correlation id': response.headers['x-correlation-id'] !== undefined
    };

    if (response.status === 200 && response.body) {
        try {
            JSON.parse(response.body);
            checks['valid json response'] = true;
        } catch (e) {
            checks['valid json response'] = false;
        }
    }

    return checks;
}

// Función para obtener configuración basada en usuarios
export function getConfigForUsers(userCount) {
    if (userCount <= 20) return 'light';
    if (userCount <= 50) return 'medium';
    if (userCount <= 100) return 'high';
    return 'extreme';
}

// Función para obtener thresholds completos para un nivel de carga
export function getThresholdsForLevel(level) {
    const baseThresholds = config.thresholds[level] || config.thresholds.light;

    return {
        // Thresholds HTTP básicos de K6
        http_req_duration: baseThresholds.http_req_duration,
        http_req_failed: baseThresholds.http_req_failed,
        http_reqs: baseThresholds.http_reqs,

        // Métricas adicionales (si se usan en los scenarios)
        iteration_duration: ['avg<5000'],
        iterations: ['count>0']
    };
}

// Función para obtener stages de carga según el nivel y modo
export function getStagesForLevel(level, isSimpleMode = false) {
    const stages = {
        light: {
            simple: [
                { duration: '10s', target: 5 },
                { duration: '20s', target: 20 },
                { duration: '60s', target: 20 },
                { duration: '10s', target: 0 }
            ],
            full: [
                { duration: '30s', target: 5 },
                { duration: '1m', target: 20 },
                { duration: '3m', target: 20 },
                { duration: '30s', target: 0 }
            ]
        },
        medium: {
            simple: [
                { duration: '15s', target: 15 },
                { duration: '15s', target: 30 },
                { duration: '15s', target: 50 },
                { duration: '90s', target: 50 },
                { duration: '15s', target: 0 }
            ],
            full: [
                { duration: '1m', target: 15 },
                { duration: '1m', target: 30 },
                { duration: '1m', target: 50 },
                { duration: '5m', target: 50 },
                { duration: '1m', target: 25 },
                { duration: '30s', target: 0 }
            ]
        },
        high: {
            simple: [
                { duration: '20s', target: 25 },
                { duration: '20s', target: 50 },
                { duration: '20s', target: 100 },
                { duration: '120s', target: 100 },
                { duration: '20s', target: 0 }
            ],
            full: [
                { duration: '2m', target: 25 },
                { duration: '2m', target: 50 },
                { duration: '2m', target: 75 },
                { duration: '2m', target: 100 },
                { duration: '10m', target: 100 },
                { duration: '2m', target: 50 },
                { duration: '1m', target: 0 }
            ]
        },
        extreme: {
            simple: [
                { duration: '30s', target: 100 },
                { duration: '30s', target: 250 },
                { duration: '30s', target: 500 },
                { duration: '180s', target: 500 },
                { duration: '30s', target: 0 }
            ],
            full: [
                { duration: '3m', target: 100 },
                { duration: '3m', target: 200 },
                { duration: '3m', target: 350 },
                { duration: '3m', target: 500 },
                { duration: '15m', target: 500 },
                { duration: '3m', target: 250 },
                { duration: '2m', target: 0 }
            ]
        }
    };

    const mode = isSimpleMode ? 'simple' : 'full';
    return stages[level]?.[mode] || stages.light.full;
}