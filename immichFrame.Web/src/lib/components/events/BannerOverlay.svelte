<script lang="ts">
	import type { FrameEvent, FrameEventAckStatus } from '$lib/events/event-service';

	let { event, onDismiss }: {
		event: FrameEvent;
		onDismiss: (status: FrameEventAckStatus) => void | Promise<void>;
	} = $props();

	let dismissing = false;

	const message = event.message ?? '';
	const allowTouchDismiss = event.input?.allowTouchDismiss ?? true;

	async function dismiss(status: FrameEventAckStatus = 'Dismissed') {
		if (dismissing) return;
		dismissing = true;
		try {
			await onDismiss(status);
		} finally {
			dismissing = false;
		}
	}

	function handleClick() {
		if (!allowTouchDismiss) return;
		void dismiss('Dismissed');
	}
</script>

<div
	class="pointer-events-none fixed inset-x-0 top-0 z-[160] flex justify-center p-4"
	role="presentation"
>
	<button
		type="button"
		class="banner-button pointer-events-auto w-[90vw] cursor-pointer rounded-2xl bg-white/80 px-8 py-6 text-center text-black shadow-2xl ring-1 ring-black/10 backdrop-blur transition hover:bg-white/90 motion-safe:animate-[bannerin_200ms_ease-out]"
		onclick={handleClick}
		aria-label="Notification: {message}"
	>
		<p class="banner-text whitespace-pre-line text-center leading-tight">{message}</p>
	</button>
</div>

<style>
	.banner-button {
		font-family:
			ui-sans-serif,
			system-ui,
			-apple-system,
			'Segoe UI',
			Roboto,
			'Helvetica Neue',
			Arial,
			'Apple Color Emoji',
			'Segoe UI Emoji',
			'Noto Color Emoji',
			sans-serif;
	}

	.banner-text {
		font-size: clamp(1.25rem, 3vw, 2.5rem);
		font-weight: 400;
		letter-spacing: -0.01em;
	}

	@keyframes bannerin {
		from {
			transform: translateY(-100%);
			opacity: 0;
		}
		to {
			transform: translateY(0);
			opacity: 1;
		}
	}
</style>
