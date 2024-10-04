// example/livedocs-client/src/hooks/useOnlineStatus.ts
import { useState, useEffect } from 'react';

export function useOnlineStatus(): boolean {
  const [isOnline, setIsOnline] = useState<boolean>(true);

  useEffect(() => {
    const updateOnlineStatus = (): void => setIsOnline(navigator.onLine);

    if (typeof window !== 'undefined') {
      setIsOnline(navigator.onLine);
      window.addEventListener('online', updateOnlineStatus);
      window.addEventListener('offline', updateOnlineStatus);
    }

    return () => {
      if (typeof window !== 'undefined') {
        window.removeEventListener('online', updateOnlineStatus);
        window.removeEventListener('offline', updateOnlineStatus);
      }
    };
  }, []);

  return isOnline;
}
