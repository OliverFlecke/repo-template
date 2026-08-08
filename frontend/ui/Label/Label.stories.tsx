import type { Story } from "@ladle/react";
import { Input } from "@/ui/Input/Input";
import { Label } from "./Label";

export const Gallery: Story = () => (
	<div
		style={{ display: "flex", flexDirection: "column", gap: 24, maxWidth: 320 }}
	>
		<div style={{ display: "flex", flexDirection: "column", gap: 4 }}>
			<Label htmlFor="label-story-input">Associated via htmlFor</Label>
			<Input id="label-story-input" placeholder="Type something" />
		</div>

		<Label
			style={{
				display: "inline-flex",
				alignItems: "center",
				gap: 8,
				fontWeight: 400,
			}}
		>
			<input type="checkbox" />
			Wrapping the control as children
		</Label>
	</div>
);
