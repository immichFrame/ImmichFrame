import { type AssetResponseDto, AssetTypeEnum } from '$lib/immichFrameApi';
export const isImageAsset = (asset: AssetResponseDto) => asset.type === AssetTypeEnum.Image;
export const isVideoAsset = (asset: AssetResponseDto) => asset.type === AssetTypeEnum.Video;
export const isSupportedAsset = (asset: AssetResponseDto) =>
    isImageAsset(asset) || isVideoAsset(asset);