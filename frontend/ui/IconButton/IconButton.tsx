import { type ButtonHTMLAttributes, forwardRef } from "react";
import { cx } from "@/ui/util/cx";
import styles from "./IconButton.module.css";

export type IconButtonVariant = "filled" | "outlined" | "text";
export type IconButtonColor = "primary" | "secondary" | "danger";
export type IconButtonSize = "sm" | "md" | "lg";

export interface IconButtonProps
	extends ButtonHTMLAttributes<HTMLButtonElement> {
	variant?: IconButtonVariant;
	color?: IconButtonColor;
	size?: IconButtonSize;
	"aria-label": string;
}

export const IconButton = forwardRef<HTMLButtonElement, IconButtonProps>(
	(
		{
			variant = "text",
			color = "primary",
			size = "md",
			className,
			type = "button",
			...props
		},
		ref,
	) => {
		return (
			<button
				ref={ref}
				type={type}
				className={cx(
					styles.iconButton,
					styles[variant],
					styles[color],
					styles[size],
					className,
				)}
				{...props}
			/>
		);
	},
);

IconButton.displayName = "IconButton";
