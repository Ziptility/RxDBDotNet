import React from 'react';
import dynamic from 'next/dynamic';
import { Typography, Box, CircularProgress } from '@mui/material';

const LiveDocsPageContent = dynamic(
  () => import('../components/LiveDocsPageContent'),
  { 
    ssr: false,
    loading: () => (
      <Box display="flex" justifyContent="center" alignItems="center" height="50vh">
        <CircularProgress />
      </Box>
    ),
  }
);

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