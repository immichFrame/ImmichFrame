// place files you want to import through the `$lib` alias in this folder.
import { defaults } from './immichFrameApi.js';

export * from './immichFrameApi.js';

export interface InitOptions {
  baseUrl: string;
  apiKey: string;
}

export const init = ({ baseUrl, apiKey }: InitOptions) => {
  setBaseUrl(baseUrl);
  setApiKey(apiKey);
};

export const getBaseUrl = () => defaults.baseUrl;

export const setBaseUrl = (baseUrl: string) => {
  defaults.baseUrl = baseUrl;
};

export const setApiKey = (apiKey: string) => {
  defaults.headers = defaults.headers || {};
  defaults.headers['x-api-key'] = apiKey;
};