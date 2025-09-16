# 🚀 Load Testing Suite - Accessibility Gateway

Este directorio contiene una suite completa de pruebas de carga para el Accessibility Gateway utilizando [k6](https://k6.io/), una herramienta moderna de testing de rendimiento.

## 📋 Índice

- [Instalación y Configuración](#instalación-y-configuración)
- [Tipos de Pruebas](#tipos-de-pruebas)
- [Estructura del Proyecto](#estructura-del-proyecto)
- [Uso Rápido](#uso-rápido)
- [Configuración Avanzada](#configuración-avanzada)
- [Interpretación de Resultados](#interpretación-de-resultados)
- [Integración CI/CD](#integración-cicd)
- [Troubleshooting](#troubleshooting)

## 🛠️ Instalación y Configuración

### Prerrequisitos

1. **PowerShell 5.1+** (Windows) o **PowerShell Core 7+** (multiplataforma)
2. **k6** - Se puede instalar automáticamente con el script

### Instalación Automática

```powershell
# Navegar al directorio de pruebas de carga
cd src\tests\Gateway.Load

# Instalar k6 automáticamente
.\manage-load-tests.ps1 -Action install
```

### Instalación Manual de k6

#### Windows

```powershell
# Con winget (recomendado)
winget install k6

# Con Chocolatey
choco install k6

# Con Scoop
scoop install k6
```

#### macOS

```bash
# Con Homebrew
brew install k6
```

#### Linux

```bash
# Ubuntu/Debian
sudo gpg --no-default-keyring --keyring /usr/share/keyrings/k6-archive-keyring.gpg --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys C5AD17C747E3415A3642D57D77C6C491D6AC1D69
echo "deb [signed-by=/usr/share/keyrings/k6-archive-keyring.gpg] https://dl.k6.io/deb stable main" | sudo tee /etc/apt/sources.list.d/k6.list
sudo apt-get update
sudo apt-get install k6

# CentOS/RHEL/Fedora
sudo dnf install https://dl.k6.io/rpm/repo.rpm
sudo dnf install k6
```

## 🎯 Tipos de Pruebas

### Pruebas Estándar

| Tipo          | Descripción      | Usuarios | Duración | Propósito                            |
| ------------- | ---------------- | -------- | -------- | ------------------------------------ |
| **smoke**     | Prueba de humo   | 1        | 30s      | Verificación básica de funcionalidad |
| **load**      | Carga normal     | 10       | 5m       | Rendimiento bajo carga típica        |
| **stress**    | Prueba de estrés | 50       | 10m      | Comportamiento bajo alta carga       |
| **spike**     | Picos de carga   | 100      | 5m       | Respuesta a incrementos súbitos      |
| **endurance** | Resistencia      | 20       | 30m      | Estabilidad en períodos largos       |

### Pruebas de Usuarios Concurrentes (NUEVAS)

| Tipo               | Usuarios | Duración | Nivel      | Descripción                         |
| ------------------ | -------- | -------- | ---------- | ----------------------------------- |
| **concurrent-20**  | 20       | 5m       | 🟢 Ligero  | Carga básica, ideal para desarrollo |
| **concurrent-50**  | 50       | 5m       | 🟡 Medio   | Patrones de uso realistas           |
| **concurrent-100** | 100      | 10m      | 🟠 Alto    | Carga intensiva con patrones mixtos |
| **concurrent-500** | 500      | 15m      | 🔴 Extremo | Prueba de límites del sistema       |

## 📁 Estructura del Proyecto

```
src/tests/Gateway.Load/
├── scenarios/                 # Scripts de pruebas k6
│   ├── smoke-test.js         # Prueba de humo
│   ├── load-test.js          # Prueba de carga estándar
│   ├── stress-test.js        # Prueba de estrés
│   ├── spike-test.js         # Prueba de picos
│   ├── endurance-test.js     # Prueba de resistencia
│   ├── concurrent-users-20.js   # 20 usuarios concurrentes
│   ├── concurrent-users-50.js   # 50 usuarios concurrentes
│   ├── concurrent-users-100.js  # 100 usuarios concurrentes
│   └── concurrent-users-500.js  # 500 usuarios concurrentes
├── utils/                    # Utilidades compartidas
│   ├── config.js            # Configuración centralizada
│   └── metrics.js           # Métricas personalizadas
├── data/                     # Datos de prueba
│   └── README.md            # Documentación de datos
├── results/                  # Resultados de ejecución
│   └── README.md            # Documentación de resultados
├── manage-load-tests.ps1    # Script principal de gestión
└── README.md               # Esta documentación
```

## 🚀 Uso Rápido

### Ejecutar una Prueba Individual

```powershell
# Prueba básica de humo
.\manage-load-tests.ps1 -Action smoke

# Prueba de 20 usuarios concurrentes
.\manage-load-tests.ps1 -Action concurrent-20

# Prueba de 50 usuarios con configuración personalizada
.\manage-load-tests.ps1 -Action concurrent-50 -BaseUrl "https://gateway.example.com" -Verbose

# Prueba de 100 usuarios con duración personalizada
.\manage-load-tests.ps1 -Action concurrent-100 -Duration "15m" -GenerateReport

# ⚠️ Prueba extrema (500 usuarios) - ¡Usar con precaución!
.\manage-load-tests.ps1 -Action concurrent-500
```

### Ejecutar Suite Completa

```powershell
# Ejecutar todas las pruebas en secuencia (excepto la extrema)
.\manage-load-tests.ps1 -Action all -GenerateReport

# La prueba extrema (500 usuarios) se preguntará al final
```

### Comandos de Gestión

```powershell
# Limpiar resultados anteriores
.\manage-load-tests.ps1 -Action clean

# Mostrar ayuda detallada
.\manage-load-tests.ps1 -Action help
```

## ⚙️ Configuración Avanzada

### Variables de Entorno

```powershell
# Configurar URL del Gateway
$env:BASE_URL = "https://my-gateway.com"

# Configurar usuarios específicos
$env:USERS = "25"

# Configurar duración específica
$env:DURATION = "10m"

# Habilitar logging verbose
$env:VERBOSE = "true"
```

### Parámetros del Script

```powershell
.\manage-load-tests.ps1 `
    -Action concurrent-50 `
    -BaseUrl "https://gateway.production.com" `
    -Users 75 `
    -Duration "8m" `
    -OutputDir "custom-results" `
    -Verbose `
    -GenerateReport `
    -SkipHealthCheck
```

### Configuración Personalizada

Edite `utils/config.js` para personalizar:

- **Endpoints**: URLs de los servicios
- **Thresholds**: Límites de rendimiento
- **Headers**: Headers HTTP personalizados
- **Datos de prueba**: Generación de datos sintéticos

## 📊 Interpretación de Resultados

### Métricas Clave

#### Métricas HTTP Estándar

- **http_req_duration**: Tiempo de respuesta de requests

  - `p(95) < 300ms` ✅ Excelente
  - `p(95) < 500ms` ✅ Bueno
  - `p(95) < 1000ms` ⚠️ Aceptable
  - `p(95) > 1000ms` ❌ Necesita optimización

- **http_req_failed**: Tasa de errores

  - `< 0.1%` ✅ Excelente
  - `< 1%` ✅ Bueno
  - `< 5%` ⚠️ Aceptable
  - `> 5%` ❌ Problemático

- **http_reqs**: Requests por segundo (RPS)
  - Indica el throughput del sistema

#### Métricas Personalizadas del Gateway

- **gateway_error_rate**: Tasa de errores específica del Gateway
- **gateway_duration**: Tiempo de respuesta del Gateway
- **service_error_rate**: Errores en servicios downstream
- **timeout_rate**: Tasa de timeouts

### Thresholds por Nivel de Carga

#### Carga Ligera (20 usuarios)

```javascript
thresholds: {
  'http_req_duration': ['p(95)<300'],
  'http_req_failed': ['rate<0.005'],
  'gateway_error_rate': ['rate<0.005']
}
```

#### Carga Media (50 usuarios)

```javascript
thresholds: {
  'http_req_duration': ['p(95)<500'],
  'http_req_failed': ['rate<0.01'],
  'gateway_error_rate': ['rate<0.01']
}
```

#### Carga Alta (100 usuarios)

```javascript
thresholds: {
  'http_req_duration': ['p(95)<800'],
  'http_req_failed': ['rate<0.02'],
  'gateway_error_rate': ['rate<0.02']
}
```

#### Carga Extrema (500 usuarios)

```javascript
thresholds: {
  'http_req_duration': ['p(95)<1500'],
  'http_req_failed': ['rate<0.05'],
  'gateway_error_rate': ['rate<0.05']
}
```

### Análisis de Archivos de Resultados

Los resultados se guardan en formato JSON en la carpeta `results/`:

```powershell
# Ver resumen rápido de un resultado
k6 summary results/concurrent-50-20241225-143022.json

# Analizar con herramientas externas
# - Grafana + InfluxDB
# - k6 Cloud
# - Datadog
# - New Relic
```

## 🔄 Integración CI/CD

### GitHub Actions

```yaml
name: Gateway Load Tests

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]
  schedule:
    - cron: '0 2 * * *' # Diario a las 2 AM

jobs:
  load-tests:
    runs-on: windows-latest

    steps:
      - uses: actions/checkout@v4

      - name: Setup Gateway
        run: |
          # Iniciar Gateway en modo test
          docker-compose -f docker-compose.test.yml up -d

      - name: Install k6
        run: |
          cd src\tests\Gateway.Load
          .\manage-load-tests.ps1 -Action install

      - name: Run Smoke Tests
        run: |
          cd src\tests\Gateway.Load
          .\manage-load-tests.ps1 -Action smoke -BaseUrl "http://localhost:5000"

      - name: Run Concurrent User Tests
        run: |
          cd src\tests\Gateway.Load
          .\manage-load-tests.ps1 -Action concurrent-20 -GenerateReport

      - name: Upload Results
        uses: actions/upload-artifact@v4
        with:
          name: load-test-results
          path: src/tests/Gateway.Load/results/
```

### Azure DevOps

```yaml
trigger:
  branches:
    include:
      - main
      - develop

pool:
  vmImage: 'windows-latest'

stages:
  - stage: LoadTests
    displayName: 'Gateway Load Tests'
    jobs:
      - job: BasicLoadTests
        displayName: 'Basic Load Testing'
        steps:
          - powershell: |
              cd src\tests\Gateway.Load
              .\manage-load-tests.ps1 -Action install
            displayName: 'Install k6'

          - powershell: |
              cd src\tests\Gateway.Load
              .\manage-load-tests.ps1 -Action smoke
            displayName: 'Smoke Test'

          - powershell: |
              cd src\tests\Gateway.Load
              .\manage-load-tests.ps1 -Action concurrent-20 -GenerateReport
            displayName: 'Concurrent Users Test'

          - task: PublishTestResults@2
            inputs:
              testResultsFormat: 'JUnit'
              testResultsFiles: 'src/tests/Gateway.Load/results/*.xml'
              testRunTitle: 'Gateway Load Tests'
```

## 🔧 Troubleshooting

### Problemas Comunes

#### 1. Gateway No Disponible

```
❌ Gateway no está disponible en http://localhost:5000
```

**Soluciones:**

- Verificar que el Gateway esté ejecutándose
- Comprobar el puerto correcto
- Usar `-SkipHealthCheck` para omitir verificación
- Verificar firewall/antivirus

#### 2. k6 No Encontrado

```
❌ k6 no está instalado
```

**Soluciones:**

```powershell
# Instalar automáticamente
.\manage-load-tests.ps1 -Action install

# O instalar manualmente
winget install k6
```

#### 3. Errores de Memoria en Tests Extremos

**Soluciones:**

- Reducir el número de usuarios virtuales
- Usar `discardResponseBodies: true` en options
- Incrementar memoria disponible del sistema
- Ejecutar en máquina más potente

#### 4. Timeouts Frecuentes

**Configuraciones en `utils/config.js`:**

```javascript
export const config = {
  timeout: '30s', // Incrementar timeout
  noConnectionReuse: false, // Reutilizar conexiones
  // ...
};
```

#### 5. Rate Limiting

Si el Gateway tiene rate limiting:

```javascript
// En scenarios, incrementar sleeps
sleep(Math.random() * 5 + 2); // 2-7 segundos
```

### Logs y Debugging

```powershell
# Ejecutar con logging verbose
.\manage-load-tests.ps1 -Action concurrent-20 -Verbose

# Ver logs del sistema
Get-Content src\tests\Gateway.Load\results\load-tests.log -Tail 50

# Analizar métricas específicas
k6 run --summary-export results/summary.json scenarios/concurrent-20.js
```

### Optimización de Rendimiento

#### Para el Sistema de Pruebas

1. **Máquina dedicada**: Ejecutar en máquina separada del Gateway
2. **Recursos suficientes**: Mínimo 8GB RAM para tests de 500 usuarios
3. **Red estable**: Conexión de baja latencia al Gateway

#### Para el Gateway

1. **Monitoring**: Usar métricas durante las pruebas
2. **Profiling**: Activar profilers durante load tests
3. **Logs**: Configurar logging apropiado para análisis

## 📈 Métricas Avanzadas

### Integración con Monitoring

#### Prometheus + Grafana

```javascript
// En scenarios, enviar métricas custom
import { Counter } from 'k6/metrics';

const businessMetric = new Counter('business_transactions');

export default function () {
  // ... hacer request
  businessMetric.add(1, { operation: 'user_creation' });
}
```

#### InfluxDB

```powershell
# Ejecutar con output a InfluxDB
k6 run --out influxdb=http://localhost:8086/k6 scenarios/concurrent-50.js
```

### Alertas Automáticas

```javascript
// En thresholds
export let options = {
  thresholds: {
    http_req_duration: ['p(95)<500', { threshold: 'p(95)<1000', abortOnFail: true }],
    http_req_failed: [{ threshold: 'rate<0.01', abortOnFail: true }],
  },
};
```

## 🎯 Roadmap

### Próximas Mejoras

- [ ] **Dashboard en tiempo real**: Grafana dashboard específico
- [ ] **Tests de API específicas**: Escenarios por endpoint
- [ ] **Tests de seguridad**: Integración con OWASP ZAP
- [ ] **Performance budgets**: Límites automáticos en CI/CD
- [ ] **Chaos testing**: Integración con Chaos Monkey
- [ ] **Multi-region testing**: Tests distribuidos geográficamente

### Contribuir

1. Fork del repositorio
2. Crear branch para nueva feature
3. Implementar mejoras en `scenarios/` o `utils/`
4. Actualizar documentación
5. Crear Pull Request

## 📞 Soporte

Para problemas o preguntas:

1. **Issues**: Crear issue en el repositorio
2. **Documentación**: Revisar esta documentación
3. **k6 Docs**: [Documentación oficial de k6](https://k6.io/docs/)
4. **Gateway Docs**: Documentación del Accessibility Gateway

---

**🚀 ¡Happy Load Testing!**

_Recuerda: Los tests de carga son una herramienta para mejorar el rendimiento, no para romper sistemas en producción. Usa tests extremos solo en entornos controlados._
