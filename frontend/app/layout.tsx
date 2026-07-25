import type { Metadata } from "next";
import { Geist, Geist_Mono } from "next/font/google";
import "./globals.css";
import { PropsWithChildren } from "react";
import QueryProvider from "@/component/QueryProvider";

const geistSans = Geist({
	variable: "--font-geist-sans",
	subsets: ["latin"],
});

const geistMono = Geist_Mono({
	variable: "--font-geist-mono",
	subsets: ["latin"],
});

export const metadata: Metadata = {
	title: "Template App",
	description: "A template application",
};

export default function RootLayout({ children }: Readonly<PropsWithChildren>) {
	return (
		<html lang="en" className={`${geistSans.variable} ${geistMono.variable}`}>
			<body>
				<QueryProvider>{children}</QueryProvider>
			</body>
		</html>
	);
}
