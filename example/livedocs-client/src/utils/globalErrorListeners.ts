// src/utils/globalErrorListeners.ts
import { errorSubject, handleError, ErrorType } from './errorHandling';

export const setupGlobalErrorListeners = (): void => {
  if (typeof window !== 'undefined') {
    window.addEventListener('unhandledrejection', (event: PromiseRejectionEvent) => {
      console.error('Unhandled promise rejection:', event.reason);

      const error = event.reason instanceof Error ? event.reason : new Error(String(event.reason));
      handleError(error, 'Unhandled Promise Rejection');
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
        Column: ${errorDetails.colno}`;

      errorSubject.next({
        message: formattedMessage,
        type: ErrorType.UNKNOWN,
      });
    });
  }
};
