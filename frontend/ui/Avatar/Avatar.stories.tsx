import type { Story } from "@ladle/react";
import { Avatar, type AvatarSize } from "./Avatar";

const sizes: AvatarSize[] = ["sm", "md", "lg"];

export const Gallery: Story = () => (
	<div style={{ display: "flex", alignItems: "center", gap: 12 }}>
		{sizes.map((size) => (
			<Avatar key={size} size={size} name="Ada Lovelace" />
		))}
		<Avatar size="lg" src="https://i.pravatar.cc/96" name="Ada Lovelace" />
		<Avatar size="lg" name="Cher" />
	</div>
);

export const Playground: Story<{ name: string; size: AvatarSize }> = ({
	name,
	size,
}) => <Avatar name={name} size={size} />;

Playground.args = { name: "Ada Lovelace" };
Playground.argTypes = {
	size: { options: sizes, control: { type: "select" }, defaultValue: "md" },
};
