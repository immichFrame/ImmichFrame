/* eslint-disable @typescript-eslint/no-explicit-any */
/**
 * ImmichFrame.WebApi
 * 1.0
 * DO NOT MODIFY - This file has been generated using oazapfts.
 * See https://www.npmjs.com/package/oazapfts
 */
import * as Oazapfts from "@oazapfts/runtime";
import * as QS from "@oazapfts/runtime/query";
export const defaults: Oazapfts.Defaults<Oazapfts.CustomHeaders> = {
    headers: {},
    baseUrl: "/",
};
const oazapfts = Oazapfts.runtime(defaults);
export const servers = {};
export type ExifResponseDto = {
    city?: string | null;
    country?: string | null;
    dateTimeOriginal?: string | null;
    description?: string | null;
    exifImageHeight?: number | null;
    exifImageWidth?: number | null;
    exposureTime?: string | null;
    fNumber?: number | null;
    fileSizeInByte?: number | null;
    focalLength?: number | null;
    iso?: number | null;
    latitude?: number | null;
    lensModel?: string | null;
    longitude?: number | null;
    make?: string | null;
    model?: string | null;
    modifyDate?: string | null;
    orientation?: string | null;
    projectionType?: string | null;
    rating?: number | null;
    state?: string | null;
    timeZone?: string | null;
    additionalProperties?: {
        [key: string]: any | null;
    } | null;
};
export type UserAvatarColor = 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9;
export type UserResponseDto = {
    avatarColor: UserAvatarColor;
    email: string;
    id: string;
    name: string;
    profileChangedAt: string;
    profileImagePath: string;
    additionalProperties?: {
        [key: string]: any | null;
    } | null;
};
export type SourceType = 0 | 1 | 2;
export type AssetFaceWithoutPersonResponseDto = {
    boundingBoxX1?: number;
    boundingBoxX2?: number;
    boundingBoxY1?: number;
    boundingBoxY2?: number;
    id: string;
    imageHeight?: number;
    imageWidth?: number;
    sourceType?: SourceType;
    additionalProperties?: {
        [key: string]: any | null;
    } | null;
};
export type PersonWithFacesResponseDto = {
    birthDate?: string | null;
    color?: string | null;
    faces: AssetFaceWithoutPersonResponseDto[];
    id: string;
    isFavorite?: boolean | null;
    isHidden?: boolean;
    name: string;
    thumbnailPath: string;
    updatedAt?: string | null;
    additionalProperties?: {
        [key: string]: any | null;
    } | null;
};
export type AssetStackResponseDto = {
    assetCount?: number;
    id: string;
    primaryAssetId: string;
    additionalProperties?: {
        [key: string]: any | null;
    } | null;
};
export type TagResponseDto = {
    color?: string | null;
    createdAt: string;
    id: string;
    name: string;
    parentId?: string | null;
    updatedAt: string;
    value: string;
    additionalProperties?: {
        [key: string]: any | null;
    } | null;
};
export type AssetTypeEnum = 0 | 1 | 2 | 3;
export type AssetVisibility = 0 | 1 | 2 | 3;
export type AssetResponseDto = {
    immichServerUrl?: string | null;
    checksum: string;
    deviceAssetId: string;
    deviceId: string;
    duplicateId?: string | null;
    duration: string;
    exifInfo?: ExifResponseDto;
    fileCreatedAt: string;
    fileModifiedAt: string;
    hasMetadata?: boolean;
    id: string;
    isArchived?: boolean;
    isFavorite?: boolean;
    isOffline?: boolean;
    isTrashed?: boolean;
    libraryId?: string | null;
    livePhotoVideoId?: string | null;
    localDateTime: string;
    originalFileName: string;
    originalMimeType?: string | null;
    originalPath: string;
    owner?: UserResponseDto;
    ownerId: string;
    people?: PersonWithFacesResponseDto[] | null;
    resized?: boolean | null;
    stack?: AssetStackResponseDto;
    tags?: TagResponseDto[] | null;
    thumbhash?: string | null;
    "type": AssetTypeEnum;
    unassignedFaces?: AssetFaceWithoutPersonResponseDto[] | null;
    updatedAt: string;
    visibility: AssetVisibility;
    additionalProperties?: {
        [key: string]: any | null;
    } | null;
};
export type AlbumUserRole = 0 | 1;
export type AlbumUserResponseDto = {
    role: AlbumUserRole;
    user: UserResponseDto;
    additionalProperties?: {
        [key: string]: any | null;
    } | null;
};
export type AssetOrder = 0 | 1;
export type AlbumResponseDto = {
    albumName: string;
    albumThumbnailAssetId?: string | null;
    albumUsers: AlbumUserResponseDto[];
    assetCount?: number;
    assets: AssetResponseDto[];
    createdAt: string;
    description: string;
    endDate?: string | null;
    hasSharedLink?: boolean;
    id: string;
    isActivityEnabled?: boolean;
    lastModifiedAssetTimestamp?: string | null;
    order?: AssetOrder;
    owner: UserResponseDto;
    ownerId: string;
    shared?: boolean;
    startDate?: string | null;
    updatedAt: string;
    additionalProperties?: {
        [key: string]: any | null;
    } | null;
};
export type ImageResponse = {
    randomImageBase64: string | null;
    thumbHashImageBase64: string | null;
    photoDate: string | null;
    imageLocation: string | null;
};
export type IAppointment = {
    startTime?: string;
    duration?: string;
    endTime?: string;
    summary?: string | null;
    description?: string | null;
    location?: string | null;
};
export type ClientSettingsDto = {
    interval?: number;
    transitionDuration?: number;
    downloadImages?: boolean;
    renewImagesDuration?: number;
    showClock?: boolean;
    clockFormat?: string | null;
    clockDateFormat?: string | null;
    showPhotoDate?: boolean;
    showProgressBar?: boolean;
    photoDateFormat?: string | null;
    showImageDesc?: boolean;
    showPeopleDesc?: boolean;
    showAlbumName?: boolean;
    showImageLocation?: boolean;
    imageLocationFormat?: string | null;
    primaryColor?: string | null;
    secondaryColor?: string | null;
    style?: string | null;
    baseFontSize?: string | null;
    showWeatherDescription?: boolean;
    weatherIconUrl?: string | null;
    imageZoom?: boolean;
    imagePan?: boolean;
    imageFill?: boolean;
    layout?: string | null;
    language?: string | null;
};
export type IWeather = {
    location?: string | null;
    temperature?: number;
    unit?: string | null;
    temperatureUnit?: string | null;
    description?: string | null;
    iconId?: string | null;
};
export function getAsset({ clientIdentifier }: {
    clientIdentifier?: string;
} = {}, opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchJson<{
        status: 200;
        data: AssetResponseDto[];
    }>(`/api/Asset${QS.query(QS.explode({
        clientIdentifier
    }))}`, {
        ...opts
    });
}
export function getAssetInfo(id: string, { clientIdentifier }: {
    clientIdentifier?: string;
} = {}, opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchJson<{
        status: 200;
        data: AssetResponseDto;
    }>(`/api/Asset/${encodeURIComponent(id)}/AssetInfo${QS.query(QS.explode({
        clientIdentifier
    }))}`, {
        ...opts
    });
}
export function getAlbumInfo(id: string, { clientIdentifier }: {
    clientIdentifier?: string;
} = {}, opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchJson<{
        status: 200;
        data: AlbumResponseDto[];
    }>(`/api/Asset/${encodeURIComponent(id)}/AlbumInfo${QS.query(QS.explode({
        clientIdentifier
    }))}`, {
        ...opts
    });
}
export function getImage(id: string, { clientIdentifier }: {
    clientIdentifier?: string;
} = {}, opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchBlob<{
        status: 200;
        data: Blob;
    }>(`/api/Asset/${encodeURIComponent(id)}/Image${QS.query(QS.explode({
        clientIdentifier
    }))}`, {
        ...opts
    });
}
export function getRandomImageAndInfo({ clientIdentifier }: {
    clientIdentifier?: string;
} = {}, opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchJson<{
        status: 200;
        data: ImageResponse;
    }>(`/api/Asset/RandomImageAndInfo${QS.query(QS.explode({
        clientIdentifier
    }))}`, {
        ...opts
    });
}
export function getAppointments({ clientIdentifier }: {
    clientIdentifier?: string;
} = {}, opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchJson<{
        status: 200;
        data: IAppointment[];
    }>(`/api/Calendar${QS.query(QS.explode({
        clientIdentifier
    }))}`, {
        ...opts
    });
}
export function getConfig({ clientIdentifier }: {
    clientIdentifier?: string;
} = {}, opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchJson<{
        status: 200;
        data: ClientSettingsDto;
    }>(`/api/Config${QS.query(QS.explode({
        clientIdentifier
    }))}`, {
        ...opts
    });
}
export function getVersion(opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchJson<{
        status: 200;
        data: string;
    }>("/api/Config/Version", {
        ...opts
    });
}
export function getWeather({ clientIdentifier }: {
    clientIdentifier?: string;
} = {}, opts?: Oazapfts.RequestOpts) {
    return oazapfts.fetchJson<{
        status: 200;
        data: IWeather;
    }>(`/api/Weather${QS.query(QS.explode({
        clientIdentifier
    }))}`, {
        ...opts
    });
}
