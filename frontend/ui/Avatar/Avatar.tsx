import { cx } from "@/ui/util/cx";
import styles from "./Avatar.module.css";

export type AvatarSize = "sm" | "md" | "lg";

export interface AvatarProps {
	src?: string;
	name?: string;
	size?: AvatarSize;
	className?: string;
}

export function Avatar({ src, name, size = "md", className }: AvatarProps) {
	return (
		<span className={cx(styles.avatar, styles[size], className)}>
			{src ? (
				// biome-ignore lint/performance/noImgElement: avatar src is an arbitrary external URL (Auth0/Gravatar/etc), not worth a next/image remote-pattern allowlist
				<img src={src} alt="Profile picture" className={styles.image} />
			) : (
				<span aria-hidden="true">{getInitials(name)}</span>
			)}
		</span>
	);
}

function getInitials(name?: string): string {
	if (!name) return "?";

	const [first, ...rest] = name.trim().split(/\s+/);
	const last = rest.at(-1);

	return ((first?.[0] ?? "") + (last?.[0] ?? "")).toUpperCase();
}
