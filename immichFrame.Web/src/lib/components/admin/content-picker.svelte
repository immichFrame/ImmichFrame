<script lang="ts">
	import { Button } from '@immich/ui';
	import * as adminApi from '$lib/services/admin';
	import type { ServerAccountSettings } from '$lib/immichFrameApi';
	import ItemPicker, { type PickerItem } from './item-picker.svelte';

	interface Props {
		account: ServerAccountSettings;
		accountIndex: number;
	}

	let { account, accountIndex }: Props = $props();

	type Section = 'albums' | 'excludedAlbums' | 'people' | 'tags';
	const sections: { id: Section; label: string }[] = [
		{ id: 'albums', label: 'Albums' },
		{ id: 'excludedAlbums', label: 'Albums to hide' },
		{ id: 'people', label: 'People' },
		{ id: 'tags', label: 'Tags' }
	];

	let section: Section = $state('albums');

	function problem(res: { status: number; data: unknown }, what: string) {
		return (res.data as { detail?: string } | null)?.detail ?? `Could not load ${what}.`;
	}

	async function loadAlbums() {
		const res = await adminApi.getAlbums(accountIndex);
		if (res.status != 200) return { error: problem(res, 'albums') };
		return {
			items: res.data.map(
				(a): PickerItem => ({
					key: a.id!,
					name: a.name || 'Untitled album',
					subtitle: `${a.assetCount ?? 0} ${a.assetCount === 1 ? 'photo' : 'photos'}${a.shared ? ' · shared' : ''}`,
					loadImage: a.thumbnailAssetId
						? () => adminApi.fetchAssetThumbnail(accountIndex, a.thumbnailAssetId!)
						: undefined
				})
			)
		};
	}

	async function loadPeople() {
		const res = await adminApi.getPeople(accountIndex);
		if (res.status != 200) return { error: problem(res, 'people') };
		return {
			items: res.data.map(
				(p): PickerItem => ({
					key: p.id!,
					name: p.name ?? '',
					loadImage: () => adminApi.fetchPersonThumbnail(accountIndex, p.id!)
				})
			)
		};
	}

	async function loadTags() {
		const res = await adminApi.getTags(accountIndex);
		if (res.status != 200) return { error: problem(res, 'tags') };
		return { items: res.data.map((t): PickerItem => ({ key: t.value!, name: t.value! })) };
	}

	const plural = (n: number, one: string, many: string) => `${n} ${n === 1 ? one : many}`;
</script>

<div class="flex flex-col gap-6">
	<div class="flex flex-wrap gap-2" role="tablist" aria-label="What to pick">
		{#each sections as s (s.id)}
			<Button
				role="tab"
				aria-selected={section === s.id}
				size="small"
				variant={section === s.id ? 'filled' : 'outline'}
				color={section === s.id ? 'primary' : 'secondary'}
				onclick={() => (section = s.id)}
			>
				{s.label}
			</Button>
		{/each}
	</div>

	{#key section}
		{#if section === 'albums'}
			<ItemPicker
				title="Albums"
				noun="albums"
				emptyText="No albums picked, so photos aren't limited to any album. Tap an album to pick it."
				pickedText={(n) =>
					`Showing photos from ${plural(n, 'album', 'albums')}. Tap an album to add or remove it.`}
				load={loadAlbums}
				selected={account.albums ?? []}
				onchange={(keys) => (account.albums = keys)}
			/>
		{:else if section === 'excludedAlbums'}
			<ItemPicker
				title="Albums to hide"
				noun="albums"
				emptyText="No albums are hidden. Photos from any album you pick here will never be shown."
				pickedText={(n) =>
					`Never showing photos from ${plural(n, 'album', 'albums')}. Tap an album to show it again.`}
				load={loadAlbums}
				selected={account.excludedAlbums ?? []}
				onchange={(keys) => (account.excludedAlbums = keys)}
			/>
		{:else if section === 'people'}
			<ItemPicker
				title="People"
				noun="people"
				variant="round-tile"
				emptyText="No people picked, so photos aren't limited to anyone. Tap a face to only show photos of them."
				pickedText={(n) =>
					`Showing photos of ${plural(n, 'person', 'people')}. Tap a face to add or remove them.`}
				load={loadPeople}
				selected={account.people ?? []}
				onchange={(keys) => (account.people = keys)}
			/>
		{:else}
			<ItemPicker
				title="Tags"
				noun="tags"
				variant="chip"
				emptyText="No tags picked, so photos aren't limited by tag. Tap a tag to pick it."
				pickedText={(n) =>
					`Showing photos with ${plural(n, 'tag', 'tags')}. Tap a tag to add or remove it.`}
				load={loadTags}
				selected={account.tags ?? []}
				onchange={(keys) => (account.tags = keys)}
			/>
		{/if}
	{/key}
</div>
