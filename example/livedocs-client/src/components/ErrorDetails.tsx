// src/components/ErrorDetails.tsx

import React from 'react';
import { Typography, Box, useTheme } from '@mui/material';
import type { ErrorNotification } from '@/utils/errorHandling';

interface ErrorDetailsProps {
  readonly error: ErrorNotification;
}

/**
 * ErrorDetails Component
 *
 * This component displays detailed information about a specific error.
 * It's designed to be used within a dialog or modal, providing comprehensive
 * error information including context, details, additional info, and stack trace.
 *
 * @component
 * @param {Object} props - The component props
 * @param {ErrorNotification} props.error - The error notification object to display details for
 */
const ErrorDetails: React.FC<ErrorDetailsProps> = ({ error }) => {
  const theme = useTheme();

  return (
    <>
      <Typography variant="body2" gutterBottom>
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
        <Typography variant="body2" component="pre" sx={{ whiteSpace: 'pre-wrap', wordBreak: 'break-word' }}>
          {error.details}
        </Typography>
        {error.additionalInfo !== undefined ? (
          <>
            <Typography variant="h6" gutterBottom sx={{ mt: 2 }}>
              Additional Information
            </Typography>
            <Typography variant="body2" component="pre" sx={{ whiteSpace: 'pre-wrap', wordBreak: 'break-word' }}>
              {error.additionalInfo}
            </Typography>
          </>
        ) : null}
        {error.stack !== undefined ? (
          <>
            <Typography variant="h6" gutterBottom sx={{ mt: 2 }}>
              Stack Trace
            </Typography>
            <Typography variant="body2" component="pre" sx={{ whiteSpace: 'pre-wrap', wordBreak: 'break-word' }}>
              {error.stack}
            </Typography>
          </>
        ) : null}
      </Box>
    </>
  );
};

export default ErrorDetails;
