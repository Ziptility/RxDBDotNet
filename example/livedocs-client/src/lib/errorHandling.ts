// src\lib\errorHandling.ts
import { Subject } from 'rxjs';
import { RxError } from 'rxdb';

export const errorSubject = new Subject<{
  message: string;
  severity: 'error' | 'warning' | 'info';
}>();

export function logError(error: RxError | Error, context = ''): void {
  console.error(`Error${context ? ` in ${context}` : ''}:`, error);
  if (error instanceof RxError) {
    console.error('RxDB Error details:', error.parameters);
  }
}

export function notifyUser(message: string, severity: 'error' | 'warning' | 'info' = 'error'): void {
  errorSubject.next({ message, severity });
}

export class ReplicationError extends Error {
  constructor(message: string, public originalError: RxError | Error) {
    super(message);
    this.name = 'ReplicationError';
  }
}
