<script lang="ts">
	import type { FrameEvent, FrameEventAckStatus } from '$lib/events/event-service';
	import PopupTextOverlay from './PopupTextOverlay.svelte';
	import BannerOverlay from './BannerOverlay.svelte';

	let {
		popupEvent = null,
		bannerEvent = null,
		dismissPopup,
		dismissBanner
	}: {
		popupEvent: FrameEvent | null;
		bannerEvent: FrameEvent | null;
		dismissPopup: (status: FrameEventAckStatus) => void | Promise<void>;
		dismissBanner: (status: FrameEventAckStatus) => void | Promise<void>;
	} = $props();
</script>

{#if popupEvent}
	{#key popupEvent.id}
		{#if popupEvent.mode === 'PopupText'}
			<PopupTextOverlay event={popupEvent} onDismiss={dismissPopup} />
		{/if}
	{/key}
{/if}

{#if bannerEvent}
	{#key bannerEvent.id}
		{#if bannerEvent.mode === 'Banner'}
			<BannerOverlay event={bannerEvent} onDismiss={dismissBanner} />
		{/if}
	{/key}
{/if}
