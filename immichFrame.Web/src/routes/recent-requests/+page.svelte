<script lang="ts">
	import { onMount } from 'svelte';
	import { getAssetStreamUrl, init } from '$lib/index';

	type RecentAssetRequest = {
		assetId: string;
		clientIdentifier: string;
		requestedAtUtc: string;
		endpoint?: string | null;
		assetType?: string | null;
		originalFileName?: string | null;
		takenAtUtc?: string | null;
		location?: string | null;
		city?: string | null;
		state?: string | null;
		country?: string | null;
	};

	init();

	let isLoading = true;
	let errorMessage = '';
	let recentRequests: RecentAssetRequest[] = [];
	let selectedRequest: RecentAssetRequest | null = null;

	const loadRecentRequests = async () => {
		try {
			isLoading = true;
			errorMessage = '';
			const response = await fetch('/api/AssetRequests/RecentAssetRequests?limit=10');
			if (!response.ok) {
				throw new Error(`Request failed with status ${response.status}`);
			}

			recentRequests = await response.json();
		} catch (error) {
			errorMessage = error instanceof Error ? error.message : 'Failed to load recent asset requests.';
		} finally {
			isLoading = false;
		}
	};

	onMount(loadRecentRequests);

	const closeModal = () => {
		selectedRequest = null;
	};

	const formatDateTime = (value?: string | null) => {
		if (!value) return 'Unknown';

		return new Intl.DateTimeFormat(undefined, {
			dateStyle: 'medium',
			timeStyle: 'short'
		}).format(new Date(value));
	};
</script>

<svelte:head>
	<title>Recent Asset Requests</title>
</svelte:head>

<div class="recent-requests-page">
	<header class="page-header">
		<div>
			<p class="eyebrow">Asset Requests</p>
			<h1>Recent Asset Requests</h1>
			<p class="subtitle">Press previews to view info.</p>
		</div>
		<button class="refresh-button" type="button" on:click={loadRecentRequests} disabled={isLoading}>
			{isLoading ? 'Refreshing...' : 'Refresh'}
		</button>
	</header>

	{#if isLoading}
		<p class="status-panel">Loading recent asset requests...</p>
	{:else if errorMessage}
		<p class="status-panel error">{errorMessage}</p>
	{:else if recentRequests.length === 0}
		<p class="status-panel">No asset requests have been recorded yet.</p>
	{:else}
		<div class="request-grid">
			{#each recentRequests as request}
				<button class="request-card" type="button" on:click={() => (selectedRequest = request)}>
					<img
						class="request-thumbnail"
						src={getAssetStreamUrl(request.assetId, request.clientIdentifier, 0)}
						alt={request.originalFileName ?? request.assetId}
						loading="lazy"
					/>
				</button>
			{/each}
		</div>
	{/if}
</div>

{#if selectedRequest}
	<button class="modal-backdrop" type="button" aria-label="Close details" on:click={closeModal}></button>
	<div class="modal-card" role="dialog" aria-modal="true" aria-labelledby="request-detail-title">
		<div class="modal-image-wrap">
			<img
				class="modal-image"
				src={getAssetStreamUrl(selectedRequest.assetId, selectedRequest.clientIdentifier, 0)}
				alt={selectedRequest.originalFileName ?? selectedRequest.assetId}
			/>
		</div>
		<div class="modal-content">
			<div class="modal-header">
				<div>
					<p class="eyebrow">Asset Detail</p>
					<h2 id="request-detail-title">{selectedRequest.originalFileName ?? selectedRequest.assetId}</h2>
				</div>
				<button class="close-button" type="button" on:click={closeModal}>Close</button>
			</div>

			<dl class="detail-list">
				<div>
					<dt>Taken</dt>
					<dd>{formatDateTime(selectedRequest.takenAtUtc)}</dd>
				</div>
				<div>
					<dt>Location</dt>
					<dd>{selectedRequest.location || 'Unknown'}</dd>
				</div>
				<div>
					<dt>Requested</dt>
					<dd>{formatDateTime(selectedRequest.requestedAtUtc)}</dd>
				</div>
				<div>
					<dt>Client</dt>
					<dd>{selectedRequest.clientIdentifier}</dd>
				</div>
			</dl>
		</div>
	</div>
{/if}

<style>
	.recent-requests-page {
		min-height: 100vh;
		padding: 2rem;
		background:
			radial-gradient(circle at top left, rgba(245, 222, 179, 0.28), transparent 35%),
			linear-gradient(180deg, #18120a 0%, #090909 100%);
		color: #f4efe4;
	}

	.page-header {
		margin: 0 auto 2rem;
		max-width: 72rem;
		display: flex;
		justify-content: space-between;
		align-items: flex-start;
		gap: 1rem;
	}

	.eyebrow {
		margin: 0 0 0.25rem;
		font-size: 0.78rem;
		letter-spacing: 0.16em;
		text-transform: uppercase;
		color: #d4b784;
	}

	h1 {
		margin: 0;
		font-size: clamp(2rem, 4vw, 3.5rem);
		line-height: 0.95;
	}

	.subtitle {
		margin-top: 0.75rem;
		max-width: 40rem;
		color: rgba(244, 239, 228, 0.72);
	}

	.refresh-button {
		border: 1px solid rgba(212, 183, 132, 0.45);
		background: rgba(212, 183, 132, 0.08);
		color: #d4b784;
		padding: 0.7rem 1rem;
		border-radius: 999px;
		cursor: pointer;
		font-weight: 700;
		white-space: nowrap;
		align-self: flex-start;
		margin-left: auto;
	}

	.refresh-button:disabled {
		opacity: 0.6;
		cursor: default;
	}

	.status-panel {
		max-width: 72rem;
		margin: 0 auto;
		padding: 1rem 1.25rem;
		border: 1px solid rgba(255, 255, 255, 0.12);
		border-radius: 1rem;
		background: rgba(255, 255, 255, 0.06);
	}

	.status-panel.error {
		color: #ffb4b4;
	}

	.request-grid {
		display: grid;
		grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
		gap: 1rem;
		max-width: 72rem;
		margin: 0 auto;
	}

	.request-card {
		border: 0;
		padding: 0;
		text-align: left;
		border-radius: 1.25rem;
		overflow: hidden;
		cursor: pointer;
		background: rgba(255, 255, 255, 0.07);
		box-shadow: 0 18px 40px rgba(0, 0, 0, 0.28);
		transition:
			transform 0.18s ease,
			box-shadow 0.18s ease;
	}

	.request-card:hover {
		transform: translateY(-3px);
		box-shadow: 0 24px 48px rgba(0, 0, 0, 0.34);
	}

	.request-thumbnail {
		display: block;
		width: 100%;
		aspect-ratio: 1 / 1;
		object-fit: cover;
		background: rgba(255, 255, 255, 0.04);
	}

	.modal-backdrop {
		position: fixed;
		inset: 0;
		background: rgba(0, 0, 0, 0.62);
		border: 0;
	}

	.modal-card {
		position: fixed;
		inset: 50% auto auto 50%;
		transform: translate(-50%, -50%);
		width: min(92vw, 56rem);
		max-height: 88vh;
		overflow: auto;
		display: grid;
		grid-template-columns: minmax(0, 1.2fr) minmax(260px, 0.8fr);
		background: #111111;
		border: 1px solid rgba(255, 255, 255, 0.12);
		border-radius: 1.4rem;
		box-shadow: 0 36px 80px rgba(0, 0, 0, 0.42);
	}

	.modal-image-wrap {
		background: #050505;
	}

	.modal-image {
		display: block;
		width: 100%;
		height: 100%;
		min-height: 20rem;
		object-fit: cover;
	}

	.modal-content {
		padding: 1.4rem;
	}

	.modal-header {
		display: flex;
		justify-content: space-between;
		gap: 1rem;
		align-items: start;
	}

	h2 {
		margin: 0;
		font-size: 1.35rem;
		color: #f4efe4;
	}

	.close-button {
		border: 1px solid rgba(255, 255, 255, 0.14);
		background: transparent;
		color: #d4b784;
		padding: 0.6rem 0.9rem;
		border-radius: 999px;
		cursor: pointer;
	}

	.detail-list {
		margin: 1.4rem 0 0;
		display: grid;
		gap: 1rem;
	}

	.detail-list div {
		padding-top: 1rem;
		border-top: 1px solid rgba(255, 255, 255, 0.08);
	}

	dt {
		font-size: 0.78rem;
		text-transform: uppercase;
		letter-spacing: 0.14em;
		color: #d4b784;
	}

	dd {
		margin: 0.3rem 0 0;
		font-size: 1rem;
		color: rgba(244, 239, 228, 0.9);
	}

	@media (max-width: 840px) {
		.recent-requests-page {
			padding: 1rem;
		}

		.request-grid {
			grid-template-columns: repeat(3, minmax(0, 1fr));
			gap: 0.75rem;
		}

		.modal-card {
			grid-template-columns: 1fr;
		}
	}

	@media (max-width: 480px) {
		.page-header {
			gap: 0.75rem;
		}

		.refresh-button {
			padding: 0.6rem 0.85rem;
		}

		.request-grid {
			grid-template-columns: repeat(2, minmax(0, 1fr));
		}
	}
</style>
