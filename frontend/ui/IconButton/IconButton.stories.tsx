import type { Story } from "@ladle/react";
import {
	IconButton,
	type IconButtonColor,
	type IconButtonSize,
	type IconButtonVariant,
} from "./IconButton";

const variants: IconButtonVariant[] = ["filled", "outlined", "text"];
const colors: IconButtonColor[] = ["primary", "secondary", "danger"];
const sizes: IconButtonSize[] = ["sm", "md", "lg"];

export const Gallery: Story = () => (
	<div style={{ display: "flex", flexDirection: "column", gap: 24 }}>
		{variants.map((variant) => (
			<div
				key={variant}
				style={{ display: "flex", alignItems: "center", gap: 12 }}
			>
				{colors.map((color) => (
					<IconButton
						key={color}
						variant={variant}
						color={color}
						aria-label={`${variant} ${color}`}
					>
						+
					</IconButton>
				))}
				<IconButton variant={variant} disabled aria-label="Disabled">
					+
				</IconButton>
			</div>
		))}

		<div style={{ display: "flex", alignItems: "center", gap: 12 }}>
			{sizes.map((size) => (
				<IconButton key={size} size={size} aria-label={`Size ${size}`}>
					+
				</IconButton>
			))}
		</div>
	</div>
);

export const Playground: Story<{
	variant: IconButtonVariant;
	color: IconButtonColor;
	size: IconButtonSize;
	disabled: boolean;
}> = ({ variant, color, size, disabled }) => (
	<IconButton
		variant={variant}
		color={color}
		size={size}
		disabled={disabled}
		aria-label="Icon button"
	>
		+
	</IconButton>
);

Playground.args = { disabled: false };
Playground.argTypes = {
	variant: {
		options: variants,
		control: { type: "select" },
		defaultValue: "text",
	},
	color: {
		options: colors,
		control: { type: "select" },
		defaultValue: "primary",
	},
	size: { options: sizes, control: { type: "select" }, defaultValue: "md" },
};
