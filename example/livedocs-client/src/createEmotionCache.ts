// src\createEmotionCache.ts
import createCache from '@emotion/cache';
import type { EmotionCache } from '@emotion/cache';

export default function createEmotionCache(): EmotionCache {
  return createCache({ key: 'css' });
}
