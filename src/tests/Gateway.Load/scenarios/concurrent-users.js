// scenarios/concurrent-users.js
// Prueba de carga GENÉRICA para usuarios concurrentes - Archivo único parametrizable
// 
// CONFIGURACIÓN VÍA VARIABLES DE ENTORNO:
//   USERS          - Número de usuarios concurrentes (default: 20)
//   TEST_LEVEL     - Nivel de prueba: light, medium, high, extreme (default: light)
//   TEST_MODE      - Modo: simple (solo /health y /metrics) o full (todos los servicios)
//   DURATION       - Duración del test (default: según TEST_LEVEL)
//   SCENARIO_TYPE  - Tipo de escenario: simple o full (alias de TEST_MODE)
//
// EJEMPLOS DE USO:
//   # Test ligero (20 usuarios)
//   k6 run --env USERS=20 --env TEST_LEVEL=light scenarios/concurrent-users.js
//   
//   # Test medio en modo simple (sin microservicios)
//   k6 run --env USERS=50 --env TEST_LEVEL=medium --env TEST_MODE=simple scenarios/concurrent-users.js
//   
//   # Test pesado completo
//   k6 run --env USERS=100 --env TEST_LEVEL=high --env TEST_MODE=full scenarios/concurrent-users.js
//   
//   # Test extremo
//   k6 run --env USERS=500 --env TEST_LEVEL=extreme scenarios/concurrent-users.js

import http from 'k6/http';
import { check, sleep } from 'k6';
import { config, endpoints, generateTestData, validateGatewayResponse, getThresholdsForLevel, getStagesForLevel } from '../utils/config.js';
import { recordRequestMetrics } from '../utils/metrics.js';
import { generateTestUserToken, createAuthHeaders } from '../utils/jwt.js';
import { executeHealthChecks, executeUserOperations, executeAnalysisOperations, executeReportOperations } from '../utils/scenarios-common.js';

// ===== CONFIGURACIÓN PARAMETRIZABLE =====
const userCount = Number.parseInt(__ENV.USERS) || 20;
const testLevel = (__ENV.TEST_LEVEL || 'light').toLowerCase();
const testMode = (__ENV.TEST_MODE || __ENV.SCENARIO_TYPE || 'full').toLowerCase();
const isSimpleMode = testMode === 'simple';

// Validar nivel de prueba
const validLevels = ['light', 'medium', 'high', 'extreme'];
if (!validLevels.includes(testLevel)) {
    throw new Error(`Invalid TEST_LEVEL: ${testLevel}. Must be one of: ${validLevels.join(', ')}`);
}

// ===== CONFIGURACIÓN K6 =====
export const options = {
    stages: getStagesForLevel(testLevel, isSimpleMode),
    thresholds: getThresholdsForLevel(testLevel),
    tags: {
        test_type: 'concurrent_users',
        user_count: userCount.toString(),
        test_level: testLevel,
        mode: testMode
    }
};

// ===== SETUP =====
export function setup() {
    console.log('╔══════════════════════════════════════════════════╗');
    console.log(`║  🚀 CONCURRENT USERS LOAD TEST                   ║`);
    console.log('╚══════════════════════════════════════════════════╝');
    console.log(`📊 Usuarios Concurrentes: ${userCount}`);
    console.log(`📈 Nivel de Prueba: ${testLevel.toUpperCase()}`);
    console.log(`🔧 Modo: ${testMode.toUpperCase()}`);
    console.log(`🌐 Base URL: ${config.baseUrl}`);
    console.log('');

    if (isSimpleMode) {
        console.log('ℹ️  Modo SIMPLE: Solo endpoints básicos (/health, /ready, /metrics)');
        console.log('   No se requieren microservicios externos');

        // Generar token JWT
        const token = generateTestUserToken(`load-test-user-${userCount}`);
        const authHeaders = createAuthHeaders(token);
        console.log('✅ Token JWT generado');

        // Verificar Gateway con timeout corto
        const healthCheck = http.get(`${config.baseUrl}${endpoints.health}`, {
            timeout: '5s'
        });

        if (healthCheck.status === 0 || healthCheck.error) {
            console.error(`❌ Gateway no accesible en ${config.baseUrl}`);
            console.error(`   Error: ${healthCheck.error || 'Conexión fallida'}`);
            console.error(`   Verifica que el Gateway esté corriendo en el puerto correcto`);
            throw new Error(`Gateway no accesible: ${healthCheck.error || 'Conexión fallida'}`);
        }
        console.log(`✅ Gateway Health: ${healthCheck.status}`);

        const readyCheck = http.get(`${config.baseUrl}${endpoints.ready}`, {
            headers: authHeaders,
            timeout: '5s'
        });
        console.log(`✅ Gateway Ready: ${readyCheck.status}`);

        return {
            startTime: Date.now(),
            mode: 'simple',
            token: token,
            authHeaders: authHeaders
        };
    } else {
        console.log('ℹ️  Modo FULL: Todos los servicios (usuarios, análisis, reportes)');
        console.log('   Se requieren microservicios activos');

        // Verificar Gateway con timeout corto
        const healthCheck = http.get(`${config.baseUrl}${endpoints.health}`, {
            timeout: '5s'
        });

        if (healthCheck.status !== 200) {
            console.error(`❌ Gateway no disponible en ${config.baseUrl}`);
            console.error(`   Status: ${healthCheck.status}`);
            console.error(`   Verifica que el Gateway esté corriendo`);
            throw new Error(`Gateway no disponible: ${healthCheck.status}`);
        }
        console.log('✅ Gateway disponible y listo');

        return {
            startTime: Date.now(),
            mode: 'full'
        };
    }
}

// ===== FUNCIÓN PRINCIPAL DE TEST =====
export default function concurrentUsersTest(data) {
    if (isSimpleMode) {
        executeSimpleMode(data);
    } else {
        executeFullMode();
    }
}

// ===== MODO SIMPLE: Solo endpoints básicos =====
function executeSimpleMode(data) {
    const rand = Math.random();

    if (rand < 0.5) {
        // 50% - Health Check
        const response = http.get(`${config.baseUrl}${endpoints.health}`, {
            tags: { endpoint: 'health', mode: 'simple' }
        });

        check(response, validateGatewayResponse(response, 200));
        recordRequestMetrics(response, 'gateway');

    } else if (rand < 0.8) {
        // 30% - Ready Check (con auth)
        const response = http.get(`${config.baseUrl}${endpoints.ready}`, {
            headers: data.authHeaders,
            tags: { endpoint: 'ready', mode: 'simple' }
        });

        check(response, validateGatewayResponse(response, 200));
        recordRequestMetrics(response, 'gateway');

    } else {
        // 20% - Metrics Check
        const response = http.get(`${config.baseUrl}${endpoints.metrics}`, {
            tags: { endpoint: 'metrics', mode: 'simple' }
        });

        check(response, validateGatewayResponse(response, 200));
        recordRequestMetrics(response, 'gateway');
    }

    sleep(Math.random() * (config.sleep.max - config.sleep.min) + config.sleep.min);
}

// ===== MODO FULL: Todos los servicios =====
function executeFullMode() {
    const testData = generateTestData();
    const rand = Math.random();

    if (rand < 0.15) {
        // 15% - Health Checks
        executeHealthChecks('concurrent_full');
        sleep(0.5);

    } else if (rand < 0.5) {
        // 35% - Operaciones de Usuarios
        executeUserOperations(testData.user, 'concurrent_full');
        sleep(Math.random() + 0.5);

    } else if (rand < 0.75) {
        // 25% - Operaciones de Análisis
        executeAnalysisOperations(testData.analysis, 'concurrent_full');
        sleep(Math.random() + 1);

    } else {
        // 25% - Operaciones de Reportes
        executeReportOperations(testData.report, 'concurrent_full');
        sleep(Math.random() + 1);
    }
}

// ===== TEARDOWN =====
export function teardown(data) {
    const duration = (Date.now() - data.startTime) / 1000;
    console.log('');
    console.log('╔══════════════════════════════════════════════════╗');
    console.log('║  ✅ TEST COMPLETADO                              ║');
    console.log('╚══════════════════════════════════════════════════╝');
    console.log(`⏱️  Duración total: ${duration.toFixed(2)}s`);
    console.log(`📊 Usuarios: ${userCount} | Nivel: ${testLevel} | Modo: ${testMode}`);
    console.log('');
}
