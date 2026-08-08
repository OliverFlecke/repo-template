import type { GlobalProvider } from "@ladle/react";
import type { CSSProperties } from "react";
import "../app/globals.css";

// Mirrors the light/dark variable values in app/globals.css. The app itself
// only follows the OS-level prefers-color-scheme, but Ladle has its own
// light/dark toggle, so we override the custom properties here to make
// story previews follow that toggle instead of the OS preference.
const lightTokens = {
	"--bg-primary": "#ffffff",
	"--bg-secondary": "#f2f2f2",
	"--fg-primary": "#171717",
	"--text-primary": "#000",
	"--text-secondary": "#666",
	"--text-disabled": "#9c9c9c",
	"--button-primary-hover": "#383838",
	"--button-secondary-hover": "#f2f2f2",
	"--button-secondary-border": "#ebebeb",
	"--color-surface": "#ffffff",
	"--color-surface-raised": "#ffffff",
	"--color-border": "#d9d9d9",
	"--color-border-strong": "#b8b8b8",
	"--color-primary": "#171717",
	"--color-primary-hover": "#383838",
	"--color-primary-active": "#000000",
	"--color-on-primary": "#ffffff",
	"--color-danger": "#c0322a",
	"--color-danger-hover": "#a12a23",
	"--color-on-danger": "#ffffff",
	"--color-success": "#1e7d3c",
	"--color-warning": "#a16207",
	"--color-focus-ring": "#0a66ff",
	"--shadow-sm": "0 1px 2px rgba(0, 0, 0, 0.08)",
	"--shadow-md": "0 4px 12px rgba(0, 0, 0, 0.12)",
};

const darkTokens = {
	"--bg-primary": "#0a0a0a",
	"--bg-secondary": "#171717",
	"--fg-primary": "#ededed",
	"--text-primary": "#ededed",
	"--text-secondary": "#ccc",
	"--text-disabled": "#6b6b6b",
	"--color-surface": "#0a0a0a",
	"--color-surface-raised": "#171717",
	"--color-border": "#333333",
	"--color-border-strong": "#4d4d4d",
	"--color-primary": "#ededed",
	"--color-primary-hover": "#d4d4d4",
	"--color-primary-active": "#ffffff",
	"--color-on-primary": "#0a0a0a",
	"--color-danger": "#e5564a",
	"--color-danger-hover": "#ef6e63",
	"--color-on-danger": "#171717",
	"--color-success": "#3ecc70",
	"--color-warning": "#e0a530",
	"--color-focus-ring": "#5b9bff",
	"--shadow-sm": "0 1px 2px rgba(0, 0, 0, 0.4)",
	"--shadow-md": "0 4px 16px rgba(0, 0, 0, 0.5)",
};

export const Provider: GlobalProvider = ({ children, globalState }) => (
	<div
		style={
			{
				minHeight: "100vh",
				padding: 24,
				colorScheme: globalState.theme,
				...(globalState.theme === "dark" ? darkTokens : lightTokens),
				background: "var(--bg-primary)",
				color: "var(--fg-primary)",
			} as CSSProperties
		}
	>
		{children}
	</div>
);
