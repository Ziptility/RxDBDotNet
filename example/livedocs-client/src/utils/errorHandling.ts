// src/utils/errorHandling.ts

import { Subject } from 'rxjs';

/**
 * Enum representing different types of errors in the application.
 * This can be expanded to include more specific error types as needed.
 */
export enum ErrorType {
  NETWORK = 'NETWORK',
  AUTHENTICATION = 'AUTHENTICATION',
  VALIDATION = 'VALIDATION',
  REPLICATION = 'REPLICATION',
  UNKNOWN = 'UNKNOWN',
}

/**
 * Interface describing the structure of error notifications.
 * This is used to standardize the error format across the application.
 */
export interface ErrorNotification {
  message: string;
  type: ErrorType;
  details?: string;
  stack?: string;
}

/**
 * RxJS Subject that emits error notifications.
 * Components can subscribe to this to receive and handle errors.
 */
export const errorSubject = new Subject<ErrorNotification>();

/**
 * Determines the type of error based on its message.
 * This function can be expanded to handle more specific error types.
 *
 * @param error - The Error object to categorize
 * @returns The determined ErrorType
 */
function determineErrorType(error: Error): ErrorType {
  if (error.message.toLowerCase().includes('network')) return ErrorType.NETWORK;
  if (error.message.toLowerCase().includes('authentication')) return ErrorType.AUTHENTICATION;
  if (error.message.toLowerCase().includes('validation')) return ErrorType.VALIDATION;
  if (error.message.toLowerCase().includes('replication')) return ErrorType.REPLICATION;
  return ErrorType.UNKNOWN;
}

/**
 * Handles errors by logging them and emitting an error notification.
 *
 * @param error - The error object or unknown value to be handled
 * @param context - A string describing where the error occurred
 */
export function handleError(error: unknown, context: string): void {
  console.error(`Error in ${context}:`, error);

  let errorNotification: ErrorNotification;

  if (error instanceof Error) {
    errorNotification = {
      message: error.message,
      type: determineErrorType(error),
      details: error.toString(),
      stack: error.stack ?? '',
    };
  } else {
    errorNotification = {
      message: 'An unknown error occurred',
      type: ErrorType.UNKNOWN,
      details: JSON.stringify(error),
    };
  }

  errorSubject.next(errorNotification);
}

/**
 * Wraps an async function to handle any errors it may throw.
 *
 * @param fn - The async function to execute
 * @param context - A string describing where the function is being called
 * @returns A Promise that resolves to the function's result or null if an error occurred
 */
export async function handleAsyncError<T>(fn: () => Promise<T>, context: string): Promise<T | null> {
  try {
    return await fn();
  } catch (error: unknown) {
    handleError(error, context);
    return null;
  }
}
