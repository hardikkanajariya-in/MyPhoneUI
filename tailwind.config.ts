import type { Config } from "tailwindcss";

export default {
  content: ["./index.html", "./src/**/*.{ts,tsx}"],
  theme: {
    extend: {
      boxShadow: {
        glow: "0 0 45px rgba(34, 211, 238, 0.18)",
        panel: "0 20px 80px rgba(0, 0, 0, 0.35)"
      },
      colors: {
        ink: "#07111f",
        panel: "rgba(12, 24, 41, 0.72)",
        line: "rgba(148, 163, 184, 0.18)"
      }
    }
  },
  plugins: []
} satisfies Config;
