<script lang="ts">
	import {onMount} from 'svelte';
	import {authSecretStore} from '$lib/stores/persist.store';
	import Editor from './crop-editor.svelte';
	type Photo={key:string;name:string};
	type Album={account:string;album:string;name:string};
	type Crop={x:number;y:number;width:number;height:number;turns:number;flipX:boolean;flipY:boolean};
	type Upload={name:string;status:string;target:string;asset?:string;receipt?:string};
	let {device,onauth}:{device:string;onauth:()=>void}=$props();
	let albums=$state<Album[]>([]), photos=$state<Photo[]>([]), total=$state(0), target=$state('');
	let status=$state<{supported:boolean;frame?:string;current?:string}>({supported:false});
	let edit=$state<{photo:Photo;image:Blob;crop:Crop}|null>(null);
	let message=$state(''), frameMessage=$state(''), uploads=$state<Upload[]>([]), working=$state(false);
	const controller=new AbortController();
	let alive=true;
	function auth(){return $authSecretStore?{Authorization:'Bearer '+$authSecretStore}:{} as Record<string,string>}
	async function api(path:string,opts:RequestInit={}){
		const r=await fetch(path,{...opts,signal:controller.signal,headers:{...auth(),...opts.headers}});
		if(r.status===401)onauth();
		if(!r.ok)throw Error((await r.text())||'Request failed.');
		return r;
	}
	async function list(offset=0){
		const data=await (await api('/api/gallery?offset='+offset)).json();
		albums=data.albums;total=data.total;photos=offset?[...photos,...data.photos]:data.photos;
		if(albums.length===1)target=albums[0].account+'/'+albums[0].album;
	}
	function remote(command:string,key?:string){return api('/api/controls/command',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({device,command,key})})}
	async function poll(){
		if(!device)return;
		try {status=await (await remote('status')).json();frameMessage=status.supported?'':'Install the gallery test app on this frame to select and crop photos.'}
		catch(e){status={supported:false};frameMessage=String(e)}
	}
	onMount(()=>{
		void list().catch(e=>message=String(e));
		let timer:ReturnType<typeof setTimeout>;
		async function tick(){await poll();if(alive)timer=setTimeout(tick,3000)}
		void tick();
		return ()=>{alive=false;clearTimeout(timer);controller.abort()};
	});
	async function jump(p:Photo){try{await remote('goto',p.key);await poll()}catch(e){message=String(e)}}
	async function openEditor(p:Photo){
		try {
			const base='/api/gallery/'+p.key;
			const [image,crop]=await Promise.all([api(base+'/image').then(r=>r.blob()),api(base+'/crop/'+status.frame).then(r=>r.json())]);
			edit={photo:p,image,crop};
		}catch(e){message=String(e)}
	}
	async function save(crop:Crop){
		if(!edit)return;
		const key=edit.photo.key;
		await api('/api/gallery/'+key+'/crop/'+status.frame,{method:'PUT',headers:{'Content-Type':'application/json'},body:JSON.stringify(crop)});
		await poll();if(status.current===key)await remote('goto',key);
		message='Frame crop saved.';
	}
	function thumbnail(node:HTMLImageElement,key:string){
		let url='',active=true;
		const observer=new IntersectionObserver(entries=>{
			if(!entries.some(e=>e.isIntersecting))return;observer.disconnect();
			api('/api/gallery/'+key+'/image?thumbnail=true').then(r=>r.blob()).then(blob=>{
				if(active){url=URL.createObjectURL(blob);node.src=url}
			}).catch(()=>{node.alt='Preview unavailable'});
		});observer.observe(node);
		return {destroy(){active=false;observer.disconnect();if(url)URL.revokeObjectURL(url)}};
	}
	async function attach(row:Upload){
		await api('/api/gallery/'+row.target+'/attach/'+row.asset,{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(row.receipt)});
		row.status='In album';row.receipt=undefined;await list();
	}
	async function upload(files:File[]){
		working=true;
		try{for(const file of files){
			uploads.push({name:file.name,status:'Starting…',target});const row=uploads[uploads.length-1];
			try{
				const result=await new Promise<{asset:string;receipt:string}>((resolve,reject)=>{
					const xhr=new XMLHttpRequest();xhr.open('POST','/api/gallery/'+row.target+'/upload');
					xhr.setRequestHeader('X-Frame-Upload','1');if($authSecretStore)xhr.setRequestHeader('Authorization','Bearer '+$authSecretStore);
					xhr.upload.onprogress=e=>{if(e.lengthComputable)row.status=Math.round(100*e.loaded/e.total)+'%'};
					xhr.onerror=()=>reject(Error('Upload connection failed.'));
					xhr.onload=()=>{if(xhr.status===401)onauth();if(xhr.status<200||xhr.status>=300)return reject(Error(xhr.responseText||'Upload failed'));try{resolve(JSON.parse(xhr.responseText))}catch(e){reject(e)}};
					const form=new FormData();form.append('file',file);xhr.send(form);
				});
				row.asset=result.asset;row.receipt=result.receipt;await attach(row);
			}catch(e){row.status=String(e)}
		}}finally{working=false}
	}
	let current=$derived(status.current ? photos.find(p=>p.key===status.current)??{key:status.current,name:'Current photo'} : null);
</script>
<section class="space-y-3 rounded-xl border p-4">
	<h2>Now showing</h2>
	{#if current}{#key current.key}
		<img class="max-h-52 rounded" alt={current.name} use:thumbnail={current.key}/>
		<button onclick={()=>openEditor(current!)}>Edit frame crop</button>
	{/key}{:else}<p>{frameMessage || (device?'Waiting for playback confirmation…':'Select a frame to see its current photo.')}</p>{/if}
	{#if edit}<Editor image={edit.image} initial={edit.crop} onsave={save} onclose={()=>edit=null}/>{/if}
	<h2>Up next <small>({total})</small></h2>
	{#if !albums.length}<p>Choose albums in the server account settings to populate this gallery.</p>{/if}
	<div class="grid grid-cols-2 gap-2 sm:grid-cols-4">
		{#each photos as p (p.key)}
			<div class="min-w-0 rounded border p-1" class:current={p.key===status.current}>
				<button class="w-full" disabled={!status.supported} title={'Show '+p.name} onclick={()=>jump(p)}>
					<img class="aspect-square w-full rounded object-cover" alt={p.name} use:thumbnail={p.key}/>
				</button>
				<button disabled={!status.supported} onclick={()=>openEditor(p)}>Edit crop</button>
			</div>
		{/each}
	</div>
	{#if photos.length<total}<button disabled={working} onclick={async()=>{working=true;try{await list(photos.length)}catch(e){message=String(e)}finally{working=false}}}>Load more</button>{/if}
	<h2>Upload to album</h2>
	<select aria-label="Upload destination album" bind:value={target} disabled={working}>
		<option value="">Select an album</option>{#each albums as a}<option value={a.account+'/'+a.album}>{a.name}</option>{/each}
	</select>
	<input aria-label="Upload images" disabled={!target||working} type="file" multiple accept="image/jpeg,image/png,image/gif,image/webp" onchange={e=>{const files=Array.from(e.currentTarget.files??[]);e.currentTarget.value='';void upload(files)}}/>
	{#each uploads as row}<p>{row.name} · {row.status}
		{#if row.receipt}<button onclick={()=>attach(row).catch(e=>row.status=String(e))}>Retry adding to album</button>{/if}</p>{/each}
	<p role="status">{message}</p>
</section>
<style>h2{font-size:1.15rem;font-weight:600}small{font-weight:400}button:disabled{opacity:.55}button:not(:disabled):hover{text-decoration:underline}.current{outline:2px solid currentColor}select{border:1px solid;border-radius:.5rem;padding:.45rem;max-width:100%}</style>
