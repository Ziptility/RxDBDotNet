// example/livedocs-client/src/utils/errorHandling.ts

/**
 * Enum representing different types of errors in the application.
 */
export enum ErrorType {
  NETWORK = 'NETWORK',
  AUTHENTICATION = 'AUTHENTICATION',
  VALIDATION = 'VALIDATION',
  REPLICATION = 'REPLICATION',
  UNKNOWN = 'UNKNOWN',
}

/**
 * Handles errors by logging them to the console.
 *
 * @param error - The error object or unknown value to be handled
 * @param context - A string describing where the error occurred
 * @param additionalInfo - Optional object containing any additional information
 */
export function handleError(error: unknown, context: string, additionalInfo?: Record<string, unknown>): void {
  console.error(`Error in ${context}:`, error);
  if (additionalInfo) {
    console.error('Additional info:', additionalInfo);
  }
}

/**
 * Wraps an async function to handle any errors it may throw.
 *
 * @param fn - The async function to execute
 * @param context - A string describing where the function is being called
 * @param additionalInfo - Optional object containing any additional information
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
