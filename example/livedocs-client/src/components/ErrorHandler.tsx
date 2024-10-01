// src/components/ErrorHandler.tsx

import React, { useState, useEffect } from 'react';
import {
  Snackbar,
  Alert,
  Button,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Typography,
  Box,
} from '@mui/material';
import { type ErrorNotification, errorSubject, ErrorType } from '@/utils/errorHandling';

/**
 * ErrorHandler Component
 *
 * This component provides a centralized error handling UI for the application.
 * It subscribes to the errorSubject and displays errors in two stages:
 * 1. A Snackbar with a brief error message and a "View Details" button.
 * 2. A Dialog with detailed error information, including stack trace if available.
 *
 * The component uses Material-UI components and follows Material Design 3 principles.
 * It categorizes errors and applies appropriate colors based on the error type.
 *
 * Usage:
 * Place this component at the top level of your application, typically in _app.tsx.
 */
const ErrorHandler: React.FC = () => {
  const [error, setError] = useState<ErrorNotification | null>(null);
  const [snackbarOpen, setSnackbarOpen] = useState(false);
  const [dialogOpen, setDialogOpen] = useState(false);

  useEffect(() => {
    const subscription = errorSubject.subscribe((errorNotification) => {
      setError(errorNotification);
      setSnackbarOpen(true);
    });

    return () => subscription.unsubscribe();
  }, []);

  const handleSnackbarClose = (_event?: React.SyntheticEvent | Event, reason?: string): void => {
    if (reason === 'clickaway') {
      return;
    }
    setSnackbarOpen(false);
  };

  const handleViewDetails = (): void => {
    setDialogOpen(true);
    setSnackbarOpen(false);
  };

  /**
   * Determines the appropriate color for the error alert based on the error type.
   *
   * @param type - The ErrorType to determine the color for
   * @returns A string representing the Material-UI color to use
   */
  const getErrorColor = (type: ErrorType): 'error' | 'warning' | 'info' => {
    switch (type) {
      case ErrorType.NETWORK:
        return 'error';
      case ErrorType.AUTHENTICATION:
        return 'warning';
      case ErrorType.VALIDATION:
        return 'info';
      case ErrorType.REPLICATION:
        return 'warning';
      default:
        return 'error';
    }
  };

  if (!error) return null;

  return (
    <>
      <Snackbar open={snackbarOpen} autoHideDuration={6000} onClose={handleSnackbarClose}>
        <Alert
          onClose={handleSnackbarClose}
          severity={getErrorColor(error.type)}
          variant="filled"
          action={
            <Button color="inherit" size="small" onClick={handleViewDetails}>
              View Details
            </Button>
          }
        >
          {error.message}
        </Alert>
      </Snackbar>
      <Dialog open={dialogOpen} onClose={() => setDialogOpen(false)} maxWidth="md" fullWidth>
        <DialogTitle>{`${error.type} Error`}</DialogTitle>
        <DialogContent>
          <Typography variant="h6" gutterBottom>
            {error.message}
          </Typography>
          <Box
            sx={{
              maxHeight: '400px',
              overflow: 'auto',
              backgroundColor: (theme) => theme.palette.grey[100],
              borderRadius: 1,
              p: 2,
              '&::-webkit-scrollbar': {
                width: '0.4em',
              },
              '&::-webkit-scrollbar-track': {
                boxShadow: 'inset 0 0 6px rgba(0,0,0,0.00)',
                webkitBoxShadow: 'inset 0 0 6px rgba(0,0,0,0.00)',
              },
              '&::-webkit-scrollbar-thumb': {
                backgroundColor: 'rgba(0,0,0,.1)',
                outline: '1px solid slategrey',
              },
            }}
          >
            <Typography variant="body2" component="pre">
              {error.details}
            </Typography>
            {error.stack !== '' ? (
              <>
                <Typography variant="h6" gutterBottom sx={{ mt: 2 }}>
                  Stack Trace
                </Typography>
                <Typography variant="body2" component="pre">
                  {error.stack}
                </Typography>
              </>
            ) : null}
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDialogOpen(false)} color="primary">
            Close
          </Button>
        </DialogActions>
      </Dialog>
    </>
  );
};

export default ErrorHandler;
