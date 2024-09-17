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

export const handleAsyncError = async <T>(asyncFunction: () => Promise<T>, context: string): Promise<T | undefined> => {
  try {
    return await asyncFunction();
  } catch (error) {
    handleError(error, context);
    return undefined;
  }
};
