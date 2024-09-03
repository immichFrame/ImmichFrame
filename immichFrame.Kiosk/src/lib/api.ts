import type { paths } from '$lib/immichFrameApi';
import createClient from 'openapi-fetch';

export const { GET, POST, PATCH, PUT, DELETE, HEAD, TRACE } = createClient<paths>({
	baseUrl: 'https://localhost:7018'
	// headers: { authorization: `Bearer ${bearerToken}` }
});