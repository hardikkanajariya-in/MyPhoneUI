import { contextBridge, ipcRenderer } from "electron";

contextBridge.exposeInMainWorld("deskcallRuntime", {
  getRuntime: () => ipcRenderer.invoke("deskcall:get-runtime")
});
