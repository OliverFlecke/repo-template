import type { Story } from "@ladle/react";
import { Tooltip } from "./Tooltip";

export const Gallery: Story = () => (
	<div style={{ display: "flex", gap: 24, padding: 64 }}>
		<Tooltip text="Email verified">
			<span>Hover or focus me</span>
		</Tooltip>
	</div>
);
