<script lang="ts">
	import { themeManager, TooltipProvider } from '@immich/ui';
	import { onMount } from 'svelte';
	import AdminPage from '$lib/components/admin/admin-page.svelte';

	onMount(() => themeManager.setPreference(themeManager.preference));
</script>

<svelte:head>
	<title>ImmichFrame Admin</title>
</svelte:head>

<TooltipProvider>
	<AdminPage />
</TooltipProvider>
