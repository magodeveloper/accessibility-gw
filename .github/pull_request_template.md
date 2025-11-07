## 📝 Descripción

<!-- Descripción clara y concisa de los cambios realizados -->

## 🔗 Issue Relacionado

<!-- ¿Este PR resuelve algún issue? -->

Fixes #(issue number)
Closes #(issue number)
Resolves #(issue number)

## 🎯 Tipo de Cambio

<!-- Marca las opciones relevantes -->

- [ ] 🐛 Bug fix (cambio que corrige un issue)
- [ ] ✨ New feature (cambio que agrega funcionalidad)
- [ ] 💥 Breaking change (fix o feature que causa que funcionalidad existente no funcione como antes)
- [ ] 📚 Documentation (cambios solo en documentación)
- [ ] 🎨 Style (formateo, missing semicolons, etc; no cambia lógica)
- [ ] ♻️ Refactor (cambio de código que no corrige bug ni agrega feature)
- [ ] ⚡ Performance (cambio que mejora performance)
- [ ] 🧪 Test (agregar tests faltantes o corregir tests existentes)
- [ ] 🔧 Chore (cambios en build process, dependencias, etc)
- [ ] 🔒 Security (cambios relacionados con seguridad)

## 📋 Cambios Realizados

<!-- Lista detallada de cambios -->

- [ ] Cambio 1
- [ ] Cambio 2
- [ ] Cambio 3

## 🧪 Testing Realizado

<!-- Describe qué testing has hecho -->

### Unit Tests

- [ ] Tests existentes pasan
- [ ] Nuevos tests agregados
- [ ] Coverage >= 90%

### Integration Tests

- [ ] Tests de integración pasan
- [ ] Nuevos tests de integración agregados (si aplica)

### Manual Testing

- [ ] Testing manual realizado
- [ ] Escenarios edge case verificados
- [ ] Testing en diferentes browsers (si aplica)

### Load Testing

- [ ] Load tests ejecutados (si aplica)
- [ ] No hay degradación de performance

**Comandos ejecutados:**

```bash
# Comandos de testing que ejecutaste
.\manage-tests.ps1 full
dotnet test --filter "FullyQualifiedName~MyFeature"
```

**Resultados:**

```
Tests: 435 passed
Coverage: 91.94%
```

## 📸 Screenshots

<!-- Si aplica, agregar screenshots de cambios UI o comportamiento -->

### Antes

<!-- Screenshot del estado anterior -->

### Después

<!-- Screenshot del nuevo estado -->

## 🔒 Security Considerations

<!-- ¿Este cambio tiene implicaciones de seguridad? -->

- [ ] No hay implicaciones de seguridad
- [ ] He revisado las implicaciones de seguridad
- [ ] Se agregaron validaciones de input
- [ ] Se actualizaron configuraciones de seguridad

## 📊 Performance Impact

<!-- ¿Cómo afecta este cambio al performance? -->

- [ ] No hay impacto en performance
- [ ] Mejora performance
- [ ] Posible impacto negativo (explicar abajo)

**Detalles:**

<!-- Si hay impacto, explicar mediciones -->

## 💥 Breaking Changes

<!-- ¿Este PR introduce breaking changes? -->

- [ ] No hay breaking changes
- [ ] Sí, hay breaking changes (detallar abajo)

**Detalles de Breaking Changes:**

<!-- Explicar qué se rompe y cómo migrar -->

**Migration Guide:**

```csharp
// Antes
OldMethod();

// Después
NewMethod();
```

## 📚 Documentation Updates

<!-- ¿Se actualizó la documentación? -->

- [ ] README.md actualizado
- [ ] API.md actualizado (si aplica)
- [ ] CHANGELOG.md actualizado
- [ ] Code comments agregados
- [ ] XML documentation agregada
- [ ] No requiere actualización de docs

## ✅ Checklist

<!-- Verifica que todos los items estén completados -->

### Code Quality

- [ ] Mi código sigue las guías de estilo del proyecto
- [ ] He realizado self-review de mi código
- [ ] He comentado código complejo o poco obvio
- [ ] Mis cambios no generan nuevos warnings
- [ ] He usado nombres descriptivos para variables y métodos
- [ ] He seguido principios SOLID

### Testing

- [ ] He agregado tests que prueban mi fix/feature
- [ ] Tests nuevos y existentes pasan localmente
- [ ] Coverage es >= 90%
- [ ] He probado en diferentes escenarios
- [ ] He probado edge cases

### Documentation

- [ ] He actualizado documentación relevante
- [ ] He agregado XML comments a public APIs
- [ ] He actualizado el CHANGELOG.md
- [ ] He agregado/actualizado ejemplos si es necesario

### Dependencies

- [ ] No agregué nuevas dependencias
- [ ] Las nuevas dependencias están justificadas (explicar abajo)
- [ ] He actualizado Directory.Packages.props si agregué paquetes
- [ ] He verificado vulnerabilidades de seguridad

### Git

- [ ] Mis commits son atómicos y descriptivos
- [ ] Mis commits siguen Conventional Commits
- [ ] He hecho rebase con master si es necesario
- [ ] No hay merge conflicts

### CI/CD

- [ ] Build pasa localmente
- [ ] Tests pasan en CI
- [ ] No hay warnings en CI
- [ ] Docker build funciona (si aplica)

## 🔄 Deployment Notes

<!-- ¿Hay algo especial a considerar para deployment? -->

- [ ] No requiere pasos especiales de deployment
- [ ] Requiere pasos especiales (detallar abajo)

**Deployment Steps:**

<!-- Si requiere pasos especiales -->

1.
2.
3.

**Rollback Plan:**

<!-- Cómo revertir si algo sale mal -->

## 💬 Notas Adicionales

<!-- Cualquier información adicional para reviewers -->

### Decisiones de Diseño

<!-- Explica decisiones importantes de diseño -->

### Known Issues

<!-- Issues conocidos que quedan pendientes -->

### Future Work

<!-- Trabajo futuro relacionado -->

## 📦 Dependencies Updated

<!-- Si actualizaste dependencias -->

| Package | Old Version | New Version | Reason  |
| ------- | ----------- | ----------- | ------- |
| Example | 1.0.0       | 2.0.0       | Bug fix |

## 🙏 Request for Review

<!-- Puntos específicos donde necesitas feedback -->

-
-

---

## 👥 Reviewers

<!-- Tag reviewers específicos si es necesario -->

@reviewer1 @reviewer2

---

**Por favor, revisa este PR y déjame saber si necesitas algún cambio.** 🙏

**¿Preguntas?** Comenta en el PR o contáctame directamente.
