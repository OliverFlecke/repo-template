import type { Story } from "@ladle/react";
import { Label } from "@/ui/Label/Label";
import { Switch } from "./Switch";

const labelStyle = {
	display: "inline-flex",
	alignItems: "center",
	gap: 8,
	fontWeight: 400,
};

export const Gallery: Story = () => (
	<div style={{ display: "flex", gap: 24 }}>
		<Label style={labelStyle}>
			<Switch />
			Off
		</Label>
		<Label style={labelStyle}>
			<Switch defaultChecked />
			On
		</Label>
		<Label style={labelStyle}>
			<Switch disabled />
			Disabled
		</Label>
		<Label style={labelStyle}>
			<Switch disabled defaultChecked />
			Disabled on
		</Label>
	</div>
);

export const Playground: Story<{
	disabled: boolean;
	defaultChecked: boolean;
}> = ({ disabled, defaultChecked }) => (
	<Label style={labelStyle}>
		<Switch disabled={disabled} defaultChecked={defaultChecked} />
		Switch
	</Label>
);

Playground.args = { disabled: false, defaultChecked: false };
