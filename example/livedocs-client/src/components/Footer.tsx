import React from 'react';
import { Box, Typography } from '@mui/material';

export const Footer: React.FC = () => (
  <Box
    component="footer"
    sx={{
      py: 2,
      px: 2,
      mt: 'auto',
      bgcolor: 'background.paper',
      borderTop: 1,
      borderColor: 'divider',
    }}
  >
    <Typography variant="body2" color="text.secondary" align="center">
      © {new Date().getFullYear()} RxDBDotNet Contributors. All rights reserved.
    </Typography>
  </Box>
);
