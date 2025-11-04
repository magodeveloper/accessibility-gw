// scenarios/quick-diagnostic.js
// Test de diagnóstico rápido para identificar problemas con endpoints

import http from 'k6/http';
import { check, sleep } from 'k6';
import { config, endpoints, generateTestData } from '../utils/config.js';
import { generateTestUserToken, createAuthHeaders } from '../utils/jwt.js';

// Test muy corto para diagnóstico rápido
export const options = {
    stages: [
        { duration: '10s', target: 5 },  // 5 usuarios por 10 segundos
        { duration: '5s', target: 0 }     // Ramp down
    ],
    thresholds: {
        http_req_duration: ['p(95)<1000'],
        // Sin threshold de errores para diagnóstico
    },
    tags: {
        test_type: 'diagnostic'
    }
};

export function setup() {
    console.log('╔══════════════════════════════════════════════════╗');
    console.log('║  🔍 DIAGNOSTIC TEST - Quick Endpoint Check       ║');
    console.log('╚══════════════════════════════════════════════════╝');
    console.log(`🌐 Base URL: ${config.baseUrl}`);
    console.log('');

    const token = generateTestUserToken('diagnostic-user');
    const authHeaders = createAuthHeaders(token);

    return {
        token: token,
        authHeaders: authHeaders
    };
}

export default function diagnosticTest(data) {
    const testData = generateTestData();

    // Test 1: Health endpoints (sin auth)
    console.log('🏥 Testing /health');
    let response = http.get(`${config.baseUrl}/health`);
    console.log(`   Status: ${response.status} - ${response.status === 200 ? '✅' : '❌'}`);

    sleep(0.2);

    // Test 2: POST /api/users (sin auth según config)
    console.log('👤 Testing POST /api/users (no auth)');
    response = http.post(
        `${config.baseUrl}/api/users`,
        JSON.stringify(testData.user),
        { headers: config.headers }
    );
    console.log(`   Status: ${response.status} - ${response.status === 201 ? '✅' : '❌'}`);
    if (response.status !== 201) {
        console.log(`   Body: ${response.body.substring(0, 200)}`);
    }

    sleep(0.2);

    // Test 3: POST /api/users (CON auth)
    console.log('👤 Testing POST /api/users (with auth)');
    response = http.post(
        `${config.baseUrl}/api/users`,
        JSON.stringify(testData.user),
        { headers: { ...config.headers, ...data.authHeaders } }
    );
    console.log(`   Status: ${response.status} - ${response.status === 201 ? '✅' : '❌'}`);
    if (response.status !== 201) {
        console.log(`   Body: ${response.body.substring(0, 200)}`);
    }

    sleep(0.2);

    // Test 4: GET /api/users (sin auth)
    console.log('📋 Testing GET /api/users (no auth)');
    response = http.get(
        `${config.baseUrl}/api/users`,
        { headers: config.headers }
    );
    console.log(`   Status: ${response.status} - ${response.status === 200 ? '✅' : '❌'}`);

    sleep(0.2);

    // Test 5: POST /api/Analysis (sin auth)
    console.log('🔍 Testing POST /api/Analysis (no auth)');
    response = http.post(
        `${config.baseUrl}/api/Analysis`,
        JSON.stringify(testData.analysis),
        { headers: config.headers }
    );
    console.log(`   Status: ${response.status} - Expected: 401/403, Got: ${response.status}`);

    sleep(0.2);

    // Test 6: POST /api/Analysis (CON auth)
    console.log('🔍 Testing POST /api/Analysis (with auth)');
    response = http.post(
        `${config.baseUrl}/api/Analysis`,
        JSON.stringify(testData.analysis),
        { headers: { ...config.headers, ...data.authHeaders } }
    );
    console.log(`   Status: ${response.status} - ${response.status === 201 || response.status === 202 ? '✅' : '❌'}`);
    if (response.status !== 201 && response.status !== 202) {
        console.log(`   Body: ${response.body.substring(0, 200)}`);
    }

    sleep(0.2);

    // Test 7: POST /api/Report (sin auth)
    console.log('📊 Testing POST /api/Report (no auth)');
    response = http.post(
        `${config.baseUrl}/api/Report`,
        JSON.stringify(testData.report),
        { headers: config.headers }
    );
    console.log(`   Status: ${response.status} - Expected: 401/403, Got: ${response.status}`);

    sleep(0.2);

    // Test 8: POST /api/Report (CON auth)
    console.log('📊 Testing POST /api/Report (with auth)');
    response = http.post(
        `${config.baseUrl}/api/Report`,
        JSON.stringify(testData.report),
        { headers: { ...config.headers, ...data.authHeaders } }
    );
    console.log(`   Status: ${response.status} - ${response.status === 201 || response.status === 202 ? '✅' : '❌'}`);
    if (response.status !== 201 && response.status !== 202) {
        console.log(`   Body: ${response.body.substring(0, 200)}`);
    }

    sleep(1);
}

export function teardown(data) {
    console.log('');
    console.log('╔══════════════════════════════════════════════════╗');
    console.log('║  ✅ DIAGNOSTIC TEST COMPLETED                    ║');
    console.log('╚══════════════════════════════════════════════════╝');
}
