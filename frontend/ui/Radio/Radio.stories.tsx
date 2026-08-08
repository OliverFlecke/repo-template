import type { Story } from "@ladle/react";
import { Label } from "@/ui/Label/Label";
import { Radio } from "./Radio";

const labelStyle = {
	display: "inline-flex",
	alignItems: "center",
	gap: 8,
	fontWeight: 400,
};

export const Gallery: Story = () => (
	<div style={{ display: "flex", gap: 24 }}>
		<Label style={labelStyle}>
			<Radio name="gallery" />
			Unchecked
		</Label>
		<Label style={labelStyle}>
			<Radio name="gallery" defaultChecked />
			Checked
		</Label>
		<Label style={labelStyle}>
			<Radio name="gallery-disabled" disabled />
			Disabled
		</Label>
		<Label style={labelStyle}>
			<Radio name="gallery-disabled" disabled defaultChecked />
			Disabled checked
		</Label>
	</div>
);

export const Playground: Story<{
	disabled: boolean;
	defaultChecked: boolean;
}> = ({ disabled, defaultChecked }) => (
	<Label style={labelStyle}>
		<Radio
			name="playground"
			disabled={disabled}
			defaultChecked={defaultChecked}
		/>
		Radio
	</Label>
);

Playground.args = { disabled: false, defaultChecked: false };
