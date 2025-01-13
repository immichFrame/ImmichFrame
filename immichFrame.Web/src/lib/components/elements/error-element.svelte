<script lang="ts">
	import { authSecretStore } from '$lib/stores/persist.store';
	interface Props {
		message?: string;
		authError?: boolean;
	}

	let authSecret: string = $state('');
	function Authenticate() {
		if (!authSecret) return;
		authSecretStore.set(authSecret);
		location.reload();
	}

	let { message = '', authError: authError = false }: Props = $props();
</script>

<div
	class="place-self-center container flex flex-col items-center justify-center px-5 mx-auto my-8 space-y-8 text-center sm:max-w-md"
>
	<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512" class="w-40 h-40 text-gray-400">
		<path
			fill="currentColor"
			d="M256,16C123.452,16,16,123.452,16,256S123.452,496,256,496,496,388.548,496,256,388.548,16,256,16ZM403.078,403.078a207.253,207.253,0,1,1,44.589-66.125A207.332,207.332,0,0,1,403.078,403.078Z"
		></path>
		<rect width="176" height="32" x="168" y="320" fill="currentColor"></rect>
		<polygon
			fill="currentColor"
			points="210.63 228.042 186.588 206.671 207.958 182.63 184.042 161.37 162.671 185.412 138.63 164.042 117.37 187.958 141.412 209.329 120.042 233.37 143.958 254.63 165.329 230.588 189.37 251.958 210.63 228.042"
		></polygon>
		<polygon
			fill="currentColor"
			points="383.958 182.63 360.042 161.37 338.671 185.412 314.63 164.042 293.37 187.958 317.412 209.329 296.042 233.37 319.958 254.63 341.329 230.588 365.37 251.958 386.63 228.042 362.588 206.671 383.958 182.63"
		></polygon>
	</svg>
	<div class="text-3xl text-gray-400">
		{#if authError}
			<p>Could not authenticate client</p>
			<br />
			<input
				type="text"
				class="text-center bg-transparent border-gray-400 border-2"
				placeholder="add secret..."
				bind:value={authSecret}
			/>
			<button class="text-xl border-gray-400 border-2 rounded-md p-2 m-2" onclick={Authenticate}
				>Authenticate</button
			>
		{:else if message}
			<p>
				{message}
			</p>
		{:else}
			<p>
				Looks like your immich-server is offline or you misconfigured immichFrame, check the
				container logs
			</p>
		{/if}
	</div>
</div>
