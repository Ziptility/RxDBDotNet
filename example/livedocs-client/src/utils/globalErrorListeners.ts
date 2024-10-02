// src/utils/globalErrorListeners.ts

export const setupGlobalErrorListeners = (): void => {
  if (typeof window !== 'undefined') {
    window.addEventListener('unhandledrejection', (event: PromiseRejectionEvent) => {
      console.error('Unhandled promise rejection:', event.reason);
    });

    window.addEventListener('error', (event: ErrorEvent) => {
      console.error('Uncaught exception:', event.error);
    });
  }
};
