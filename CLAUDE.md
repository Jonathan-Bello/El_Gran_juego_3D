# Lamento — reglas de desarrollo

## Stack del proyecto

- Motor: Unity 6000.6.5f1.
- Renderizado: Universal Render Pipeline 17.6.
- Lenguaje: C#.
- Entrada: Input System 1.20, con *Active Input Handling* configurado en `Both`.
- Paquetes disponibles: Timeline, AI Navigation, uGUI y Test Framework.
- Cinemachine se instala desde Package Manager cuando la tarea lo requiera.
- Repositorio: `Jonathan-Bello/El_Gran_juego_3D`; la rama de integración es `main` y Git LFS está configurado.

## Reglas obligatorias para agentes

1. Solo editar o crear archivos `.cs` y `.md`. No editar manualmente archivos `.unity`, `.prefab`, `.asset`, `.meta`, `.inputactions` ni `ProjectSettings`.
2. Describir todos los cambios de Unity Editor en pasos numerados para que una persona los aplique.
3. Mover o renombrar archivos únicamente desde la ventana **Project** de Unity, incluyendo sus `.meta`; nunca hacerlo desde el explorador de archivos.
4. Crear cada tarea en una rama `ia/<tarea>` y llevarla a `main` mediante un Pull Request revisado.
5. Usar C# compatible con Unity 6. Nombres de clases, métodos, variables y comentarios en español, alineados con el GDD.
6. Usar un `MonoBehaviour` por archivo y hacer que el nombre del archivo coincida con el de su clase.
7. Exponer valores ajustables con `[SerializeField] private`, `[Header]` y `[Tooltip]`; no usar números mágicos.
8. Leer entrada y actualizar temporizadores en `Update`; aplicar física en `FixedUpdate`.
9. Usar `Rigidbody.linearVelocity`; no usar la API obsoleta `Rigidbody.velocity`.
10. No añadir paquetes de terceros ni de la Asset Store sin aprobación explícita.
11. Entregar siempre archivos completos, no fragmentos.
12. Implementar solo el paso solicitado. Si el GDD y la tarea no especifican algo, pedir aclaración antes de inventarlo.

## Estructura del proyecto

```text
El_Gran_juego_3D/
├── docs/
│   └── GDD.md                 # fuente de verdad del diseño
├── AGENTS.md                  # reglas para Codex
├── CLAUDE.md                  # reglas para Claude Code
├── Assets/
│   ├── Scripts/
│   │   ├── Core/              # GameManager, eventos, pausa
│   │   ├── Player/            # Movimiento de Itzel, aliento, ruido
│   │   ├── Enemy/             # LloronaIA, AIDirector
│   │   ├── Audio/             # LamentoInvertido, mezclador
│   │   ├── World/             # Ofrendas, transiciones, escondites
│   │   ├── UI/                # HUD, menús, mapa
│   │   └── Save/              # Guardado y carga
│   ├── Scenes/                # Una escena por zona
│   ├── Prefabs/
│   ├── Materials/
│   ├── Models/                # .fbx (Git LFS)
│   ├── Audio/                 # .wav / .ogg (Git LFS)
│   ├── Settings/              # Assets de URP y Volumes
│   └── ScriptableObjects/     # Habilidades, datos de zonas
├── Packages/
└── ProjectSettings/
```

## Notas actuales

- `Assets/sus_control.cs` es el controlador temporal. Cuando se migre, moverlo a `Assets/Scripts/Player/` desde Unity y no borrar su `.meta` manualmente.
- El jugador de Lamento se mueve en X e Y; el eje Z queda bloqueado.
- El GDD en `docs/GDD.md` prevalece sobre cualquier decisión no especificada aquí.
