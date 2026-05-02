# DeskCall

DeskCall is a Windows desktop Bluetooth calling app that gives a laptop a smartwatch-style calling surface for one paired phone. It is intentionally calling-only: no SMS, photo access, notifications, file sync, screen mirroring, Phone Link, or mobile companion app.

## What It Does

- Shows paired Bluetooth devices detected by Windows.
- Lets you select one phone and inspect likely HFP/audio readiness.
- Provides a polished dial pad, local contacts, incoming-call popup, active-call timer, and connection logs.
- Runs a native C# helper for Bluetooth, HFP command abstraction, audio endpoint discovery, local storage, and logs.
- Includes a complete MockMode so the UI and bridge can be tested without a phone.
- Includes RealMode discovery and clear limitation reporting for machines where Windows or the Bluetooth driver does not expose HFP/RFCOMM control.

## Architecture

Electron and React are the presentation layer only. The renderer never talks directly to Bluetooth hardware.

```text
React UI <-> local WebSocket bridge <-> C# .NET 8 helper <-> Windows Bluetooth/audio APIs
```

The Electron main process starts the C# helper automatically. The helper listens on `ws://127.0.0.1:49321/deskcall` and exchanges strongly typed JSON messages such as `devices:list`, `call:dial`, `call:answer`, `contacts:create`, `log:entry`, and `call:incoming`.

## Why JavaScript Alone Is Not Enough

Browser and Electron renderer APIs do not expose the Windows Bluetooth Hands-Free Profile control surface needed for phone-call AT commands, RFCOMM service checks, or audio endpoint inspection. Web Bluetooth also does not provide the classic Bluetooth HFP gateway behavior needed here. DeskCall therefore keeps hardware work in the C# helper and keeps the UI isolated from native details.

## Why The C# Helper Exists

C#/.NET can safely host the local bridge, persist state, run Windows discovery commands, structure HFP AT commands, and later add deeper Windows APIs such as WASAPI and RFCOMM without changing the React UI contract.

## Setup

Requirements:

- Windows 10/11
- Node.js 20 or newer
- npm
- .NET 8 SDK
- A phone paired manually through Windows Bluetooth settings for RealMode experiments

Install dependencies:

```powershell
npm install
```

Run development mode:

```powershell
npm run dev
```

Build the UI, Electron shell, and helper:

```powershell
npm run build
```

## Pair A Phone Manually In Windows

1. Open Windows Settings.
2. Go to Bluetooth & devices.
3. Enable Bluetooth.
4. Pair your phone normally.
5. Confirm Windows shows the phone as paired or connected.
6. Open DeskCall and press refresh in the selected-phone card.

DeskCall does not install anything on the phone and does not use Phone Link.

## MockMode

MockMode is the default and is fully usable without Bluetooth hardware.

Use Settings -> Helper mode -> MockMode, then:

- Press Incoming to test the smartwatch-style incoming call modal.
- Use Answer, Reject, and End.
- Use the dial pad or contact list for outgoing test calls.
- Watch bridge and helper events in the bottom log drawer.

MockMode is also the right mode for UI development and bridge testing.

## RealMode

Use Settings -> Helper mode -> RealMode after pairing the phone in Windows. In this MVP, RealMode performs best-effort detection and reports what Windows exposes:

- Paired Bluetooth PnP entries.
- Likely HFP service names when visible.
- Likely Bluetooth hands-free audio endpoint names.
- Structured errors when Windows or the driver does not expose app-level HFP control.

DeskCall does not attempt unsafe driver resets or unsupported radio hacks.

## Known Windows Bluetooth HFP Limitations

Windows Bluetooth behavior varies heavily by adapter, driver, phone vendor, and OS build. Many Windows systems do not expose a public, stable API for a normal desktop app to behave as a Bluetooth HFP audio gateway for every phone. Some systems expose audio endpoints but hide RFCOMM/HFP service details. Others allow call audio through the OS but do not permit third-party call-control AT command sockets.

DeskCall handles this by separating the HFP layer, logging all detection results, keeping MockMode complete, and returning explicit RealMode errors instead of pretending that hardware control succeeded.

## Troubleshooting

### Phone Not Visible

- Confirm the phone is paired in Windows Settings.
- Toggle Bluetooth off/on in Windows.
- Press refresh in DeskCall.
- Check the log drawer for `Bluetooth` entries.

### HFP Service Not Visible

- Some drivers hide HFP/RFCOMM service details from normal desktop apps.
- Check whether Windows created a hands-free or headset audio device.
- Try removing and pairing the phone again.

### Call Audio Not Routing

- Open Windows Sound settings.
- Look for endpoints with names such as Hands-Free, AG Audio, Bluetooth, or Headset.
- Set the correct input/output endpoint manually if Windows does not route automatically.

### Bluetooth Driver Issue

- Update the Bluetooth adapter driver from the laptop or adapter vendor.
- Avoid generic driver rollback unless you have a known-good version.
- Reboot after driver changes before retesting RealMode.

### iPhone Restrictions

iPhone Bluetooth call-control behavior can be restricted by iOS and Windows driver support. If HFP control is not visible, DeskCall will log the limitation and MockMode remains available for UI testing.

### Android Permissions

No mobile app is installed, so Android app permissions do not apply. Bluetooth pairing and call audio availability are controlled by Android Bluetooth settings and the Windows Bluetooth stack.

## Local Data

DeskCall stores local app state under the current Windows user profile:

- Selected phone id/name
- Helper mode
- Local contacts in `contacts.json`
- Recent call records

No SMS, photos, notifications, browser data, or phone files are accessed.

## Future Improvements

- PBAP contact import with explicit user consent.
- Deeper WASAPI endpoint routing and monitoring.
- A signed Windows installer.
- Auto-start with Windows.
- Tray mode and background call popup.
- Optional RFCOMM implementation for hardware/driver combinations that expose HFP sockets.
