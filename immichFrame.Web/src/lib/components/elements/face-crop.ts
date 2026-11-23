import type { AssetFaceResponseDto } from '$lib/immichFrameApi';

export function faceCropPosition(
	faces: AssetFaceResponseDto[],
	imageAspect: number,
	paneAspect: number
): string | undefined {
	if (!imageAspect || !paneAspect || imageAspect === paneAspect) return;

	const horizontal = imageAspect > paneAspect;
	const visible = horizontal ? paneAspect / imageAspect : imageAspect / paneAspect;
	let first = 1;
	let last = 0;

	for (const face of faces) {
		const start = horizontal ? face.boundingBoxX1 : face.boundingBoxY1;
		const end = horizontal ? face.boundingBoxX2 : face.boundingBoxY2;
		const size = horizontal ? face.imageWidth : face.imageHeight;
		if (start == null || end == null || !size || start < 0 || end > size || end <= start) {
			continue;
		}
		first = Math.min(first, start / size);
		last = Math.max(last, end / size);
	}

	if (first >= last) return;

	// Keep the centered crop when possible. When the faces cannot all fit,
	// center the crop on their combined bounds.
	const overflow = 1 - visible;
	const centered = overflow / 2;
	const offset =
		last - first <= visible
			? Math.min(first, Math.max(last - visible, centered))
			: (first + last - visible) / 2;
	const position = (Math.min(overflow, Math.max(0, offset)) / overflow) * 100;
	return horizontal ? `${position}% 50%` : `50% ${position}%`;
}
