export const decodeBase64 = (data: string) => Uint8Array.from(atob(data), (c) => c.charCodeAt(0));

export const handlePromiseError = <T>(promise: Promise<T>): void => {
	promise.catch((error) => console.error(`[utils.ts]:handlePromiseError ${error}`, error));
};

export const getPositionClasses = (position: string): string => {
	const [vertical, horizontal] = position.split('-');
	
	const verticalClass = vertical === 'top' ? 'top-0' : 'bottom-0';
	
	let horizontalClass = '';
	switch (horizontal) {
		case 'left':
			horizontalClass = 'left-0';
			break;
		case 'center':
			horizontalClass = 'left-1/2 transform -translate-x-1/2';
			break;
		case 'right':
			horizontalClass = 'right-0';
			break;
		default:
			horizontalClass = 'left-0';
	}
	
	return `${verticalClass} ${horizontalClass}`;
};

export const getTextAlignment = (position: string): string => {
	if (position.endsWith('-right')) {
		return 'text-right';
	} else if (position.endsWith('-center')) {
		return 'text-center';
	} else {
		return 'text-left';
	}
};
