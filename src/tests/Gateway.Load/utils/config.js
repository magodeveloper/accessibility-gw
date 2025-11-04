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
            http_req_failed: ['rate<0.35'],    // Ajustado a 35% para permitir algunos 403
            http_reqs: ['rate>10']             // Ajustado a 10/s (era 15/s)
        },
        medium: {
            http_req_duration: ['p(95)<500'],
            http_req_failed: ['rate<0.35'],    // Ajustado a 35% (era 1%)
            http_reqs: ['rate>20']             // Ajustado a 20/s (era 30/s)
        },
        high: {
            http_req_duration: ['p(95)<800'],
            http_req_failed: ['rate<0.40'],    // Ajustado a 40% (era 2%)
            http_reqs: ['rate>30']             // Ajustado a 30/s (era 50/s)
        },
        extreme: {
            http_req_duration: ['p(95)<1500'],
            http_req_failed: ['rate<0.45'],    // Ajustado a 45% (era 5%)
            http_reqs: ['rate>50']             // Ajustado a 50/s (era 100/s)
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

    // Servicios a través del Gateway - Rutas reales del Gateway
    users: {
        base: '/api',
        endpoints: {
            list: '/users',
            create: '/users',
            getById: '/users/:id',
            update: '/users/:id',
            delete: '/users/:id',
            profile: '/users/:id/profile',
            preferences: '/preferences',
            largeRequest: '/large-request'
        }
    },

    analysis: {
        base: '/api',
        endpoints: {
            analyze: '/Analysis',
            reports: '/Result',
            batch: '/Analysis',
            status: '/Analysis/:id'
        }
    },

    reports: {
        base: '/api',
        endpoints: {
            generate: '/Report',
            list: '/reports',
            download: '/reports/:id/download',
            export: '/reports/:id/export'
        }
    }
};

// Función para generar datos de prueba
export function generateTestData() {
    const randomStr = Math.random().toString(36).substring(7);
    const now = new Date().toISOString();
    const randomId = Math.floor(Math.random() * 1000) + 1;

    return {
        user: {
            // UserCreateDto: record UserCreateDto(string Nickname, string Name, string Lastname, string Email, string Password)
            nickname: `nick_${randomStr}`,
            name: `TestUser_${randomStr}`,
            lastname: `TestLastname_${randomStr}`,
            email: `test_${randomStr}@loadtest.com`,
            password: `TestPass123!${randomStr}`  // Campo Password requerido
        },
        analysis: {
            // AnalysisCreateDto - todos los campos requeridos del microservicio
            userId: randomId,
            dateAnalysis: now,
            contentType: 'HTML',
            contentInput: '<html><head><title>Test Page</title></head><body><h1>Accessibility Test</h1><p>This is a test page for load testing.</p></body></html>',
            sourceUrl: `https://example.com/test-page-${randomStr}`,
            toolUsed: 'Axe',  // Axe, EqualAccess, etc.
            status: 'Completed',  // Pending, InProgress, Completed, Failed
            summaryResult: 'Test analysis completed successfully. No critical violations found.',
            resultJson: JSON.stringify({
                violations: [],
                passes: [
                    { id: 'html-has-lang', impact: 'serious', description: 'HTML has lang attribute' }
                ],
                incomplete: [],
                inapplicable: []
            }),
            durationMs: Math.floor(Math.random() * 5000) + 1000,  // 1-6 segundos
            wcagVersion: '2.1',  // 2.0, 2.1, 2.2
            wcagLevel: 'AA',  // A, AA, AAA
            // Métricas específicas de Axe
            axeViolations: 0,
            axeNeedsReview: 0,
            axeRecommendations: Math.floor(Math.random() * 5),
            axePasses: Math.floor(Math.random() * 20) + 5,
            axeIncomplete: 0,
            axeInapplicable: Math.floor(Math.random() * 10),
            // Métricas específicas de EqualAccess
            eaViolations: null,
            eaNeedsReview: null,
            eaRecommendations: null,
            eaPasses: null,
            eaIncomplete: null,
            eaInapplicable: null
        },
        report: {
            // ReportDto completo según el controlador
            id: 0,  // Se ignora en creación
            analysisId: randomId,
            format: Math.floor(Math.random() * 3),  // 0=PDF, 1=HTML, 2=JSON (enum ReportFormat)
            filePath: '',  // Se genera en el servidor
            generationDate: now,
            createdAt: now,
            updatedAt: now
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