import type { Metadata } from "next";
import { Geist, Geist_Mono } from "next/font/google";
import "./globals.css";
import type { PropsWithChildren } from "react";
import AuthProvider from "@/component/auth/Provider";
import Nav from "@/component/nav/Nav";
import OrganizationProvider from "@/component/organization/OrganizationProvider";
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
				<AuthProvider>
					<QueryProvider>
						<OrganizationProvider>
							<Nav>{children}</Nav>
						</OrganizationProvider>
					</QueryProvider>
				</AuthProvider>
			</body>
		</html>
	);
}
