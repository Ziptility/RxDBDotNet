// src/utils/errorHandling.ts

import { toast, type ToastOptions } from 'react-toastify';
import { RxError } from 'rxdb';
import { Subject } from 'rxjs';

/**
 * Represents the structure of an error notification.
 */
interface ErrorNotification {
  message: string;
  severity: 'error' | 'warning' | 'info';
}

/**
 * A Subject that emits error notifications.
 * This can be used to implement a global error handling mechanism.
 */
export const errorSubject = new Subject<ErrorNotification>();

/**
 * Logs an error to the console with optional context.
 * If the error is an RxError, it logs additional details.
 *
 * @param error - The error to be logged.
 * @param context - Optional context information for the error.
 */
export function logError(error: RxError | Error, context = ''): void {
  console.error(`Error${context ? ` in ${context}` : ''}:`, error);
  if (error instanceof RxError) {
    console.error('RxDB Error details:', error.parameters);
  }
}

/**
 * Notifies the user of an error or information through the errorSubject.
 * This can be used to trigger global error notifications.
 *
 * @param message - The message to be displayed to the user.
 * @param severity - The severity level of the message (default: 'error').
 */
export function notifyUser(message: string, severity: ErrorNotification['severity'] = 'error'): void {
  errorSubject.next({ message, severity });
}

/**
 * Handles an error by logging it, displaying a toast notification, and emitting it through errorSubject.
 * This function provides a comprehensive way to handle errors throughout the application.
 *
 * @param error - The error to be handled.
 * @param context - The context in which the error occurred.
 */
export function handleError(error: unknown, context: string): void {
  console.error(`Error in ${context}:`, error);

  let errorMessage: string;
  let errorDetails: string | undefined;

  if (error instanceof RxError) {
    errorMessage = error.message;
    errorDetails = JSON.stringify(error.parameters, null, 2);
  } else if (error instanceof Error) {
    errorMessage = error.message;
    errorDetails = error.stack;
  } else {
    errorMessage = 'An unknown error occurred';
  }

  const toastOptions: ToastOptions = {
    position: 'top-right',
    autoClose: 5000,
    hideProgressBar: false,
    closeOnClick: true,
    pauseOnHover: true,
    draggable: true,
  };

  const toastMessage = `Error in ${context}: ${errorMessage}${errorDetails === undefined ? '\n\nSee console for more details.' : ''}`;

  toast.error(toastMessage, toastOptions);

  // Also notify through the errorSubject for global error handling
  notifyUser(errorMessage, 'error');
}

/**
 * Handles asynchronous errors by executing a given function and handling any errors that occur.
 * This is useful for wrapping async operations with consistent error handling.
 *
 * @template T - The type of the value returned by the async function.
 * @param fn - The async function to execute.
 * @param errorMessage - A custom error message to use if an error occurs.
 * @returns A promise that resolves to the result of fn, or null if an error occurred.
 */
export async function handleAsyncError<T>(fn: () => Promise<T>, errorMessage: string): Promise<T | null> {
  try {
    return await fn();
  } catch (error: unknown) {
    handleError(error, errorMessage);
    return null;
  }
}

/**
 * A custom error class for replication errors.
 * This can be used to distinguish replication-specific errors in the application.
 */
export class ReplicationError extends Error {
  /**
   * Creates a new ReplicationError.
   *
   * @param message - The error message.
   * @param originalError - The original error that caused this ReplicationError.
   */
  constructor(
    message: string,
    public readonly originalError: RxError | Error
  ) {
    super(message);
    this.name = 'ReplicationError';
  }
}

/**
 * Sets up global error listeners for unhandled promise rejections and uncaught exceptions.
 * This function should be called once when the application initializes.
 */
export function setupGlobalErrorListeners(): void {
  if (typeof window !== 'undefined') {
    window.addEventListener('unhandledrejection', (event: PromiseRejectionEvent) => {
      const error: unknown = event.reason;
      console.error('Unhandled promise rejection:', error);
      handleError(error instanceof Error ? error : new Error(String(error)), 'Unhandled Promise Rejection');
    });

    window.addEventListener('error', (event: ErrorEvent) => {
      console.error('Uncaught exception:', event.error);
      handleError(event.error, 'Uncaught Exception');
    });
  }
}
