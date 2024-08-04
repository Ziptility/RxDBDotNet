import { Subject } from 'rxjs';
import { RxError } from 'rxdb';

export const errorSubject = new Subject<{ message: string; severity: 'error' | 'warning' | 'info' }>();

export function logError(error: RxError | Error, context?: string) {
  console.error(`Error${context ? ` in ${context}` : ''}:`, error);
  if (error instanceof RxError) {
    console.error('RxDB Error details:', error.parameters);
  }
}

export function notifyUser(message: string, severity: 'error' | 'warning' | 'info' = 'error') {
  errorSubject.next({ message, severity });
}

export async function retryWithBackoff<T>(
  operation: () => Promise<T>,
  retries = 3,
  baseDelay = 1000,
  maxDelay = 10000
): Promise<T> {
  let lastError: Error;

  for (let attempt = 0; attempt < retries; attempt++) {
    try {
      return await operation();
    } catch (error) {
      lastError = error as Error;
      const delay = Math.min(baseDelay * Math.pow(2, attempt), maxDelay);
      console.warn(`Attempt ${attempt + 1} failed. Retrying in ${delay}ms...`);
      await new Promise(resolve => setTimeout(resolve, delay));
    }
  }

  throw lastError!;
}

export class ReplicationError extends Error {
  constructor(message: string, public originalError: RxError | Error) {
    super(message);
    this.name = 'ReplicationError';
  }
}