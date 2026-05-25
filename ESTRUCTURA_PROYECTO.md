# ANÁLISIS DE ESTRUCTURA - PROYECTO BASKETBALL GAME EN UNITY

## 📋 RESUMEN EJECUTIVO

El proyecto es un juego de baloncesto en Unity que implementa un **sistema de cámara en tercera persona con anti-colisión y modo de apuntado**. Está diseñado para enseñar mecánicas avanzadas de cámara en videojuegos.

---

## 🏗️ ESTRUCTURA DEL PROYECTO

### Carpetas Principales

```
Assets/
├── Scripts/
│   ├── PlayerController.cs (base genérica)
│   ├── Basketball/ (lógica específica del juego)
│   └── Setup/ (configuración de escena)
├── Scenes/
│   ├── BasketballScene.unity (escena principal)
│   └── SampleScene.unity
├── Prefabs/
│   └── Ball.prefab (pelota)
├── Materials/
│   └── [Materiales visuales]
└── Animations/
    └── [Controladores de animación]
```

---

## 🎮 COMPONENTES PRINCIPALES DEL SISTEMA

### 1. **PlayerController.cs** (Control Base del Personaje)

**Ubicación:** `Assets/Scripts/PlayerController.cs`

**Responsabilidades:**
- Lectura de input (teclas y ratón)
- Movimiento básico del jugador
- Gestión de gravedad y saltos
- Sistema de Input Actions (configurable)

**Variables Clave:**
```csharp
[SerializeField] private float moveSpeed = 5f;
[SerializeField] private float sprintMultiplier = 2f;
[SerializeField] private float jumpHeight = 1.5f;
[SerializeField] private Transform cameraTransform;
[SerializeField] private float lookSensitivity = 0.1f;
```

**Acciones de Input:**
- Move: Movimiento WASD
- Look: Rotación del ratón
- Jump: Saltar
- Sprint: Correr
- Crouch: Agacharse
- Attack: Atacar
- Interact: Interactuar

---

### 2. **BasketballPlayer.cs** (Lógica Específica del Baloncesto)

**Ubicación:** `Assets/Scripts/Basketball/BasketballPlayer.cs`

**Responsabilidades:**
- Control específico del jugador de baloncesto
- Sistema de disparo/lanzamiento
- Detección de zonas de puntuación (2 puntos, 3 puntos, etc.)
- Trayectoria predictiva de la pelota
- Asistencia de apuntado (aim assist)
- Modo de apuntado (aiming) para visualizar trayectoria

**Variables Clave:**
```csharp
[SerializeField] private float moveSpeed = 5f;
[SerializeField] private Transform shotPoint; // Punto de lanzamiento
[SerializeField] private GameObject ballPrefab;
[SerializeField] private Transform hoopTarget; // Posición del aro
[SerializeField, Range(0f, 1f)] private float aimAssist = 0.6f; // Corrección lateral
[SerializeField] private float shotAngle = 58f; // Ángulo óptimo
[SerializeField] private TrajectoryRenderer trajectoryRenderer;
[SerializeField] private ShotZoneDetector zoneDetector;
```

**Modos de Operación:**
- **Modo Libre:** Caminar/correr normalmente
- **Modo Apuntado:** Presionar Sprint para ver la trayectoria
- **Modo Disparo:** Presionar Attack para lanzar

**API Pública:**
```csharp
public bool IsAiming => _isAiming; // Consultado por la cámara
```

---

### 3. **ThirdPersonCamera.cs** (Sistema de Cámara Orbital)

**Ubicación:** `Assets/Scripts/Basketball/ThirdPersonCamera.cs`

**Responsabilidades:**
- Cámara orbital alrededor del jugador
- Seguimiento suave del jugador
- Transición automática entre modo normal y modo apuntado
- Control mediante ratón (yaw/pitch)
- Anti-colisión integrada

**Arquitectura de Configuración:**
```
Jerarquía de Escena:
├── Player (Controlador del personaje)
├── CameraRig (GameObject vacío)
│   └── Main Camera (ThirdPersonCamera + CameraCollisionHandler)
```

**⚠️ IMPORTANTE:** El CameraRig NO es hijo del Player. La cámara sigue al jugador mediante código, no por jerarquía.

**Variables de Configuración:**

#### Posicionamiento:
```csharp
[SerializeField] private float normalDistance = 5f;      // Distancia en modo libre
[SerializeField] private float pivotHeight = 2f;         // Altura del punto de rotación
[SerializeField] private float aimDistance = 3f;         // Distancia al apuntar
[SerializeField] private Vector3 aimShoulderOffset = new Vector3(0.6f, 0.15f, 0f);
[SerializeField] private float aimTransitionSpeed = 8f;  // Velocidad de transición
```

#### Control Visual:
```csharp
[SerializeField] private float minPitch = -20f;          // Límite mirando hacia abajo
[SerializeField] private float maxPitch = 60f;           // Límite mirando hacia arriba
[SerializeField] private float mouseSensitivity = 0.18f; // Sensibilidad del ratón
[SerializeField] private bool invertY = false;           // Invertir eje Y
```

#### Suavizado:
```csharp
[SerializeField] private float positionSmoothTime = 0.08f;  // Suavizado de posición
[SerializeField] private float rotationSmoothSpeed = 12f;   // Suavizado de rotación
[SerializeField] private float followSmoothTime = 0.05f;    // Seguimiento del jugador
```

**Métodos Clave:**
1. `LeerInputMouse()` - Captura delta del ratón
2. `ActualizarDistancia()` - Transición normal ↔ apuntado
3. `ActualizarPivote()` - Sigue al jugador suavemente
4. `AplicarTransformCamara()` - Calcula la posición final segura

**Comportamiento de Apuntado:**
- Lee `basketballPlayer.IsAiming`
- Si es true: distancia → aimDistance, aplica shoulder offset
- Si es false: distancia → normalDistance, offset → 0
- Transición suave con `aimTransitionSpeed`

---

### 4. **CameraCollisionHandler.cs** (Anti-Colisión de Cámara)

**Ubicación:** `Assets/Scripts/Basketball/CameraCollisionHandler.cs`

**Responsabilidades:**
- Detectar obstrucciones entre el jugador y la cámara
- Evitar que la cámara atraviese paredes
- Acercar automáticamente la cámara cuando hay obstáculos
- Alejar suavemente cuando se despeja

**Técnica:**
- Usa `Physics.SphereCast` para detectar colisiones
- Radio: 0.3 metros (esfera de seguridad)
- Solo colisiona con layer `CameraObstacle`

**Variables de Configuración:**
```csharp
[SerializeField] private float sphereRadius = 0.3f;      // Radio de detección
[SerializeField] private float surfaceOffset = 0.12f;    // Separación de paredes
[SerializeField] private float minimumDistance = 0.8f;   // Zoom mínimo
[SerializeField] private LayerMask obstacleLayerMask;    // Qué colisiona
[SerializeField] private float approachSpeed = 20f;      // Velocidad acercarse
[SerializeField] private float recoverSpeed = 6f;        // Velocidad alejarse
```

**Método Principal:**
```csharp
public Vector3 GetSafePosition(Vector3 pivot, Vector3 desiredPos)
{
    // 1. Lanza SphereCast desde el pivote (cabeza del jugador)
    // 2. Si hay obstáculo, recorta la distancia
    // 3. Suaviza la transición asimétricamente
    // 4. Devuelve la posición segura final
}
```

**Características Especiales:**
- **Acercamiento rápido** (20 fps): Evita clipping instantáneo en paredes
- **Alejamiento lento** (6 fps): Recuperación suave cuando se despeja
- **Distancia mínima**: Previene zoom extremo de 0.8 metros

---

## 📊 FLUJO DE EJECUCIÓN (POR FRAME)

### Orden en LateUpdate():

```
1. ThirdPersonCamera.LateUpdate()
   ├─ LeerInputMouse()          → Actualiza _yaw y _pitch
   ├─ ActualizarDistancia()     → Calcula normalDistance vs aimDistance
   ├─ ActualizarPivote()        → Mueve el punto de rotación
   └─ AplicarTransformCamara()  → Posiciona la cámara
       ├─ Calcula posición ideal
       ├─ Llama a CameraCollisionHandler.GetSafePosition()
       └─ Aplica la posición y rotación finales

2. CameraCollisionHandler.GetSafePosition()
   ├─ Physics.SphereCast()      → Detecta obstáculos
   ├─ Si colisión: recorta distancia
   └─ Suaviza transición (approach/recover)
```

---

## 🔌 REFERENCIAS E INYECCIONES

### En el Inspector se asignan:

**ThirdPersonCamera necesita:**
```
- player (Transform)              → Raíz del jugador
- basketballPlayer (BasketballPlayer) → Para leer IsAiming
```

**BasketballPlayer necesita:**
```
- shotPoint (Transform)           → Punto de lanzamiento
- ballPrefab (GameObject)         → Pelota a instanciar
- hoopTarget (Transform)          → Centro del aro
- trajectoryRenderer (TrajectoryRenderer)
- zoneDetector (ShotZoneDetector)
- goalDetector (GoalDetector)
- thirdPersonCamera (ThirdPersonCamera) → Para movimiento relativo
```

**CameraCollisionHandler necesita:**
```
- obstacleLayerMask              → Layer "CameraObstacle"
```

---

## 🎯 PASOS PARA CREAR LA ESCENA DESDE CERO

### Paso 1: Crear el Controlador del Personaje

1. Crear un GameObject vacío llamado **"Player"**
2. Agregar componente `CharacterController`
3. Adjuntar script `BasketballPlayer.cs`
4. Crear modelo visual (cubo temporal o modelo 3D)
5. Asignar este modelo como hijo de Player

**Configuración CharacterController:**
- Center: (0, 1, 0)
- Radius: 0.3
- Height: 2

### Paso 2: Crear el Sistema de Cámara

1. Crear GameObject vacío llamado **"CameraRig"** (en la raíz, NO hijo de Player)
2. Mover Main Camera como hijo de CameraRig
3. Adjuntar script `ThirdPersonCamera.cs` a Main Camera
4. Adjuntar script `CameraCollisionHandler.cs` a Main Camera

**Jerarquía Final:**
```
Scene
├── Player
│   ├── [Modelo Visual]
│   └── CharacterController
├── CameraRig (vacío)
│   └── Main Camera
│       ├── Camera (componente)
│       ├── ThirdPersonCamera (script)
│       └── CameraCollisionHandler (script)
```

### Paso 3: Configurar Referencias en el Inspector

**Main Camera Inspector:**
- `ThirdPersonCamera.player` → Player transform
- `ThirdPersonCamera.basketballPlayer` → Script BasketballPlayer del Player
- `CameraCollisionHandler.obstacleLayerMask` → Layer "CameraObstacle"

**Player Inspector:**
- `BasketballPlayer.shotPoint` → Transform donde sale la pelota
- `BasketballPlayer.hoopTarget` → Centro del aro
- `BasketballPlayer.thirdPersonCamera` → ThirdPersonCamera de Main Camera

### Paso 4: Configurar Layers

1. Crear Layer `"CameraObstacle"`
2. Asignar a:
   - Paredes
   - Suelo
   - Poste de canasta
   - Tablero
   - Cualquier obstáculo que deba bloquear la cámara

**IMPORTANTE:** El Player NO debe estar en este layer.

### Paso 5: Crear Geometría de Escena

- Suelo (plano)
- Canasta (aro + tablero)
- Paredes (para probar anti-colisión)
- Obstáculos varios

Todos en layer `"CameraObstacle"` excepto el Player.

---

## ⚙️ CONFIGURACIÓN RECOMENDADA PARA PRUEBAS

### ThirdPersonCamera:
```
Normal Distance:          5
Pivot Height:            2
Aim Distance:            3
Aim Shoulder Offset:     (0.6, 0.15, 0)
Min Pitch:              -20
Max Pitch:               60
Mouse Sensitivity:       0.18
Position Smooth Time:    0.08
Rotation Smooth Speed:   12
Follow Smooth Time:      0.05
```

### CameraCollisionHandler:
```
Sphere Radius:          0.3
Surface Offset:         0.12
Minimum Distance:       0.8
Approach Speed:         20
Recover Speed:          6
```

### BasketballPlayer:
```
Move Speed:            5
Jump Height:           1.5
Shot Angle:           58
Aim Assist:          0.6
```

---

## 🐛 PROBLEMAS COMUNES Y SOLUCIONES

### Problema: La cámara se pega a la pared
**Solución:** Aumentar `surfaceOffset` en CameraCollisionHandler (ej: 0.2)

### Problema: La cámara está muy lenta siguiendo
**Solución:** Disminuir `followSmoothTime` en ThirdPersonCamera (ej: 0.03)

### Problema: El ratón está muy sensible
**Solución:** Disminuir `mouseSensitivity` en ThirdPersonCamera (ej: 0.1)

### Problema: El jugador no se ve cuando apunta
**Solución:** Verificar `aimShoulderOffset` y `aimDistance` son correctos

### Problema: La cámara atraviesa geometría
**Solución:** 
- Verificar que la geometría está en layer `CameraObstacle`
- Aumentar `sphereRadius` en CameraCollisionHandler

---

## 🎬 ESCENAS ADICIONALES SEGÚN INTEGRANTES DEL GRUPO

### Estructura Recomendada:

```
Assets/Scenes/
├── 01_Escena1_ControladorCamara.unity (Escena base con explicación)
├── 02_Escena2_SnipereliteStyle.unity   (Integrante 1)
├── 03_Escena3_GodOfWarStyle.unity      (Integrante 2)
├── 04_Escena4_LastOfUsStyle.unity      (Integrante 3)
├── 05_Escena5_GTAStyle.unity           (Integrante 4)
└── 06_Escena6_ForzaStyle.unity         (Integrante 5)
```

### Cada escena debe incluir:

1. **Análisis de Diseño** (documento en escena)
   - Qué juego inspiró el sistema
   - Cómo afecta la cámara a la jugabilidad
   - Mecánicas implementadas

2. **Implementación del Sistema**
   - Scripts específicos de cámara
   - Transiciones entre modos
   - Efectos visuales/cinemáticos

3. **Escenario de Prueba**
   - Geometría para probar la cámara
   - Mecánicas del juego asociadas

---

## 📝 REFERENCIAS DE CÓDIGO IMPORTANTE

### Event Loop Completo en ThirdPersonCamera:

```csharp
private void LateUpdate()
{
    if (player == null) return;
    
    LeerInputMouse();        // 1. Captura input
    ActualizarDistancia();   // 2. Calcula distancia (normal vs aim)
    ActualizarPivote();      // 3. Mueve punto de rotación
    AplicarTransformCamara(); // 4. Posiciona y rota la cámara
}
```

### Integración Anti-Colisión:

```csharp
// Dentro de AplicarTransformCamara():
Vector3 desiredPosition = _pivotSmoothed + quaternion * offset;
Vector3 safePosition = _collision.GetSafePosition(_pivotSmoothed, desiredPosition);
transform.position = Vector3.SmoothDamp(
    transform.position, 
    safePosition, 
    ref _positionVelocity, 
    positionSmoothTime
);
```

### Transición de Apuntado:

```csharp
private void ActualizarDistancia()
{
    bool isAiming = basketballPlayer != null && basketballPlayer.IsAiming;
    float targetDistance = isAiming ? aimDistance : normalDistance;
    _currentDistance = Mathf.Lerp(_currentDistance, targetDistance, 
        Time.deltaTime * aimTransitionSpeed);
}
```

---

## ✅ CHECKLIST PARA CREAR UNA NUEVA ESCENA

- [ ] Crear carpeta Scene_[Nombre]
- [ ] Crear escena.unity
- [ ] Crear GameObject Player con CharacterController
- [ ] Crear CameraRig vacío
- [ ] Adjuntar Main Camera a CameraRig
- [ ] Agregar ThirdPersonCamera.cs a Main Camera
- [ ] Agregar CameraCollisionHandler.cs a Main Camera
- [ ] Asignar referencias en Inspector
- [ ] Crear Layer "CameraObstacle"
- [ ] Asignar geometría al layer correcto
- [ ] Probar anti-colisión con pared cerca
- [ ] Documentar sistema de cámara en PDF

---

## 📚 DOCUMENTACIÓN RECOMENDADA EN PDF

### Sección 1: Escena Base
1. Introducción a sistemas de cámara en videojuegos
2. Paso a paso: Crear controlador del personaje
3. Paso a paso: Implementar cámara orbital
4. Sistema anti-colisión explicado
5. Pruebas y ajustes

### Secciones Adicionales (1 por integrante)
1. Inspiración: [Juego elegido]
2. Análisis: Cómo la cámara afecta la jugabilidad
3. Implementación: Modificaciones al código
4. Resultados: Comportamiento final
5. Conclusiones: Lecciones aprendidas

---

**Documento Generado:** Análisis de Estructura del Proyecto Basketball Game
**Propósito:** Servir como guía para crear documentación PDF detallada
**Versión:** 1.0
