// src/utils/errorHandling.ts

import { Subject } from 'rxjs';
import { v4 as uuidv4 } from 'uuid';

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
  id: string;
  message: string;
  type: ErrorType;
  details: string | undefined;
  stack: string | undefined;
  context: string;
  additionalInfo: string | undefined;
}

/**
 * RxJS Subject that emits error notifications.
 * Components can subscribe to this to receive and handle errors.
 */
export const errorSubject = new Subject<ErrorNotification>();

/**
 * Determines the type of error based on its properties or message.
 * This function can be expanded to handle more specific error types.
 *
 * @param error - The Error object to categorize
 * @returns The determined ErrorType
 */
function determineErrorType(error: Error): ErrorType {
  const errorMessage = error.message.toLowerCase();
  if ('networkError' in error || errorMessage.includes('network')) return ErrorType.NETWORK;
  if ('authError' in error || errorMessage.includes('authentication')) return ErrorType.AUTHENTICATION;
  if ('validationError' in error || errorMessage.includes('validation')) return ErrorType.VALIDATION;
  if ('replicationError' in error || errorMessage.includes('replication')) return ErrorType.REPLICATION;
  return ErrorType.UNKNOWN;
}

/**
 * Handles errors by logging them and emitting an error notification.
 *
 * This function performs the following tasks:
 * 1. Logs the error to the console with context
 * 2. Creates an ErrorNotification object with a unique id
 * 3. Determines the error type
 * 4. Emits the error notification through the errorSubject
 *
 * @param error - The error object or unknown value to be handled
 * @param context - A string describing where the error occurred
 * @param additionalInfo - Optional object containing any additional information to include in the error details
 */
export function handleError(error: unknown, context: string, additionalInfo?: Record<string, unknown>): void {
  console.error(`Error in ${context}:`, error);

  const errorNotification: ErrorNotification = {
    id: uuidv4(),
    message: error instanceof Error ? error.message : 'An unknown error occurred',
    type: error instanceof Error ? determineErrorType(error) : ErrorType.UNKNOWN,
    details: error instanceof Error ? error.toString() : JSON.stringify(error),
    stack: error instanceof Error ? error.stack : undefined,
    context,
    additionalInfo: additionalInfo ? JSON.stringify(additionalInfo) : undefined,
  };

  errorSubject.next(errorNotification);
}

/**
 * Wraps an async function to handle any errors it may throw.
 *
 * This function provides a convenient way to wrap asynchronous operations
 * with error handling. It will catch any errors, process them using handleError,
 * and return null in case of an error.
 *
 * @param fn - The async function to execute
 * @param context - A string describing where the function is being called
 * @param additionalInfo - Optional object containing any additional information to include in the error details
 * @returns A Promise that resolves to the function's result or null if an error occurred
 */
export async function handleAsyncError<T>(
  fn: () => Promise<T>,
  context: string,
  additionalInfo?: Record<string, unknown>
): Promise<T | null> {
  try {
    return await fn();
  } catch (error: unknown) {
    handleError(error, context, additionalInfo);
    return null;
  }
}

/**
 * Creates a type guard function for a specific error type.
 *
 * This function generates a type guard that can be used to narrow down
 * the type of an unknown error to a specific Error subclass.
 *
 * @template T - The specific Error subclass to check for
 * @param errorType - The constructor of the specific Error subclass
 * @returns A type guard function for the specified Error subclass
 *
 * @example
 * const isNetworkError = createErrorTypeGuard(NetworkError);
 * if (isNetworkError(error)) {
 *   // error is now typed as NetworkError
 * }
 */
export function createErrorTypeGuard<T extends Error>(
  errorType: new (...args: unknown[]) => T
): (error: unknown) => error is T {
  return (error: unknown): error is T => error instanceof errorType;
}
