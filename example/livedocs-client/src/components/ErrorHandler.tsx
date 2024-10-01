// src/components/ErrorHandler.tsx

import React, { useState, useEffect, useCallback, useMemo } from 'react';
import {
  Error as ErrorIcon,
  Warning as WarningIcon,
  Info as InfoIcon,
  ExpandMore as ExpandMoreIcon,
  ExpandLess as ExpandLessIcon,
} from '@mui/icons-material';
import {
  Alert,
  AlertTitle,
  Button,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Typography,
  Box,
  useTheme,
} from '@mui/material';
import { motion, AnimatePresence } from 'framer-motion';
import { type ErrorNotification, errorSubject, ErrorType } from '@/utils/errorHandling';
import { motionProps } from '@/utils/motionSystem';

/**
 * Maximum number of errors to display before collapsing
 */
const MAX_VISIBLE_ERRORS = 3;

/**
 * Enum representing the possible states of the error handler
 */
enum ErrorState {
  NO_ERRORS = 'NO_ERRORS',
  SINGLE_ERROR = 'SINGLE_ERROR',
  MULTIPLE_ERRORS_COLLAPSED = 'MULTIPLE_ERRORS_COLLAPSED',
  MULTIPLE_ERRORS_EXPANDED = 'MULTIPLE_ERRORS_EXPANDED',
  ERROR_DETAILS_VIEW = 'ERROR_DETAILS_VIEW',
}

/**
 * Type representing a transition in the state machine
 */
type Transition = (prevState: ErrorState, context: ErrorHandlerContext) => ErrorState;

/**
 * Interface representing the context for the error handler state machine
 */
interface ErrorHandlerContext {
  errors: ErrorNotification[];
  expandedErrors: boolean;
  openDialogId: string | null;
}

/**
 * State machine for the error handler
 *
 * This state machine defines the transitions between different error states
 * and the actions that trigger these transitions. It ensures that the error
 * handling UI behaves consistently and predictably across various scenarios.
 */
const errorHandlerStateMachine: Record<ErrorState, Record<string, Transition>> = {
  /**
   * NO_ERRORS State:
   * Initial state when there are no errors to display.
   */
  [ErrorState.NO_ERRORS]: {
    // Transition when an error occurs
    ERROR_OCCURRED: (_, context) =>
      // If it's the first error, go to SINGLE_ERROR state
      // Otherwise, go to MULTIPLE_ERRORS_COLLAPSED state
      context.errors.length === 1 ? ErrorState.SINGLE_ERROR : ErrorState.MULTIPLE_ERRORS_COLLAPSED,
  },

  /**
   * SINGLE_ERROR State:
   * State when there is exactly one error being displayed.
   */
  [ErrorState.SINGLE_ERROR]: {
    // Transition when an error is resolved
    ERROR_RESOLVED: (_, context) =>
      // If all errors are resolved, go back to NO_ERRORS state
      // Otherwise, remain in SINGLE_ERROR state
      context.errors.length === 0 ? ErrorState.NO_ERRORS : ErrorState.SINGLE_ERROR,

    // Transition when another error occurs
    ERROR_OCCURRED: (_, context) =>
      // If there's more than one error now, go to MULTIPLE_ERRORS_COLLAPSED state
      // Otherwise, remain in SINGLE_ERROR state
      context.errors.length > 1 ? ErrorState.MULTIPLE_ERRORS_COLLAPSED : ErrorState.SINGLE_ERROR,

    // Transition when user requests to view error details
    VIEW_DETAILS: () => ErrorState.ERROR_DETAILS_VIEW,
  },

  /**
   * MULTIPLE_ERRORS_COLLAPSED State:
   * State when there are multiple errors, but they are displayed in a collapsed view.
   */
  [ErrorState.MULTIPLE_ERRORS_COLLAPSED]: {
    // Transition when an error is resolved
    ERROR_RESOLVED: (_, context) => {
      if (context.errors.length === 0) return ErrorState.NO_ERRORS;
      if (context.errors.length === 1) return ErrorState.SINGLE_ERROR;
      return ErrorState.MULTIPLE_ERRORS_COLLAPSED;
    },

    // Transition when another error occurs
    ERROR_OCCURRED: () => ErrorState.MULTIPLE_ERRORS_COLLAPSED,

    // Transition when user expands the error list
    EXPAND_ERRORS: () => ErrorState.MULTIPLE_ERRORS_EXPANDED,

    // Transition when user requests to view error details
    VIEW_DETAILS: () => ErrorState.ERROR_DETAILS_VIEW,
  },

  /**
   * MULTIPLE_ERRORS_EXPANDED State:
   * State when there are multiple errors and they are displayed in an expanded view.
   */
  [ErrorState.MULTIPLE_ERRORS_EXPANDED]: {
    // Transition when an error is resolved
    ERROR_RESOLVED: (_, context) => {
      if (context.errors.length === 0) return ErrorState.NO_ERRORS;
      if (context.errors.length === 1) return ErrorState.SINGLE_ERROR;
      return ErrorState.MULTIPLE_ERRORS_EXPANDED;
    },

    // Transition when another error occurs
    ERROR_OCCURRED: () => ErrorState.MULTIPLE_ERRORS_EXPANDED,

    // Transition when user collapses the error list
    COLLAPSE_ERRORS: () => ErrorState.MULTIPLE_ERRORS_COLLAPSED,

    // Transition when user requests to view error details
    VIEW_DETAILS: () => ErrorState.ERROR_DETAILS_VIEW,
  },

  /**
   * ERROR_DETAILS_VIEW State:
   * State when the user is viewing detailed information about a specific error.
   */
  [ErrorState.ERROR_DETAILS_VIEW]: {
    // Transition when user closes the error details view
    CLOSE_DETAILS: (_, context) => {
      if (context.errors.length === 0) return ErrorState.NO_ERRORS;
      if (context.errors.length === 1) return ErrorState.SINGLE_ERROR;
      // Return to the appropriate multiple errors state based on whether errors were expanded
      return context.expandedErrors ? ErrorState.MULTIPLE_ERRORS_EXPANDED : ErrorState.MULTIPLE_ERRORS_COLLAPSED;
    },
  },
};

/**
 * ErrorHandler Component
 *
 * This component provides a centralized error handling UI for the LiveDocs application,
 * designed to align with Material Design 3 principles and WCAG 2.1 AA standards.
 * It is optimized for desktop/laptop users with screens ranging from 13" to 27".
 *
 * @component
 */
const ErrorHandler: React.FC = () => {
  const [errors, setErrors] = useState<ErrorNotification[]>([]);
  const [openDialogId, setOpenDialogId] = useState<string | null>(null);
  const [expandedErrors, setExpandedErrors] = useState(false);
  const [currentState, setCurrentState] = useState<ErrorState>(ErrorState.NO_ERRORS);
  const theme = useTheme();

  const context: ErrorHandlerContext = useMemo(
    () => ({
      errors,
      expandedErrors,
      openDialogId,
    }),
    [errors, expandedErrors, openDialogId]
  );

  const transition = useCallback(
    (action: string) => {
      setCurrentState((prevState) => {
        const nextState = errorHandlerStateMachine[prevState][action]?.(prevState, context);
        return nextState ?? prevState;
      });
    },
    [context]
  );

  useEffect(() => {
    const subscription = errorSubject.subscribe((errorNotification) => {
      setErrors((prevErrors) => {
        const newErrors = [...prevErrors, errorNotification];
        transition('ERROR_OCCURRED');
        return newErrors;
      });
    });

    return () => subscription.unsubscribe();
  }, [transition]);

  const handleViewDetails = useCallback(
    (id: string) => () => {
      setOpenDialogId(id);
      transition('VIEW_DETAILS');
    },
    [transition]
  );

  const handleDialogClose = useCallback((): void => {
    setOpenDialogId(null);
    transition('CLOSE_DETAILS');
  }, [transition]);

  const getErrorSeverity = useCallback((type: ErrorType): 'error' | 'warning' | 'info' => {
    switch (type) {
      case ErrorType.NETWORK:
      case ErrorType.REPLICATION:
        return 'error';
      case ErrorType.AUTHENTICATION:
        return 'warning';
      case ErrorType.VALIDATION:
        return 'info';
      default:
        return 'error';
    }
  }, []);

  const getErrorIcon = useCallback((severity: 'error' | 'warning' | 'info') => {
    switch (severity) {
      case 'error':
        return <ErrorIcon />;
      case 'warning':
        return <WarningIcon />;
      case 'info':
        return <InfoIcon />;
    }
  }, []);

  if (currentState === ErrorState.NO_ERRORS) {
    return null;
  }

  return (
    <>
      <Box
        sx={{
          position: 'fixed',
          top: theme.spacing(2),
          right: theme.spacing(2),
          maxWidth: '400px',
          width: '100%',
          zIndex: theme.zIndex.snackbar + 1,
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'flex-end',
          pointerEvents: 'none',
        }}
      >
        <AnimatePresence>
          {errors.slice(0, expandedErrors ? undefined : MAX_VISIBLE_ERRORS).map((error) => (
            <motion.div
              key={error.id}
              {...motionProps['slideInFromTop']}
              style={{ width: '100%', marginBottom: theme.spacing(1) }}
            >
              <Alert
                severity={getErrorSeverity(error.type)}
                variant="filled"
                icon={getErrorIcon(getErrorSeverity(error.type))}
                sx={{
                  width: '100%',
                  pointerEvents: 'auto',
                  borderRadius: '12px',
                  boxShadow: theme.shadows[3],
                  '& .MuiAlert-message': {
                    overflow: 'hidden',
                    textOverflow: 'ellipsis',
                  },
                }}
              >
                <AlertTitle>{error.type} Error</AlertTitle>
                <Typography variant="body2" sx={{ mb: 1 }}>
                  {error.message.length > 50 ? `${error.message.substring(0, 50)}...` : error.message}
                </Typography>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                  <Button color="inherit" size="small" onClick={handleViewDetails(error.id)}>
                    View Details
                  </Button>
                  <Button
                    color="inherit"
                    size="small"
                    onClick={() => {
                      setErrors((prevErrors) => prevErrors.filter((e) => e.id !== error.id));
                      transition('ERROR_RESOLVED');
                    }}
                  >
                    Dismiss
                  </Button>
                </Box>
              </Alert>
            </motion.div>
          ))}
        </AnimatePresence>
        {errors.length > MAX_VISIBLE_ERRORS && (
          <Button
            onClick={() => {
              setExpandedErrors((prev) => !prev);
              transition(expandedErrors ? 'COLLAPSE_ERRORS' : 'EXPAND_ERRORS');
            }}
            sx={{ mt: 1, pointerEvents: 'auto' }}
            startIcon={expandedErrors ? <ExpandLessIcon /> : <ExpandMoreIcon />}
          >
            {expandedErrors ? 'Show Less' : `Show ${errors.length - MAX_VISIBLE_ERRORS} More`}
          </Button>
        )}
      </Box>
      <Dialog
        open={currentState === ErrorState.ERROR_DETAILS_VIEW}
        onClose={handleDialogClose}
        maxWidth="md"
        fullWidth
        aria-labelledby="error-dialog-title"
        PaperProps={{
          style: {
            borderRadius: '12px',
          },
        }}
      >
        <motion.div {...motionProps['fadeIn']}>
          {openDialogId !== null &&
            (() => {
              const error = errors.find((e) => e.id === openDialogId);
              if (!error) return null;
              return (
                <>
                  <DialogTitle id="error-dialog-title" sx={{ padding: theme.spacing(3) }}>
                    {`${error.type} Error Details`}
                  </DialogTitle>
                  <DialogContent dividers sx={{ padding: theme.spacing(3) }}>
                    <Typography
                      variant="body2"
                      gutterBottom
                      sx={{
                        overflow: 'hidden',
                        textOverflow: 'ellipsis',
                        whiteSpace: 'nowrap',
                        cursor: 'pointer',
                      }}
                    >
                      Context: {error.context}
                    </Typography>
                    <Box
                      sx={{
                        maxHeight: '400px',
                        overflow: 'auto',
                        backgroundColor: theme.palette.background.default,
                        borderRadius: '12px',
                        p: 2,
                        mt: 2,
                        '&::-webkit-scrollbar': {
                          width: '0.4em',
                        },
                        '&::-webkit-scrollbar-track': {
                          boxShadow: 'inset 0 0 6px rgba(0,0,0,0.00)',
                        },
                        '&::-webkit-scrollbar-thumb': {
                          backgroundColor: theme.palette.primary.light,
                          outline: `1px solid ${theme.palette.primary.main}`,
                          borderRadius: '4px',
                        },
                      }}
                    >
                      <Typography
                        variant="body2"
                        component="pre"
                        sx={{ whiteSpace: 'pre-wrap', wordBreak: 'break-word' }}
                      >
                        {error.details}
                      </Typography>
                      {error.additionalInfo !== undefined ? (
                        <>
                          <Typography variant="h6" gutterBottom sx={{ mt: 2 }}>
                            Additional Information
                          </Typography>
                          <Typography
                            variant="body2"
                            component="pre"
                            sx={{ whiteSpace: 'pre-wrap', wordBreak: 'break-word' }}
                          >
                            {error.additionalInfo}
                          </Typography>
                        </>
                      ) : null}
                      {error.stack !== undefined ? (
                        <>
                          <Typography variant="h6" gutterBottom sx={{ mt: 2 }}>
                            Stack Trace
                          </Typography>
                          <Typography
                            variant="body2"
                            component="pre"
                            sx={{ whiteSpace: 'pre-wrap', wordBreak: 'break-word' }}
                          >
                            {error.stack}
                          </Typography>
                        </>
                      ) : null}
                    </Box>
                  </DialogContent>
                  <DialogActions>
                    <Button onClick={handleDialogClose} color="primary">
                      Close
                    </Button>
                  </DialogActions>
                </>
              );
            })()}
        </motion.div>
      </Dialog>
    </>
  );
};

export default ErrorHandler;
