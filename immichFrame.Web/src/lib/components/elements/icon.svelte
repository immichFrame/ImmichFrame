<script lang="ts">
	import type { AriaRole } from 'svelte/elements';

	
	interface Props {
		size?: string | number;
		color?: string;
		path: string;
		title?: string | null;
		desc?: string;
		flipped?: boolean;
		class?: string;
		viewBox?: string;
		role?: AriaRole;
		ariaHidden?: boolean | undefined;
		ariaLabel?: string | undefined;
		ariaLabelledby?: string | undefined;
	}

	let {
		size = '1em',
		color = 'currentColor',
		path,
		title = null,
		desc = '',
		flipped = false,
		class: className = '',
		viewBox = '0 0 24 24',
		role = 'img',
		ariaHidden = undefined,
		ariaLabel = undefined,
		ariaLabelledby = undefined
	}: Props = $props();
</script>

<svg
	{...size ? { width: size, height: size } : {}}
	{viewBox}
	class="{className} {flipped ? '-scale-x-100' : ''}"
	{role}
	aria-label={ariaLabel}
	aria-hidden={ariaHidden}
	aria-labelledby={ariaLabelledby}
>
	{#if title}
		<title>{title}</title>
	{/if}
	{#if desc}
		<desc>{desc}</desc>
	{/if}
	<path d={path} fill={color} />
</svg>
