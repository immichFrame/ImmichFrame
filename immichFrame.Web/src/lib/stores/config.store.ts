// import { writable } from 'svelte/store';

function createConfigStore() {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const config: any = fetchConfig();

  return {
    config
  };
}

const fetchConfig = async () => {
  console.log('fetching');
  const res = await fetch('../../../ImmichFrame.WebApi/settings.json');
  let config;
  if (res.ok) {
    config = await res.json();
    console.log(config);
  } else {
    console.error('Failed to load config file');
  }
};

export const configStore = createConfigStore();