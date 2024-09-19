// src\pages\livedocs.tsx
import React from 'react';
import { Typography, Box, CircularProgress } from '@mui/material';
import dynamic from 'next/dynamic';

const LiveDocsPageContent = dynamic(() => import('../components/LiveDocsPageContent'), {
  ssr: false,
  loading: () => (
    <Box display="flex" justifyContent="center" alignItems="center" height="50vh">
      <CircularProgress />
    </Box>
  ),
});

const LiveDocsPage: React.FC = () => {
  return (
    <>
      <Typography variant="h4" component="h1" gutterBottom>
        Live Documents
      </Typography>
      <LiveDocsPageContent />
    </>
  );
};

export default LiveDocsPage;
