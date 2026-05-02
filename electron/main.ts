import { app, BrowserWindow, ipcMain } from "electron";
import path from "node:path";
import { spawn, ChildProcessWithoutNullStreams } from "node:child_process";
import fs from "node:fs";

const helperPort = 49321;
let helperProcess: ChildProcessWithoutNullStreams | null = null;
let mainWindow: BrowserWindow | null = null;

const isDev = !app.isPackaged;

function resolveHelperLaunch() {
  if (isDev) {
    return {
      command: "dotnet",
      args: ["run", "--project", path.join(app.getAppPath(), "native", "DeskCall.Helper", "DeskCall.Helper.csproj"), "--", "--port", String(helperPort)],
      cwd: app.getAppPath()
    };
  }

  const exePath = path.join(process.resourcesPath, "DeskCall.Helper", "DeskCall.Helper.exe");
  if (fs.existsSync(exePath)) {
    return { command: exePath, args: ["--port", String(helperPort)], cwd: path.dirname(exePath) };
  }

  return null;
}

function startHelper() {
  if (helperProcess) {
    return;
  }

  const launch = resolveHelperLaunch();
  if (!launch) {
    console.error("DeskCall helper executable was not found in packaged resources.");
    return;
  }

  helperProcess = spawn(launch.command, launch.args, {
    cwd: launch.cwd,
    windowsHide: true,
    stdio: "pipe"
  });

  helperProcess.stdout.on("data", (chunk) => console.log(`[helper] ${chunk.toString().trim()}`));
  helperProcess.stderr.on("data", (chunk) => console.error(`[helper] ${chunk.toString().trim()}`));
  helperProcess.on("exit", (code) => {
    console.log(`DeskCall helper exited with code ${code ?? "unknown"}`);
    helperProcess = null;
  });
}

function stopHelper() {
  if (!helperProcess) {
    return;
  }

  helperProcess.kill();
  helperProcess = null;
}

async function createWindow() {
  startHelper();

  mainWindow = new BrowserWindow({
    width: 1360,
    height: 880,
    minWidth: 1100,
    minHeight: 720,
    title: "DeskCall",
    backgroundColor: "#07111f",
    titleBarStyle: "hiddenInset",
    webPreferences: {
      preload: path.join(__dirname, "preload.js"),
      contextIsolation: true,
      nodeIntegration: false
    }
  });

  if (isDev) {
    await mainWindow.loadURL("http://127.0.0.1:5173");
    mainWindow.webContents.openDevTools({ mode: "detach" });
  } else {
    await mainWindow.loadFile(path.join(app.getAppPath(), "dist", "index.html"));
  }
}

ipcMain.handle("deskcall:get-runtime", () => ({
  helperPort,
  platform: process.platform,
  isPackaged: app.isPackaged
}));

app.whenReady().then(createWindow);

app.on("window-all-closed", () => {
  stopHelper();
  if (process.platform !== "darwin") {
    app.quit();
  }
});

app.on("activate", () => {
  if (BrowserWindow.getAllWindows().length === 0) {
    void createWindow();
  }
});

app.on("before-quit", stopHelper);
