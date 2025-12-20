# Terrain alarm – manual (KerbNoteLite)

Terrain alarm is a set of low-altitude (AGL) flight warnings. It helps prevent crashes and supports landing approach (gear, sink rate, altitude callouts).

## How to open the settings

1. Open the mod main window.
2. Open the **Global Alarms** panel (left sliding bar).
3. Click **Terrain alarm**.

At the top you have **Enable / Disable** – it turns the whole Terrain alarm on/off.

## Units and basics

- **AGL** = meters above ground.
- **VSp** = vertical speed in m/s.
  - descent is **negative** (e.g. `-30` = 30 m/s down).

## Vehicle type filter

In **Vehicle type filter:** choose one of the two options:

- **All vessels** – works for every craft (rockets, landers, aircraft).
- **Aircraft only** – works only for aircraft.

How does the mod know it is an aircraft?
- if the craft has typical aerodynamic parts (wings / lifting surfaces / control surfaces, e.g. wings, ailerons), it is treated as an aircraft.

## Terrain warning (Pull Up)

Checkbox: **Terrain warning (Pull Up)**

Main warning: when you are low (AGL) and descending fast (VSp), a warning will trigger.

Fields:
- **AGL:** (meters) – below this height the warning can trigger.
  - typical: `200–1000`
- **VSp:** (m/s, negative) – how fast you must descend to trigger the warning.
  - typical: `-15` to `-60`

Examples:
- Universal: `AGL=750`, `VSp=-30`
- Later/landing-friendly: `AGL=250`, `VSp=-20`

Mute option:
- **Silence (visual only)** – check if you want screen message only (no sound).

Note:
- With landing gear deployed, the “Pull Up” warning is suppressed so it does not disturb landing.

## Counting out

Checkbox: **Counting out**

Enables altitude callouts during descent (messages like `50m`, `40m`, `30m`, `20m`, `10m`).

Mute option:
- **Silence (visual only)** – mutes sound, keeps the text.

## Sink rate alarm

Checkbox: **Sink rate alarm**

Warns when:
- gear is deployed,
- you are low above ground,
- you are descending too fast.

Mute option:
- **Silence (visual only)**

## Gear not deployed

Checkbox: **Gear not deployed**

Warns when you approach the ground without gear deployed (while descending and at a “landable” speed).

Fields (shown after enabling):
- **GearAGL:** (meters) – warn below this altitude.
  - typical: `150–400`
- **MaxSpd:** (m/s) – warn only if surface speed is below this value.
  - typical aircraft: `70–140`
  - typical lander: `20–80`

Mute option:
- **Silence (visual only)**

Example (aircraft):
- `GearAGL=250`, `MaxSpd=120`

## Terrain ahead (TTI)

Checkbox: **Terrain ahead (TTI)**

Predicts “terrain ahead” on your current flight path (useful for fast, low-level flight, e.g. through valleys).

Fields (shown after enabling):
- **MaxTime(s):** (seconds) – how far ahead to look.
  - typical: `4–10`
- **Step(s):** (seconds) – sampling resolution (smaller = more accurate).
  - typical: `0.2–0.5`
- **Margin(m):** (meters) – clearance margin above terrain.
  - typical: `100–500`
- **MinSpd:** (m/s) – minimum speed where this feature makes sense.
  - typical: `20–40`

Example:
- `MaxTime(s)=6`, `Step(s)=0.25`, `Margin(m)=0`, `MinSpd=30`

## Landed sound

Section: **Landed sound:**

- **Silence (no sound on landing)** – check if you do not want the landing sound.

## FAQ

- **What should I enter in VSp?** Use a negative value for descent, e.g. `-20`, `-30`.
- **Pull Up does not work during landing:** normal – with gear deployed the warning is suppressed.