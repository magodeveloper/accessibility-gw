// utils/jwt.js
// Utilidad para generar tokens JWT para autenticación en tests de carga

import encoding from 'k6/encoding';
import crypto from 'k6/crypto';

/**
 * Helper para errores de configuración
 */
function throwConfigError(message) {
    throw new Error(`[JWT Config Error] ${message}`);
}

/**
 * Configuración JWT del Gateway
 * IMPORTANTE: Las claves se obtienen de variables de entorno (.env)
 * NUNCA hardcodear claves secretas en el código
 */
export const jwtConfig = {
    secretKey: __ENV.JWT_SECRET || throwConfigError('JWT_SECRET is required in .env file'),
    issuer: __ENV.JWT_ISSUER || 'https://api.accessibility.company.com/users',
    audience: __ENV.JWT_AUDIENCE || 'https://accessibility.company.com',
    expirationMinutes: Number.parseInt(__ENV.JWT_EXPIRATION_MINUTES) || 60
};

/**
 * Codifica en Base64 URL-safe (sin padding)
 */
function base64UrlEncode(str) {
    return encoding.b64encode(str, 'rawurl');
}

/**
 * Genera una firma HMAC-SHA256 usando K6 crypto
 */
function hmacSha256(message, secret) {
    const hash = crypto.hmac('sha256', secret, message, 'binary');
    return encoding.b64encode(hash, 'rawurl');
}

/**
 * Genera un token JWT simple para pruebas de carga
 * 
 * @param {Object} payload - Datos del payload (ej: { sub: "user123", email: "test@test.com" })
 * @param {Object} options - Opciones adicionales (issuer, audience, expirationMinutes)
 * @returns {string} Token JWT
 */
export function generateJwtToken(payload = {}, options = {}) {
    const now = Math.floor(Date.now() / 1000);

    // Header JWT estándar
    const header = {
        alg: 'HS256',
        typ: 'JWT'
    };

    // Payload con claims estándar
    const jwtPayload = {
        ...payload,
        iss: options.issuer || jwtConfig.issuer,
        aud: options.audience || jwtConfig.audience,
        iat: now,
        nbf: now,
        exp: now + ((options.expirationMinutes || jwtConfig.expirationMinutes) * 60)
    };

    // Codificar header y payload
    const encodedHeader = base64UrlEncode(JSON.stringify(header));
    const encodedPayload = base64UrlEncode(JSON.stringify(jwtPayload));

    // Crear mensaje a firmar
    const message = `${encodedHeader}.${encodedPayload}`;

    // Generar firma HMAC-SHA256
    const encodedSignature = hmacSha256(message, options.secretKey || jwtConfig.secretKey);

    // Retornar token completo
    return `${message}.${encodedSignature}`;
}

/**
 * Genera un token JWT para un usuario de prueba
 * 
 * @param {string} userId - ID del usuario (default: "load-test-user")
 * @param {string} email - Email del usuario (default: "loadtest@gateway.com")
 * @param {Array<string>} roles - Roles del usuario (default: ["User"])
 * @returns {string} Token JWT
 */
export function generateTestUserToken(
    userId = 'load-test-user',
    email = 'loadtest@gateway.com',
    roles = ['User']
) {
    const payload = {
        sub: userId,
        email: email,
        name: `Load Test User ${userId}`,
        role: roles[0],  // Claim individual para el middleware (espera string, no array)
        roles: roles,    // Claim array para compatibilidad
        jti: `${userId}-${Date.now()}` // Unique token ID
    };

    return generateJwtToken(payload);
}

/**
 * Genera un token JWT para un administrador de prueba
 * 
 * @param {string} adminId - ID del administrador (default: "load-test-admin")
 * @returns {string} Token JWT con rol Admin
 */
export function generateTestAdminToken(adminId = 'load-test-admin') {
    return generateTestUserToken(
        adminId,
        `admin-${adminId}@gateway.com`,
        ['User', 'Admin']
    );
}

/**
 * Genera un Correlation ID único para rastreo de requests
 * @returns {string} Correlation ID en formato UUID-like
 */
function generateCorrelationId() {
    const timestamp = Date.now().toString(36);
    const random = Math.random().toString(36).substring(2, 15);
    return `k6-load-${timestamp}-${random}`;
}

/**
 * Crea headers HTTP básicos con Correlation ID
 * 
 * @param {Object} additionalHeaders - Headers adicionales
 * @returns {Object} Headers HTTP con Correlation ID
 */
export function createBasicHeaders(additionalHeaders = {}) {
    return {
        'Content-Type': 'application/json',
        'Accept': 'application/json',
        'X-Correlation-Id': generateCorrelationId(),
        'User-Agent': 'Gateway-LoadTest-K6/1.0',
        ...additionalHeaders
    };
}

/**
 * Crea headers HTTP con autenticación Bearer
 * 
 * @param {string} token - Token JWT
 * @param {Object} additionalHeaders - Headers adicionales
 * @returns {Object} Headers HTTP con Authorization y Correlation ID
 */
export function createAuthHeaders(token, additionalHeaders = {}) {
    return {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json',
        'Accept': 'application/json',
        'X-Correlation-Id': generateCorrelationId(),
        'User-Agent': 'Gateway-LoadTest-K6/1.0',
        ...additionalHeaders
    };
}

/**
 * Genera token y headers en una sola llamada
 * 
 * @param {string} userId - ID del usuario (opcional)
 * @returns {Object} Objeto con token y headers
 */
export function generateAuthContext(userId) {
    const token = generateTestUserToken(userId);
    const headers = createAuthHeaders(token);

    return {
        token,
        headers
    };
}
