import { useEffect, useMemo, useState } from "react";
import { AppShell } from "./components/AppShell";
import { ActiveCallPanel } from "./components/ActiveCallPanel";
import { ConnectionLogs } from "./components/ConnectionLogs";
import { ContactList } from "./components/ContactList";
import { DialPad } from "./components/DialPad";
import { IncomingCallModal } from "./components/IncomingCallModal";
import { PhoneStatusCard } from "./components/PhoneStatusCard";
import { SettingsPanel } from "./components/SettingsPanel";
import { deskCallBridge } from "./lib/bridge";
import { contactNameForNumber } from "./lib/contacts";
import type { ActiveCall, BluetoothDevice, BridgeInbound, Contact, HelperState, LogEntry } from "./lib/types";

const initialState: HelperState = {
  helperMode: "MockMode",
  deviceStatus: "Disconnected",
  audioEndpoints: [],
  recentCalls: []
};

export default function App() {
  const [helperConnected, setHelperConnected] = useState(false);
  const [state, setState] = useState<HelperState>(initialState);
  const [devices, setDevices] = useState<BluetoothDevice[]>([]);
  const [contacts, setContacts] = useState<Contact[]>([]);
  const [logs, setLogs] = useState<LogEntry[]>([]);
  const [activeCall, setActiveCall] = useState<ActiveCall | null>(null);
  const [incomingCall, setIncomingCall] = useState<ActiveCall | null>(null);
  const [settingsOpen, setSettingsOpen] = useState(false);

  useEffect(() => {
    const offMessage = deskCallBridge.onMessage(handleBridgeMessage);
    const offConnection = deskCallBridge.onConnection(setHelperConnected);
    deskCallBridge.connect().catch((error) => {
      setLogs((existing) => [
        {
          id: crypto.randomUUID(),
          timestamp: new Date().toISOString(),
          level: "Error",
          source: "Renderer",
          message: error instanceof Error ? error.message : "Unable to connect to DeskCall helper."
        },
        ...existing
      ]);
    });

    return () => {
      offMessage();
      offConnection();
    };
  }, []);

  const selectedContactName = useMemo(() => {
    if (!activeCall) {
      return undefined;
    }

    return contactNameForNumber(contacts, activeCall.number);
  }, [activeCall, contacts]);

  function handleBridgeMessage(message: BridgeInbound) {
    switch (message.type) {
      case "helper:status":
      case "device:statusChanged":
        setState(message.payload);
        break;
      case "device:listResult":
        setDevices(message.payload);
        break;
      case "contacts:listResult":
        setContacts(message.payload);
        break;
      case "audio:devicesChanged":
        setState((current) => ({ ...current, audioEndpoints: message.payload }));
        break;
      case "logs:listResult":
        setLogs(message.payload);
        break;
      case "log:entry":
        setLogs((current) => [message.payload, ...current].slice(0, 250));
        break;
      case "call:incoming":
      case "call:ringing":
        setIncomingCall(message.payload);
        setActiveCall(message.payload);
        break;
      case "call:active":
        setIncomingCall(null);
        setActiveCall(message.payload);
        setState((current) => ({ ...current, deviceStatus: "CallActive" }));
        break;
      case "call:ended":
        setIncomingCall(null);
        setActiveCall(message.payload);
        window.setTimeout(() => setActiveCall(null), 900);
        setState((current) => ({ ...current, deviceStatus: current.selectedDevice ? "Connected" : "Disconnected" }));
        break;
      case "call:error":
        setLogs((current) => [
          {
            id: crypto.randomUUID(),
            timestamp: new Date().toISOString(),
            level: "Error",
            source: "HFP",
            message: message.payload.message
          },
          ...current
        ]);
        break;
    }
  }

  const bridgeAction = {
    refreshDevices: () => deskCallBridge.request("devices:list"),
    selectDevice: (deviceId: string) => deskCallBridge.request("device:select", { deviceId }),
    connectDevice: () => deskCallBridge.request("device:connect"),
    disconnectDevice: () => deskCallBridge.request("device:disconnect"),
    dial: (number: string) => deskCallBridge.request("call:dial", { number }),
    answer: () => deskCallBridge.request("call:answer"),
    reject: () => deskCallBridge.request("call:reject"),
    end: () => deskCallBridge.request("call:end"),
    createContact: (contact: Pick<Contact, "name" | "phone" | "favorite">) => deskCallBridge.request("contacts:create", contact),
    updateContact: (contact: Contact) => deskCallBridge.request("contacts:update", contact),
    deleteContact: (contactId: string) => deskCallBridge.request("contacts:delete", { contactId }),
    setMode: (helperMode: "RealMode" | "MockMode") => deskCallBridge.request("helper:setMode", { helperMode }),
    mockIncoming: () => deskCallBridge.request("test:incoming"),
    testLog: () => deskCallBridge.request("test:log")
  };

  return (
    <AppShell
      helperConnected={helperConnected}
      state={state}
      onOpenSettings={() => setSettingsOpen(true)}
    >
      <div className="grid h-full grid-cols-[320px_minmax(420px,1fr)_360px] gap-5">
        <div className="flex min-h-0 flex-col gap-5">
          <PhoneStatusCard
            state={state}
            devices={devices}
            helperConnected={helperConnected}
            onRefreshDevices={bridgeAction.refreshDevices}
            onSelectDevice={bridgeAction.selectDevice}
            onConnect={bridgeAction.connectDevice}
            onDisconnect={bridgeAction.disconnectDevice}
          />
        </div>

        <main className="flex min-h-0 flex-col gap-5">
          <ActiveCallPanel
            activeCall={activeCall ? { ...activeCall, name: activeCall.name ?? selectedContactName } : null}
            onAnswer={bridgeAction.answer}
            onReject={bridgeAction.reject}
            onEnd={bridgeAction.end}
          />
          <DialPad onDial={bridgeAction.dial} disabled={!helperConnected} />
        </main>

        <ContactList
          contacts={contacts}
          onDial={(phone) => bridgeAction.dial(phone)}
          onCreate={bridgeAction.createContact}
          onUpdate={bridgeAction.updateContact}
          onDelete={bridgeAction.deleteContact}
        />
      </div>

      <ConnectionLogs logs={logs} />

      {incomingCall ? (
        <IncomingCallModal
          call={{ ...incomingCall, name: incomingCall.name ?? contactNameForNumber(contacts, incomingCall.number) }}
          onAnswer={bridgeAction.answer}
          onReject={bridgeAction.reject}
        />
      ) : null}

      <SettingsPanel
        open={settingsOpen}
        state={state}
        devices={devices}
        helperConnected={helperConnected}
        onClose={() => setSettingsOpen(false)}
        onSetMode={bridgeAction.setMode}
        onMockIncoming={bridgeAction.mockIncoming}
        onTestLog={bridgeAction.testLog}
        onRefreshDevices={bridgeAction.refreshDevices}
      />
    </AppShell>
  );
}
