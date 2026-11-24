<script lang="ts">
	import {onMount,untrack} from 'svelte';
	type Crop = {x:number;y:number;width:number;height:number;turns:number;flipX:boolean;flipY:boolean};
	let {image,initial,onsave,onclose}:{image:Blob;initial:Crop;onsave:(crop:Crop)=>Promise<void>;onclose:()=>void}=$props();
	let canvas:HTMLCanvasElement;
	let source=$state<HTMLImageElement>(), crop=$state(untrack(()=>({...initial}))), ratio=$state('10:16'), zoom=$state(1);
	let busy=$state(false), error=$state('');
	let dimensions=$derived(source ? (crop.turns%2 ? [source.naturalHeight,source.naturalWidth] : [source.naturalWidth,source.naturalHeight]) : [1,1]);
	onMount(()=>{
		const url=URL.createObjectURL(image), img=new Image();
		img.onload=()=>{source=img;ratio=(crop.width*dimensions[0]/(crop.height*dimensions[1])).toFixed(4)+':1';};
		img.onerror=()=>error='Could not decode this image.';
		img.src=url;
		return ()=>{img.onload=null;URL.revokeObjectURL(url)};
	});
	function fit(){
		const [w,h]=ratio.split(':').map(Number);
		if(!(w>0&&h>0&&Number.isFinite(w/h))) {error='Enter a ratio such as 10:16.';return}
		error='';
		const aspect=w/h*dimensions[1]/dimensions[0];
		const width=Math.min(1,aspect)/zoom,height=Math.min(1,1/aspect)/zoom;
		crop={...crop,x:Math.max(0,Math.min(1-width,crop.x+(crop.width-width)/2)),y:Math.max(0,Math.min(1-height,crop.y+(crop.height-height)/2)),width,height};
	}
	$effect(()=>{
		if(!source||!canvas)return;
		const [w,h]=dimensions;
		const scale=Math.min(800/(crop.width*w),500/(crop.height*h));
		canvas.width=Math.max(1,Math.round(crop.width*w*scale));canvas.height=Math.max(1,Math.round(crop.height*h*scale));
		const ctx=canvas.getContext('2d');if(!ctx)return;
		ctx.scale(scale,scale);ctx.translate(-crop.x*w+w/2,-crop.y*h+h/2);
		ctx.scale(crop.flipX?-1:1,crop.flipY?-1:1);ctx.rotate(crop.turns*Math.PI/2);
		ctx.drawImage(source,-source.naturalWidth/2,-source.naturalHeight/2);
	});
	let last:{x:number;y:number}|null=null;
	function move(e:PointerEvent){
		if(!last)return;
		const rect=canvas.getBoundingClientRect();
		crop.x=Math.max(0,Math.min(1-crop.width,crop.x-(e.clientX-last.x)/rect.width*crop.width));
		crop.y=Math.max(0,Math.min(1-crop.height,crop.y-(e.clientY-last.y)/rect.height*crop.height));
		last={x:e.clientX,y:e.clientY};
	}
	async function save(){
		busy=true;try{await onsave({...crop});onclose()}catch(e){error=String(e)}finally{busy=false}
	}
</script>
<section class="rounded-xl border p-4">
	<h3>Frame crop</h3>
	<div class="my-3 flex min-h-32 justify-center bg-black">
		<canvas aria-label="Drag photo to adjust crop" bind:this={canvas} style="max-width:100%;max-height:45vh;object-fit:contain;align-self:center;touch-action:none;cursor:grab"
			onpointerdown={(e)=>{canvas.setPointerCapture(e.pointerId);last={x:e.clientX,y:e.clientY}}}
			onpointermove={move} onlostpointercapture={()=>last=null}></canvas>
	</div>
	<div class="flex flex-wrap items-center gap-3">
		<label>Ratio <input class="w-24 border" bind:value={ratio} onchange={fit}/></label>
		<label>Zoom <input type="range" min="1" max="8" step=".05" bind:value={zoom} oninput={(e)=>{zoom=+e.currentTarget.value;fit()}}/></label>
		<button onclick={()=>{crop.turns=(crop.turns+3)%4;fit()}}>↶ Left</button>
		<button onclick={()=>{crop.turns=(crop.turns+1)%4;fit()}}>↷ Right</button>
		<button aria-pressed={crop.flipX} onclick={()=>crop.flipX=!crop.flipX}>↔ Flip horizontal</button>
		<button aria-pressed={crop.flipY} onclick={()=>crop.flipY=!crop.flipY}>↕ Flip vertical</button>
		<button onclick={()=>{crop={x:0,y:0,width:1,height:1,turns:0,flipX:false,flipY:false};zoom=1;ratio=dimensions[0]+':'+dimensions[1];error=''}}>Reset</button>
		<button onclick={save} disabled={busy||!source||!!error}>Save crop</button><button onclick={onclose}>Cancel</button>
	</div>
	{#if error}<p role="alert">{error}</p>{/if}
</section>
<style>button{border:1px solid currentColor;border-radius:.6rem;padding:.4rem .65rem}button[aria-pressed=true]{font-weight:bold}</style>
