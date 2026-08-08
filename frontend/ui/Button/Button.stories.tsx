import type { Story } from "@ladle/react";
import {
	Button,
	type ButtonColor,
	type ButtonSize,
	type ButtonVariant,
} from "./Button";

const variants: ButtonVariant[] = ["filled", "outlined", "text"];
const colors: ButtonColor[] = ["primary", "secondary", "danger"];
const sizes: ButtonSize[] = ["sm", "md", "lg"];

export const Gallery: Story = () => (
	<div style={{ display: "flex", flexDirection: "column", gap: 24 }}>
		{variants.map((variant) => (
			<div
				key={variant}
				style={{ display: "flex", alignItems: "center", gap: 12 }}
			>
				{colors.map((color) => (
					<Button key={color} variant={variant} color={color}>
						{variant} {color}
					</Button>
				))}
				<Button variant={variant} disabled>
					Disabled
				</Button>
			</div>
		))}

		<div style={{ display: "flex", alignItems: "center", gap: 12 }}>
			{sizes.map((size) => (
				<Button key={size} size={size}>
					Size {size}
				</Button>
			))}
		</div>
	</div>
);

export const Playground: Story<{
	label: string;
	variant: ButtonVariant;
	color: ButtonColor;
	size: ButtonSize;
	disabled: boolean;
}> = ({ label, variant, color, size, disabled }) => (
	<Button variant={variant} color={color} size={size} disabled={disabled}>
		{label}
	</Button>
);

Playground.args = { label: "Button", disabled: false };
Playground.argTypes = {
	variant: {
		options: variants,
		control: { type: "select" },
		defaultValue: "filled",
	},
	color: {
		options: colors,
		control: { type: "select" },
		defaultValue: "primary",
	},
	size: { options: sizes, control: { type: "select" }, defaultValue: "md" },
};
