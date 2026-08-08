import { type ButtonHTMLAttributes, forwardRef } from "react";
import { cx } from "@/ui/util/cx";
import styles from "./Button.module.css";

export type ButtonVariant = "filled" | "outlined" | "text";
export type ButtonColor = "primary" | "secondary" | "danger";
export type ButtonSize = "sm" | "md" | "lg";

export interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
	variant?: ButtonVariant;
	color?: ButtonColor;
	size?: ButtonSize;
}

export const Button = forwardRef<HTMLButtonElement, ButtonProps>(
	(
		{
			variant = "filled",
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
					styles.button,
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

Button.displayName = "Button";
