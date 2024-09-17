// src\utils\errorHandling.ts
import { toast, ToastOptions } from 'react-toastify';

export const handleError = (error: unknown, context: string): void => {
  console.error(`Error in ${context}:`, error);

  const errorMessage: string = error instanceof Error ? error.message : 'An unknown error occurred';

  const toastOptions: ToastOptions = {
    position: 'top-right',
    autoClose: 5000,
    hideProgressBar: false,
    closeOnClick: true,
    pauseOnHover: true,
    draggable: true,
  };

  toast.error(`An error occurred in ${context}: ${errorMessage}. Please try again.`, toastOptions);
};

export async function handleAsyncError<T>(fn: () => Promise<T>, errorMessage: string): Promise<T | null> {
  try {
    return await fn();
  } catch (error) {
    console.error(`${errorMessage}:`, error);
    return null;
  }
}
