import type { AssetResponseDto } from '$lib/immichFrameApi';
export const AssetType = {
    IMAGE: 0,
    VIDEO: 1,
    AUDIO: 2,
    OTHER: 3
} as const;
export const isImageAsset = (asset: AssetResponseDto) => asset.type === AssetType.IMAGE;
export const isVideoAsset = (asset: AssetResponseDto) => asset.type === AssetType.VIDEO;
export const isSupportedAsset = (asset: AssetResponseDto) =>
    isImageAsset(asset) || isVideoAsset(asset);