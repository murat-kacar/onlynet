# Firmware Reference

This document is the stable reference for the ESP32 table display firmware
and its tenant-runtime contract.

Firmware is part of tenant lifecycle operations, but devices are tenant
runtime clients. They MUST NOT know platform internals.

## Source Location

Committed firmware source lives under:

```text
src/packages/firmware/arduino/tabflow-table-display/
  firmware.ino
  config.example.h
```

`config.example.h` is the safe committed template. A real `config.h`
contains Wi-Fi credentials and a device key, so it must remain outside
source control.

## Hardware Profile

Current locked hardware profile:

- ESP32-C3 Super Mini V1.6.0.1
- 1.8 inch TFT SPI 128x160 V1.1
- Adafruit GFX
- Adafruit ST7735 or ST7789

Current prototype pin map:

```text
TFT SCK/SCLK -> GPIO0
TFT SDA/MOSI -> GPIO1
TFT A0/DC    -> GPIO2
TFT RESET    -> GPIO3
TFT CS       -> GPIO4
TFT LED/BL   -> 3V3 or optional configured backlight pin
TFT GND      -> GND
TFT VCC      -> 3V3
```

Pins intentionally avoided:

- GPIO8 and GPIO9 because of boot and strapping risk
- GPIO20 and GPIO21 to avoid USB and serial interference

GPIO2 is also boot and strapping sensitive, but the current physical wiring
uses it for TFT A0/DC. This is accepted for the current prototype profile.
If boot instability appears, move A0/DC to a safer free GPIO and update the
generated firmware config output at the same time.

## Runtime Contract

The firmware does not generate QR codes. The tenant host owns token
generation and WebSocket delivery.

Device config contains:

- table id
- backend host and port
- device key
- Wi-Fi SSID and password for the physical environment
- display pin constants
- firmware timing constants

Device behavior:

- connects to Wi-Fi with sleep disabled
- syncs time for TLS
- connects to the tenant host WebSocket at
  `wss://<backend-host>/ws/tables/<table-number>?deviceKey=<device-key>`
- authenticates with the table id and device key (constant-time compare
  on the server)
- receives `auth_ok`
- receives `new_token`
- renders backend-produced QR matrix data
- keeps the last valid QR until the backend sends a replacement or
  expiry state

Current token payload fields:

- `url`
- `ttl_seconds`
- `expires_at`
- `qr_side`
- `qr_bits_hex`

`qr_side` and `qr_bits_hex` are produced by the tenant host using the
runtime QR encoder. Firmware stays backend-driven and must not grow its own
QR generation logic.

## Generated Artifacts

Tenant provisioning and table creation may generate per-table flash-ready
`.ino` artifacts.

Rules:

- generated `.ino` files contain secrets
- generated firmware is a runtime output
- generated firmware must not be committed
- committed source stays under `src/packages/firmware`
- generated artifacts belong in runtime-owned output roots

Reference output layout:

```text
runtime/generated/tenants/<tenant-code>/firmware/
  table-000.ino
  table-999.ino
  table-balcony-003.ino
```

Production deployments should point provisioning output at a restricted
host runtime directory outside the source tree.

Generated filenames come from the current table label slugged into a
flash-ready single sketch name. Slugs are lowercased, ASCII-only, and use
hyphens as separators. For example:

```text
table-balcony-003.ino
floor-2-garden-counter-002.ino
```

The generated sketch contains the shared firmware source plus
device-specific defines for tenant domain, Wi-Fi defaults, table id, device
key, pin map, and timing constants.
