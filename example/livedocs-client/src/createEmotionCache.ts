// example/livedocs-client/src/createEmotionCache.ts
import createCache from '@emotion/cache';
import type { EmotionCache } from '@emotion/cache';

export function createEmotionCache(): EmotionCache {
  return createCache({ key: 'css' });
}
