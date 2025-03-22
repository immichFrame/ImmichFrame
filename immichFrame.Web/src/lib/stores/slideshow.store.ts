import { writable } from 'svelte/store';

export enum SlideshowState {
  PlaySlideshow = 'play-slideshow',
  StopSlideshow = 'stop-slideshow',
  None = 'none',
}


function createSlideshowStore() {
  const restartState = writable<boolean>(false);
  const stopState = writable<boolean>(false);

  const slideshowState = writable<SlideshowState>(SlideshowState.None);
  const instantTransition = writable<boolean>(false);

  return {
    restartProgress: {
      subscribe: restartState.subscribe,
      set: (value: boolean) => {
        // Trigger an action whenever the restartProgress is set to true. Automatically
        // reset the restart state after that
        if (value) {
          restartState.set(true);
          restartState.set(false);
        }
      },
    },
    stopProgress: {
      subscribe: stopState.subscribe,
      set: (value: boolean) => {
        // Trigger an action whenever the stopProgress is set to true. Automatically
        // reset the stop state after that
        if (value) {
          stopState.set(true);
          stopState.set(false);
        }
      },
    },
    slideshowState,
    instantTransition,
  };
}

export const slideshowStore = createSlideshowStore();
