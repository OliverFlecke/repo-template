import type { GlobalProvider } from "@ladle/react";
import "../app/globals.css";

export const Provider: GlobalProvider = ({ children }) => (
	<div
		style={{
			minHeight: "100vh",
			padding: 24,
			background: "var(--bg-primary)",
			color: "var(--fg-primary)",
		}}
	>
		{children}
	</div>
);
