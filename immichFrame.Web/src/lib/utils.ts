export const decodeBase64 = (data: string) => Uint8Array.from(atob(data), (c) => c.charCodeAt(0));

export const handlePromiseError = <T>(promise: Promise<T>): void => {
	promise.catch((error) => console.error(`[utils.ts]:handlePromiseError ${error}`, error));
};
