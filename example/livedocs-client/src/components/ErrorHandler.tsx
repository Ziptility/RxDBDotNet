// src/components/ErrorHandler.tsx

import React, { useState, useEffect, useCallback, useMemo } from 'react';
import { ExpandMore as ExpandMoreIcon, ExpandLess as ExpandLessIcon } from '@mui/icons-material';
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
  Collapse,
} from '@mui/material';
import { motion, AnimatePresence } from 'framer-motion';
import { type ErrorNotification, errorSubject, ErrorType } from '@/utils/errorHandling';
import { motionProps } from '@/utils/motionSystem';

/**
 * Maximum number of errors to display before collapsing
 */
const MAX_VISIBLE_ERRORS = 3;

/**
 * ErrorHandler Component
 *
 * This component provides a centralized error handling UI for the application,
 * designed to align with Material Design 3 principles and WCAG 2.1 AA standards.
 *
 * Key Features:
 * - Displays multiple errors as compact, grouped alerts
 * - Allows viewing detailed error information in a dialog
 * - Animates error notifications entry and exit
 * - Implements a collapsible UI for handling many simultaneous errors
 * - Ensures accessibility with proper ARIA attributes and color contrast
 *
 * Usage:
 * This component should be placed high in the component tree, typically in _app.tsx
 * or a layout component. It listens to the global errorSubject for new errors.
 *
 * @example
 * // In _app.tsx or layout component
 * import ErrorHandler from '@/components/ErrorHandler';
 *
 * const MyApp: React.FC = ({ Component, pageProps }) => (
 *   <>
 *     <ErrorHandler />
 *     <Component {...pageProps} />
 *   </>
 * );
 *
 * @component
 */
const ErrorHandler: React.FC = () => {
  const [errors, setErrors] = useState<ErrorNotification[]>([]);
  const [openDialogId, setOpenDialogId] = useState<string | null>(null);
  const [expandedErrors, setExpandedErrors] = useState(false);
  const theme = useTheme();

  useEffect(() => {
    const subscription = errorSubject.subscribe((errorNotification) => {
      setErrors((prevErrors) => [...prevErrors, errorNotification]);
    });

    return () => subscription.unsubscribe();
  }, []);

  const handleViewDetails = useCallback(
    (id: string) => () => {
      setOpenDialogId(id);
    },
    []
  );

  const handleDialogClose = useCallback((): void => {
    setOpenDialogId(null);
  }, []);

  /**
   * Determines the severity of the error alert based on the error type.
   * This affects the color and icon of the alert.
   */
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

  /**
   * Groups errors by their type for more compact display
   */
  const groupedErrors = useMemo(() => {
    return errors.reduce<Record<string, ErrorNotification[]>>((groups, error) => {
      const errorType = error.type;
      if (groups[errorType]) {
        groups[errorType].push(error);
      } else {
        groups[errorType] = [error];
      }
      return groups;
    }, {});
  }, [errors]);

  const toggleExpandErrors = useCallback(() => {
    setExpandedErrors((prev) => !prev);
  }, []);

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
          {Object.entries(groupedErrors).map(([errorType, errorGroup], index) => (
            <motion.div
              key={errorType}
              {...motionProps['slideInFromTop']}
              style={{ width: '100%', marginBottom: theme.spacing(1) }}
            >
              <Collapse in={index < MAX_VISIBLE_ERRORS || expandedErrors}>
                <Alert
                  severity={getErrorSeverity(errorType as ErrorType)}
                  variant="filled"
                  sx={{
                    width: '100%',
                    pointerEvents: 'auto',
                    borderRadius: theme.shape.borderRadius,
                    boxShadow: theme.shadows[3],
                  }}
                >
                  <AlertTitle>
                    {errorType} Error{errorGroup.length > 1 ? `s (${errorGroup.length})` : ''}
                  </AlertTitle>
                  <Typography variant="body2" sx={{ mb: 1 }}>
                    {(errorGroup[0]?.message?.length ?? 0) > 50
                      ? `${errorGroup[0]?.message?.substring(0, 50) ?? ''}...`
                      : (errorGroup[0]?.message ?? '')}
                  </Typography>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    {errorGroup[0] ? (
                      <Button color="inherit" size="small" onClick={handleViewDetails(errorGroup[0].id)}>
                        View Details
                      </Button>
                    ) : null}
                    {errorGroup.length > 1 && (
                      <Button color="inherit" size="small" onClick={() => setExpandedErrors((prev) => !prev)}>
                        {expandedErrors ? 'Hide' : 'Show All'}
                      </Button>
                    )}
                  </Box>
                </Alert>
              </Collapse>
            </motion.div>
          ))}
        </AnimatePresence>
        {Object.keys(groupedErrors).length > MAX_VISIBLE_ERRORS && (
          <Button
            onClick={toggleExpandErrors}
            startIcon={expandedErrors ? <ExpandLessIcon /> : <ExpandMoreIcon />}
            sx={{ mt: 1, pointerEvents: 'auto' }}
          >
            {expandedErrors ? 'Show Less' : `Show ${Object.keys(groupedErrors).length - MAX_VISIBLE_ERRORS} More`}
          </Button>
        )}
      </Box>
      <Dialog
        open={openDialogId !== null}
        onClose={handleDialogClose}
        maxWidth="md"
        fullWidth
        aria-labelledby="error-dialog-title"
        PaperProps={{
          style: {
            borderRadius: theme.shape.borderRadius,
            overflow: 'hidden',
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
                  <DialogTitle id="error-dialog-title">{`${error.type} Error Details`}</DialogTitle>
                  <DialogContent dividers>
                    <Typography variant="h6" gutterBottom>
                      {error.message}
                    </Typography>
                    <Typography variant="body2" gutterBottom>
                      Context: {error.context}
                    </Typography>
                    <Box
                      sx={{
                        maxHeight: '400px',
                        overflow: 'auto',
                        backgroundColor: theme.palette.background.default,
                        borderRadius: theme.shape.borderRadius,
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
                      {error.additionalInfo !== undefined && (
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
                      )}
                      {error.stack !== undefined && (
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
                      )}
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
