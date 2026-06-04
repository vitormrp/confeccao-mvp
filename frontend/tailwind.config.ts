import type { Config } from "tailwindcss";

const config: Config = {
  content: [
    "./app/**/*.{ts,tsx}",
    "./components/**/*.{ts,tsx}",
    "./lib/**/*.{ts,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        bg: "#F5F4F0",
        bg2: "#EDECEA",
        bg3: "#E4E2DE",
        card: "#FFFFFF",
        border: "#D8D5D0",
        border2: "#C8C4BE",
        text: "#1A1816",
        text2: "#6B6760",
        text3: "#9E9A94",
        accent: "#2D5BE3",
        "accent-light": "#EEF2FD",
        "accent-border": "#C2D0F8",
        green: "#1E7B4B",
        "green-light": "#EAFAF1",
        "green-border": "#A8E0C0",
        amber: "#B45309",
        "amber-light": "#FEF3CD",
        "amber-border": "#F5D57A",
        red: "#C0392B",
        "red-light": "#FDECEA",
        "red-border": "#F0B0AA",
        purple: "#6B4FBB",
        "purple-light": "#F2EEFF",
        "purple-border": "#C8B8F0",
      },
      fontFamily: {
        sans: ["DM Sans", "system-ui", "sans-serif"],
        mono: ["DM Mono", "ui-monospace", "monospace"],
      },
      boxShadow: {
        card: "0 1px 3px rgba(0,0,0,0.08), 0 1px 2px rgba(0,0,0,0.04)",
        "card-lg": "0 4px 12px rgba(0,0,0,0.1)",
      },
    },
  },
  plugins: [],
};

export default config;
