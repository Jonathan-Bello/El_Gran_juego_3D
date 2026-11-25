# Créditos de recursos externos

Todos los recursos de terceros de *Lamento* deben estar aquí. Solo se aceptan recursos gratuitos con una licencia que permita redistribuir los archivos en este repositorio (de preferencia **CC0**).

| Recurso | Autor | URL | Licencia | Descargado | Ubicación en el proyecto |
|---|---|---|---|---|---|
| Universal Base Characters (Standard): `Superhero_Female_FullBody.fbx`, `Hair_Long.fbx`, `Hair_Buns.fbx` y texturas de piel, ojos y cabello | Quaternius | https://quaternius.itch.io/universal-base-characters | CC0 1.0 | 2026-11-25 | `Assets/Models/Itzel/` y `Assets/Models/Itzel/Texturas/` |
| Modular Character Outfits – Fantasy (Standard): `Female_Peasant.fbx` y su mapa normal | Quaternius | https://quaternius.itch.io/modular-character-outfits-fantasy | CC0 1.0 | 2026-11-25 | `Assets/Models/Itzel/` y `Assets/Models/Itzel/Texturas/` |
| Universal Animation Library (Standard): `UAL1_Standard.fbx` (sin root motion) | Quaternius | https://quaternius.itch.io/universal-animation-library | CC0 1.0 | 2026-11-25 | `Assets/Models/Itzel/Animaciones/` |
| Nature Kit 2.1: 30 modelos FBX (rocas, árboles, troncos, carrizos/bambú, pasto, puente y camino de madera, cerca, canoa, lirios) | Kenney | https://kenney.nl/assets/nature-kit | CC0 1.0 | 2026-11-25 | `Assets/Models/Orilla/` (incluye `Kenney_License.txt`) |
| Ground 106 (1K-JPG): color, normal GL y rugosidad | ambientCG (Lennart Demes) | https://ambientcg.com/view?id=Ground106 | CC0 1.0 | 2026-11-25 | `Assets/Textures/Orilla/` |

## Modificaciones hechas a los recursos

- Las texturas de 4096 px se redujeron a 2048 px para que el repositorio pese menos.
- `T_Peasant_Itzel_BaseColor.png` es una **obra derivada** de `T_Peasant_BaseColor.png`: se pasó a escala de grises y se reasignó a la paleta del GDD (de *Luna fría* `#6FA3C7` a *Aliento* `#E6F3FA`), para que el atuendo de Itzel sea claro y destaque sobre los fondos oscuros (GDD 9.3).
- Los mapas normales provienen de las carpetas `Normals Unity - Godot` (convención OpenGL que usa Unity).
- Las texturas JPG de ambientCG se convirtieron a PNG sin cambiar su contenido. Los modelos de Kenney se recolorean en Unity con materiales de la paleta azul (no se modifica el FBX).
- Solo se copiaron los archivos que usa el proyecto; los ZIP originales tienen muchos más modelos.

> CC0 no obliga a dar crédito, pero lo damos por cortesía. Si te gustan estos paquetes, considera apoyar a [Quaternius en Patreon](https://www.patreon.com/quaternius).
