import { forwardRef, type TextareaHTMLAttributes } from "react";
import { cx } from "@/ui/util/cx";
import styles from "./Textarea.module.css";

export type TextareaProps = TextareaHTMLAttributes<HTMLTextAreaElement>;

export const Textarea = forwardRef<HTMLTextAreaElement, TextareaProps>(
	({ className, ...props }, ref) => {
		return (
			<textarea
				ref={ref}
				className={cx(styles.textarea, className)}
				{...props}
			/>
		);
	},
);

Textarea.displayName = "Textarea";
