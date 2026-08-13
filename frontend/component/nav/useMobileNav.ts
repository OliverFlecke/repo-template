import { useEffect, useState } from "react";

/** Whether the mobile overlay nav is open; closes on navigation and Escape. */
export function useMobileNav(pathname: string) {
	const [open, setOpen] = useState(false);

	// biome-ignore lint/correctness/useExhaustiveDependencies: only re-run on navigation
	useEffect(() => {
		setOpen(false);
	}, [pathname]);

	useEffect(() => {
		if (!open) return;

		document.body.style.overflow = "hidden";
		const onKeyDown = (e: KeyboardEvent) => {
			if (e.key === "Escape") setOpen(false);
		};
		window.addEventListener("keydown", onKeyDown);

		return () => {
			document.body.style.overflow = "";
			window.removeEventListener("keydown", onKeyDown);
		};
	}, [open]);

	const toggle = () => setOpen((o) => !o);

	return [open, toggle] as const;
}
