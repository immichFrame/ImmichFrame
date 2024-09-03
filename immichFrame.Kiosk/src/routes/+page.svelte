<script lang="ts">
	import { GET } from '$lib/api';
</script>

<h1>Welcome to SvelteKit</h1>
<p>
	Visit <a href="https://kit.svelte.dev">kit.svelte.dev</a> to read the documentation
</p>
<h1>Test</h1>
{#await GET('/Asset')}
	<p>...waiting</p>
{:then { data }}
	{#if data}
		<div class="image">
			<p>{data.id}</p>
			{#await GET('/Asset/{id}', { params: { path: { id: data.id } } })}
				<p>...waiting</p>
			{:then { data }}
				{console.log(data)}
				{#if data}
					<!-- svelte-ignore a11y-img-redundant-alt -->
					<img src={data} alt="Dog image" />
				{/if}
			{:catch { error }}
				{console.log(error)}
				<p>An error occurred!{error}</p>
			{/await}
		</div>
	{/if}
{/await}
