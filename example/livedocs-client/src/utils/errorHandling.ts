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

  (toast.error as (message: string, options?: ToastOptions) => void)(
    `An error occurred in ${context}: ${errorMessage}. Please try again.`,
    toastOptions
  );
};
