<script lang="ts">
	import type { FrameEvent, FrameEventAckStatus } from '$lib/events/event-service';
	import { onMount } from 'svelte';

	let { event, onDismiss }: {
		event: FrameEvent;
		onDismiss: (status: FrameEventAckStatus) => void | Promise<void>;
	} = $props();

	let dismissing = false;

	const allowTouchDismiss = event.input?.allowTouchDismiss ?? true;
	const allowKeyboardDismiss = event.input?.allowKeyboardDismiss ?? true;

	const actions = event.actions?.length
		? event.actions
		: [{ id: 'close', label: 'Dismiss', kind: 'primary' }];

	const message = event.message ?? '';
	const timeoutMs = event.timeoutMs ?? 0;

	let secondsRemaining = $state(timeoutMs > 0 ? Math.ceil(timeoutMs / 1000) : 0);

	onMount(() => {
		if (timeoutMs <= 0) return;

		const interval = setInterval(() => {
			secondsRemaining = Math.max(0, secondsRemaining - 1);
		}, 1000);

		return () => clearInterval(interval);
	});

	async function dismiss(status: FrameEventAckStatus = 'Closed') {
		if (dismissing) return;
		dismissing = true;
		try {
			await onDismiss(status);
		} finally {
			dismissing = false;
		}
	}

	function handleBackdropPointerDown(e: PointerEvent) {
		if (!allowTouchDismiss) return;
		if (e.target === e.currentTarget) {
			void dismiss('Closed');
		}
	}

	function handleKey(e: KeyboardEvent) {
		if (!allowKeyboardDismiss) return;
		if (e.key === 'Escape') {
			e.preventDefault();
			void dismiss('Closed');
		}
	}
</script>

<svelte:window onkeydown={handleKey} />

<div
	class="popup-root absolute inset-0 z-[150] flex items-center justify-center bg-black/30 p-6 backdrop-blur-sm"
	role="presentation"
	onpointerdown={handleBackdropPointerDown}
>
	<div
		class="popup-card relative w-[80vw] max-w-[48rem] rounded-2xl bg-white/85 p-10 text-black shadow-2xl ring-1 ring-black/10 backdrop-blur motion-safe:animate-[popupin_180ms_ease-out] {allowTouchDismiss ? 'cursor-pointer' : ''}"
		role="dialog"
		aria-modal="true"
		aria-labelledby={event.title ? 'popup-text-title' : undefined}
		onpointerdown={(e) => {
			if (!allowTouchDismiss) return;
			if (e.target instanceof Element && e.target.closest('button')) return;
			void dismiss('Closed');
		}}
	>
		{#if secondsRemaining > 0}
			<span class="popup-countdown absolute right-5 top-4 tabular-nums text-black/40">{secondsRemaining}s</span>
		{/if}
		{#if event.title}
			<h2 id="popup-text-title" class="popup-title mb-4 text-center">{event.title}</h2>
		{/if}
		<p class="popup-message mb-8 whitespace-pre-line text-center">{message}</p>
		<div class="flex flex-wrap items-center justify-center gap-4">
			{#each actions as action}
				<button
					type="button"
					class={`popup-action rounded-full px-8 py-3 transition focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-offset-white ${
						action.kind === 'primary'
							? 'bg-black text-white hover:bg-neutral-800 focus:ring-black/40'
							: 'bg-black/10 text-black hover:bg-black/20 focus:ring-black/30'
					}`}
					onclick={() => void dismiss('Closed')}
				>
					{action.label}
				</button>
			{/each}
		</div>
	</div>
</div>

<style>
	.popup-root,
	.popup-card {
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

	.popup-title {
		font-size: clamp(1.5rem, 3.5vw, 2.75rem);
		font-weight: 600;
		letter-spacing: -0.015em;
	}

	.popup-message {
		font-size: clamp(1.125rem, 2.5vw, 2rem);
		font-weight: 400;
		line-height: 1.4;
	}

	.popup-action {
		font-size: clamp(1rem, 1.75vw, 1.5rem);
		font-weight: 500;
	}

	.popup-countdown {
		font-size: clamp(0.875rem, 1.25vw, 1.125rem);
	}

	@keyframes popupin {
		from {
			transform: scale(0.96);
			opacity: 0;
		}
		to {
			transform: scale(1);
			opacity: 1;
		}
	}
</style>
