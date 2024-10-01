// src/utils/globalErrorListeners.ts

import { v4 as uuidv4 } from 'uuid';
import { errorSubject, ErrorType } from './errorHandling';

/**
 * Sets up global error listeners for unhandled promise rejections and uncaught exceptions.
 * This function should be called once at the application startup, typically in _app.tsx.
 */
export const setupGlobalErrorListeners = (): void => {
  if (typeof window !== 'undefined') {
    window.addEventListener('unhandledrejection', (event: PromiseRejectionEvent) => {
      console.error('Unhandled promise rejection:', event.reason);

      const error = event.reason instanceof Error ? event.reason : new Error(String(event.reason));

      errorSubject.next({
        id: uuidv4(),
        message: 'Unhandled Promise Rejection',
        type: ErrorType.UNKNOWN,
        details: error.message,
        stack: error.stack,
        context: 'Global Error Listener',
        additionalInfo: JSON.stringify(error, null, 2),
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
        Column: ${errorDetails.colno}`;

      errorSubject.next({
        id: uuidv4(),
        message: 'Uncaught Exception',
        type: ErrorType.UNKNOWN,
        details: formattedMessage,
        stack: errorDetails.error.stack,
        context: 'Global Error Listener',
        additionalInfo: JSON.stringify(errorDetails, null, 2),
      });
    });
  }
};
