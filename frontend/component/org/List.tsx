"use client";

import { getOrganizationsAdminOptions } from "@/api/@tanstack/react-query.gen";
import { useQuery } from "@tanstack/react-query";

export default function List() {
	const { data, isLoading } = useQuery({
		...getOrganizationsAdminOptions(),
	});

	if (isLoading) {
		return <>Loading...</>;
	}

	if (!data) {
		return <></>;
	}

	return (
		<ul>
			{data.data.map((org) => (
				<li key={org.name}>{org.name}</li>
			))}
		</ul>
	);
}
