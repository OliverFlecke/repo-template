"use client";

import { getOrganizationOptions } from "@/api/@tanstack/react-query.gen";
import { useQuery } from "@tanstack/react-query";

export default function List() {
	const { data, isLoading } = useQuery({
		...getOrganizationOptions(),
	});

	if (isLoading) {
		return <>Loading...</>;
	}

	if (!data) {
		return <></>;
	}

	return (
		<ul>
			{data.map((org) => (
				<li key={org.name}>{org.name}</li>
			))}
		</ul>
	);
}
