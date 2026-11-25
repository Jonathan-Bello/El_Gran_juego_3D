# Zona: La Orilla (nivel tutorial)

> Fuente: GDD 8.1 (La Orilla: ribera con carrizos, muelle roto), 8.2 (reglas de diseño), 8.3 (profundidad 2.5D), 9.2 (paleta) y 5.2 (movimiento).
> Escena: `Assets/Scenes/Zona_Orilla.unity`, generada por el menú **Lamento > Construir nivel La Orilla**.

## Objetivo

Enseñar, sin texto y en este orden, las mecánicas base: **caminar → correr → saltar un hueco → salto alto a un muelle → agacharse bajo un tronco → llegar a la ofrenda**. Es de noche, hay neblina y el río está a la izquierda del jugador (fondo). La Llorona todavía no aparece.

## Números que limitan el diseño

Se calcularon con los valores actuales de `ItzelController` (fuerza de salto 7 m/s, gravedad de caída ×2.5):

| Medida | Valor aproximado | Uso en el nivel |
|---|---|---|
| Altura máxima de salto (botón sostenido) | ~2.5 m | El muelle está a **1.8 m**: se necesita mantener el botón. |
| Distancia de salto caminando | ~4 m | El primer hueco mide **3 m**: se cruza caminando. |
| Distancia de salto corriendo | ~7.5 m | El hueco del muelle roto mide **2.5 m**, para practicar con margen. |
| Altura de Itzel de pie / agachada | 1.2 m / 0.66 m | El tronco deja **0.9 m** libres: solo se pasa agachada. |

## Mapa (vista lateral, X de 0 a 110 m, 1 carácter ≈ 2 m)

```text
 Leyenda:  I = inicio   ⛵ = canoa (obstáculo)   ░ = carrizos frontales (escondite)   * = luz ámbar de ruta
           ≈ = agua     ═ = muelle (1.8 m)   ▄ = tronco bajo   ¥ = ofrenda   ▓ = suelo de lodo

 FONDO (Z +5 a +20):  luna, colinas, árboles secos, río ≈≈≈ a todo lo largo, canoa varada

                                                        *            *
                                                   ═══════ ══════════         (1.8 m)
   *        *         *     ⛵    *          *      ║  ║  ≈ ║   ║   ║▒▒                ▄▄▄       *   ¥
 I                                                 ║  ║  ≈ ║   ║   ║  ▒▒              ▄▄▄ (0.9 m)    ┃
 ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓≈≈≈▓▓▓▓≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈≈▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
 ░░░         ░░░                                       ░░░                        ░░░
 |── 1. Despertar ──|── 2. La Ribera ──|─ 3. Hueco ─|───── 4. Muelle roto ─────|── 5. Tronco ──|─ Ofrenda ─|
 0                 20                 40   44 47   58     68 70.5           80 83     88  93    102 106 110

   ▒ = rocas-escalón para bajar del muelle (X 80–83)
   ≈ bajo el muelle = agua con zona de muerte (caer por el tablón roto te regresa al inicio)

 FRENTE (Z −2 a −6):  matas de carrizos ░ que tapan parte de la vista
```

## Secciones

| # | Sección | X (m) | Enseña | Cómo lo enseña | Regla 8.2 |
|---|---|---|---|---|---|
| 1 | **Despertar** | 0–20 | Caminar | Suelo plano y seguro; las luces ámbar marcan la dirección (derecha). | Escondite: carrizos frontales en X≈4. |
| 2 | **La Ribera** | 20–40 | Correr | Tramo recto y largo que invita a mantener `Shift`; una **canoa varada** (X 31–34, 1 m de alto) se salta sin dejar de correr. | Escondite: carrizos en X≈26. |
| 3 | **El Hueco** | 40–58 | Saltar | Hueco de 3 m (X 44–47) con agua abajo y zona de muerte. Al caer, Itzel reaparece en el inicio. | Dos salidas: seguir a la derecha o volver. |
| 4 | **El Muelle Roto** | 58–83 | Salto alto | Muelle de madera a 1.8 m (X 58–80) sobre el agua, con postes; falta un tablón (X 68–70.5). Unas rocas-escalón (X 80–83) permiten bajar. | Dos salidas: el muelle o volver; escondite en los carrizos de X≈56. |
| 5 | **El Tronco Caído** | 83–102 | Agacharse | Tronco apoyado en rocas, con su parte baja a 0.9 m (X 88–93). | Escondite: carrizos en X≈96. |
| — | **La Ofrenda** | 102–110 | Meta | Ofrenda con luz ámbar más intensa en X 106. | Ofrenda (8.2: "cada 3–5 salas"). |

## Capas de profundidad (GDD 8.3)

La cámara mira hacia **+Z** (ver "Convención de cámara"), así que el signo de las capas se invierte respecto a los números originales del GDD:

- **Capa de juego (Z = 0):** suelo de lodo, canoa, muelle, rocas-escalón y tronco. Todo con collider, capa `Ground` y tag `Ground`.
- **Capa de fondo (Z +5 a +20):** plano de agua, colinas, árboles secos, una segunda canoa, lirios y luna. Sin colliders.
- **Capa frontal (Z −2 a −6):** carrizos (bambú y pasto alto de Kenney) que tapan parcialmente la vista. Sin colliders.

## Iluminación y color (GDD 9.2 y 9.4)

- Luz direccional fría tipo luna (`#6FA3C7`), ambiente `#0B1D3A`.
- Niebla *Exponential Squared* `#0B1D3A`.
- Ámbar `#E8A84A` **solo** en las luces de ruta (puntuales tenues) y en la ofrenda.
- Los modelos de Kenney se recolorean con materiales de la paleta: madera `#2C5F8A`, roca `#123A63`, vegetación `#0B1D3A`/`#123A63` y agua `#123A63` semitransparente.

## Convención de cámara

La cámara mira hacia **+Z** desde Z negativa (`CamaraPruebasItzel`, desplazamiento `(0, 4.5, −18)`, FOV 32°). Así, moverse a la derecha (+X) se ve a la derecha en pantalla. Esta convención queda fijada para todo el proyecto (GDD 8.3, versión 0.3): **fondo en Z positiva y frente en Z negativa**.

## Pendientes fuera de este nivel

- Escondites funcionales (la mecánica *Esconderse* aún no existe; los carrizos solo marcan dónde irán).
- Ofrenda funcional (guardado y punto de reaparición).
- La Llorona errante en la capa de fondo y el Lamento Invertido.
