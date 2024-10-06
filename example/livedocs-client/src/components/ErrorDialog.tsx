// example/livedocs-client/src/components/ErrorDialog.tsx
import React from 'react';
import { Dialog, DialogTitle, DialogContent, DialogActions, Button, Typography, Box } from '@mui/material';

interface ErrorDialogProps {
  readonly open: boolean;
  readonly onClose: () => void;
  readonly error: Error | null;
}

const ErrorDialog: React.FC<ErrorDialogProps> = ({ open, onClose, error }) => {
  if (!error) return null;

  return (
    <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
      <DialogTitle>Error Details</DialogTitle>
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
            {error.stack}
          </Typography>
        </Box>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} color="primary">
          Close
        </Button>
      </DialogActions>
    </Dialog>
  );
};

export default ErrorDialog;
