import type { Story } from "@ladle/react";
import { Label } from "@/ui/Label/Label";
import { Checkbox } from "./Checkbox";

const labelStyle = {
	display: "inline-flex",
	alignItems: "center",
	gap: 8,
	fontWeight: 400,
};

export const Gallery: Story = () => (
	<div style={{ display: "flex", gap: 24 }}>
		<Label style={labelStyle}>
			<Checkbox />
			Unchecked
		</Label>
		<Label style={labelStyle}>
			<Checkbox defaultChecked />
			Checked
		</Label>
		<Label style={labelStyle}>
			<Checkbox disabled />
			Disabled
		</Label>
		<Label style={labelStyle}>
			<Checkbox disabled defaultChecked />
			Disabled checked
		</Label>
	</div>
);

export const Playground: Story<{
	disabled: boolean;
	defaultChecked: boolean;
}> = ({ disabled, defaultChecked }) => (
	<Label style={labelStyle}>
		<Checkbox disabled={disabled} defaultChecked={defaultChecked} />
		Checkbox
	</Label>
);

Playground.args = { disabled: false, defaultChecked: false };
