// src/utils/globalErrorListeners.ts
import { errorSubject } from './errorHandling';

export const setupGlobalErrorListeners = (): void => {
  if (typeof window !== 'undefined') {
    window.addEventListener('unhandledrejection', (event: PromiseRejectionEvent) => {
      console.error('Unhandled promise rejection:', event.reason);
      errorSubject.next({
        message: `Unhandled promise rejection: ${event.reason}`,
        severity: 'error',
      });
    });

    window.addEventListener('error', (event: ErrorEvent) => {
      const errorDetails = {
        message: event.message,
        filename: event.filename,
        lineno: event.lineno,
        colno: event.colno,
        error: event.error instanceof Error ? event.error : new Error(String(event.error)),
      };

      console.error('Uncaught exception:', errorDetails);

      const formattedMessage = `Uncaught exception:
        Message: ${errorDetails.message}
        File: ${errorDetails.filename}
        Line: ${errorDetails.lineno}
        Column: ${errorDetails.colno}
        Stack: ${errorDetails.error.stack}`;

      errorSubject.next({
        message: formattedMessage,
        severity: 'error',
      });
    });
  }
};
